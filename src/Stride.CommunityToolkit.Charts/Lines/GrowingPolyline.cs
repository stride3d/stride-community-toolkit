using Stride.CommunityToolkit.Graphics;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Charts.Lines;

/// <summary>
/// A polyline that grows one point at a time - the trail of a moving body, drawn while it moves. The GPU
/// buffers are allocated once for <see cref="Capacity"/> points and updated in place, so feeding a point
/// every frame creates no buffers, no entities and no garbage.
/// </summary>
/// <remarks>
/// <para>
/// A static ribbon from <see cref="PolylineMeshBuilder"/> is built once and never touched again; this class
/// exists for the other case, a line whose points are not known up front. <see cref="Add"/> appends a point,
/// <see cref="Break"/> lifts the pen so the next point starts a new run (how a trail leaves the chart and
/// comes back), <see cref="Clear"/> starts over. The buffers use <see cref="GraphicsResourceUsage.Default"/>
/// and are refilled with <c>UpdateSubresource</c>; the CPU-side lists are reused between calls.
/// </para>
/// <para>
/// Call <see cref="Add"/>, <see cref="Break"/> and <see cref="Clear"/> from the game thread only - they
/// write to the GPU buffers through the game's command list.
/// </para>
/// <para>
/// The mesh's bounds either stay fixed (pass <c>bounds</c> when the reachable area is known, such as a
/// chart's ranges) or grow to enclose the points as they arrive. The <c>U</c> texture coordinate is the
/// distance along the run in world units, not normalised - a growing line has no final length to divide by.
/// </para>
/// </remarks>
internal sealed class GrowingPolyline : IDisposable
{
    private readonly IGame _game;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly MeshDraw _draw;
    private readonly List<List<Vector3>> _runs = [];
    private readonly List<VertexPositionNormalTexture> _vertices;
    private readonly List<int> _indices;
    private readonly Vector3 _normal;
    private readonly float _baseHalfWidth;
    private float _halfWidth;
    private readonly BoundingBox? _fixedBounds;
    private bool _hasGrownBounds;
    private int _count;
    private bool _isDisposed;

    /// <summary>The mesh to put in a <see cref="Model"/>; its draw count and bounds are kept up to date.</summary>
    internal Mesh Mesh { get; }

    /// <summary>How many points the line holds, over all runs.</summary>
    internal int Count => _count;

    /// <summary>The most points the line can hold; fixed at construction, when the buffers are allocated.</summary>
    internal int Capacity { get; }

    /// <summary>
    /// What a full line does with the next point: <see langword="false"/> (the default) ignores it,
    /// <see langword="true"/> drops the oldest point instead - an oscilloscope trace.
    /// </summary>
    internal bool RollOver { private get; set; }

    /// <summary>
    /// Allocates the GPU buffers for a line of at most <paramref name="capacity"/> points.
    /// </summary>
    /// <param name="game">The game whose device the buffers live on and whose command list uploads the points.</param>
    /// <param name="capacity">The most points the line can hold. At least two.</param>
    /// <param name="options">Width and plane of the ribbon. <see cref="PolylineOptions.Closed"/> is ignored; a growing line is open.</param>
    /// <param name="bounds">
    /// The mesh bounds to use for the line's whole life, when the reachable area is known up front - a
    /// chart's ranges, an arena. <see langword="null"/> grows the bounds as points arrive.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than two.</exception>
    /// <exception cref="ArgumentException">If <see cref="PolylineOptions.Normal"/> has no length.</exception>
    internal GrowingPolyline(IGame game, int capacity, PolylineOptions options, BoundingBox? bounds = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);

        _game = game;
        Capacity = capacity;
        _normal = PolylineMeshBuilder.NormalOf(options);
        _baseHalfWidth = options.Width * 0.5f;
        _halfWidth = _baseHalfWidth;
        _fixedBounds = bounds;

        var maxVertices = capacity * 2;
        var maxIndices = (capacity - 1) * 6;

        _vertices = new List<VertexPositionNormalTexture>(maxVertices);
        _indices = new List<int>(maxIndices);
        _runs.Add([]);

        var device = game.GraphicsDevice;
        _vertexBuffer = Buffer.Vertex.New(device, maxVertices * VertexPositionNormalTexture.Layout.CalculateSize(), GraphicsResourceUsage.Default, BufferFlags.None);
        _indexBuffer = Buffer.Index.New(device, maxIndices * sizeof(int), GraphicsResourceUsage.Default);

        _draw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            VertexBuffers = [new VertexBufferBinding(_vertexBuffer, VertexPositionNormalTexture.Layout, maxVertices)],
            IndexBuffer = new IndexBufferBinding(_indexBuffer, is32Bit: true, maxIndices),
            DrawCount = 0,
        };

        Mesh = new Mesh
        {
            Draw = _draw,
            BoundingBox = bounds ?? BoundingBox.Empty,
            BoundingSphere = bounds is { } b ? BoundingSphere.FromBox(b) : default,
        };
    }

    /// <summary>
    /// Appends a point and updates the mesh. A point that coincides with the previous one is absorbed, a
    /// point that is not finite acts as a <see cref="Break"/>.
    /// </summary>
    /// <param name="point">The next point of the line.</param>
    /// <returns>
    /// <see langword="false"/> when the line is full and <see cref="RollOver"/> is off, so the point was
    /// ignored; otherwise <see langword="true"/>.
    /// </returns>
    internal bool Add(Vector3 point)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z))
        {
            Break();
            return true;
        }

        var run = _runs[^1];

        if (run.Count > 0 && (point - run[^1]).LengthSquared() <= MathUtil.ZeroTolerance * MathUtil.ZeroTolerance)
        {
            return true;
        }

        if (_count == Capacity)
        {
            if (!RollOver)
                return false;

            DropOldest();
        }

        run.Add(point);
        _count++;

        GrowBounds(point);
        Rebuild();

        return true;
    }

    /// <summary>
    /// Lifts the pen: the next <see cref="Add"/> starts a new run instead of connecting to the last point.
    /// Harmless when the line is empty or already broken.
    /// </summary>
    internal void Break()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_runs[^1].Count > 0)
        {
            _runs.Add([]);
        }
    }

    /// <summary>
    /// Removes every point, keeping the buffers for reuse.
    /// </summary>
    internal void Clear()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _runs.Clear();
        _runs.Add([]);
        _count = 0;
        _vertices.Clear();
        _indices.Clear();
        _draw.DrawCount = 0;

        if (_fixedBounds is null)
        {
            _hasGrownBounds = false;
            Mesh.BoundingBox = BoundingBox.Empty;
            Mesh.BoundingSphere = default;
        }
    }

    /// <summary>
    /// Rescales the ribbon width relative to the width the line was created with and rebuilds the mesh -
    /// how a view-driven chart keeps a recorded trail the same thickness on screen while zooming.
    /// </summary>
    /// <param name="scale">The multiplier on the creation-time width; <c>1</c> restores it.</param>
    internal void SetWidthScale(float scale)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var halfWidth = _baseHalfWidth * scale;

        if (halfWidth == _halfWidth)
            return;

        _halfWidth = halfWidth;
        Rebuild();
    }

    /// <summary>
    /// Disposes the vertex and index buffers. Do not draw the mesh afterwards.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    private void DropOldest()
    {
        var first = _runs[0];

        first.RemoveAt(0);
        _count--;

        // A run reduced to one point draws nothing; drop it too unless it is the only (open) run
        if (first.Count == 0 && _runs.Count > 1)
        {
            _runs.RemoveAt(0);
        }
    }

    private void GrowBounds(Vector3 point)
    {
        if (_fixedBounds is not null)
            return;

        var margin = new Vector3(_halfWidth);
        var pointBox = new BoundingBox(point - margin, point + margin);

        Mesh.BoundingBox = _hasGrownBounds ? BoundingBox.Merge(Mesh.BoundingBox, pointBox) : pointBox;
        Mesh.BoundingSphere = BoundingSphere.FromBox(Mesh.BoundingBox);
        _hasGrownBounds = true;
    }

    /// <summary>
    /// Refills the CPU lists from the runs and uploads the used part of both buffers. A full rebuild every
    /// point is deliberately simple; at a few thousand points it is far below anything measurable, and one
    /// code path means the mitred joins are always right.
    /// </summary>
    private void Rebuild()
    {
        _vertices.Clear();
        _indices.Clear();

        foreach (var run in _runs)
        {
            if (run.Count >= 2)
            {
                PolylineMeshBuilder.AppendRibbon(_vertices, _indices, run, _normal, _halfWidth, closed: false);
            }
        }

        _draw.DrawCount = _indices.Count;

        if (_vertices.Count == 0)
            return;

        var commandList = _game.GraphicsContext.CommandList;
        // Pinned: see SetDataPinned for what the engine's Direct3D 11 upload does with an unpinned array
        _vertexBuffer.SetDataPinned(commandList, (ReadOnlySpan<VertexPositionNormalTexture>)CollectionsMarshal.AsSpan(_vertices));
        _indexBuffer.SetDataPinned(commandList, (ReadOnlySpan<int>)CollectionsMarshal.AsSpan(_indices));
    }
}