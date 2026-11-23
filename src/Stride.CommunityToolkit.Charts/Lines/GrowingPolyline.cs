using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using System.Runtime.InteropServices;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The GPU side of a trail that grows: one ribbon mesh whose buffers are made once, for the trail's capacity,
/// and refilled from the trail's points when they change or when the width they are drawn at has moved.
/// </summary>
/// <remarks>
/// The points themselves live in a <see cref="TrailBuffer"/>, which also draws a flat chart's trail as strokes;
/// this type only turns them into a ribbon for a 3D chart. Runs are separated by the buffer's break marks, and a
/// run of fewer than two points draws nothing.
/// </remarks>
internal sealed class GrowingPolyline : IDisposable
{
    private readonly IGame _game;
    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly MeshDraw _draw;
    private readonly List<Vector3> _run = [];
    private readonly List<VertexPositionNormalTexture> _vertices;
    private readonly List<int> _indices;
    private readonly float _baseHalfWidth;
    private float _halfWidth;
    private bool _isDisposed;

    /// <summary>The mesh to put in a model; its draw count and bounds follow the points.</summary>
    internal Mesh Mesh { get; }

    /// <summary>
    /// Creates the ribbon with room for <paramref name="capacity"/> points.
    /// </summary>
    /// <param name="game">The game, for the device and the command list.</param>
    /// <param name="capacity">The most points the trail holds; at least two.</param>
    /// <param name="options">The ribbon's width, which <see cref="Update"/> scales.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than two.</exception>
    internal GrowingPolyline(IGame game, int capacity, PolylineOptions options)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);

        _game = game;
        _baseHalfWidth = options.Width * 0.5f;
        _halfWidth = _baseHalfWidth;

        var maxVertices = capacity * 2;
        var maxIndices = (capacity - 1) * 6;

        _vertices = new List<VertexPositionNormalTexture>(maxVertices);
        _indices = new List<int>(maxIndices);

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

        Mesh = new Mesh { Draw = _draw, BoundingBox = BoundingBox.Empty };
    }

    /// <summary>
    /// Rebuilds the ribbon from the trail's points when they changed, or when the width has moved by more than
    /// <see cref="ChartRibbon.WidthTolerance"/> since it was last built; otherwise does nothing. A perspective
    /// camera changes the width a little on every orbit step, and a rebuild is two buffer uploads.
    /// </summary>
    /// <param name="items">The trail's points, with its break marks, as <see cref="TrailBuffer.Items"/> gives them.</param>
    /// <param name="widthScale">What the options' width is multiplied by: one pixel's worth of chart units.</param>
    /// <param name="changed">Whether the points changed since the last call.</param>
    internal void Update(ReadOnlySpan<Vector3> items, float widthScale, bool changed)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var halfWidth = _baseHalfWidth * widthScale;

        if (!changed && MathF.Abs(halfWidth - _halfWidth) <= _halfWidth * ChartRibbon.WidthTolerance) return;

        _halfWidth = halfWidth;

        Rebuild(items);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    private void Rebuild(ReadOnlySpan<Vector3> items)
    {
        _vertices.Clear();
        _indices.Clear();
        _run.Clear();

        var margin = new Vector3(_halfWidth);
        var bounds = BoundingBox.Empty;
        var any = false;

        foreach (ref readonly var item in items)
        {
            if (TrailBuffer.IsBreak(in item))
            {
                AppendRun();
                continue;
            }

            _run.Add(item);

            var pointBox = new BoundingBox(item - margin, item + margin);

            bounds = any ? BoundingBox.Merge(bounds, pointBox) : pointBox;
            any = true;
        }

        AppendRun();

        _draw.DrawCount = _indices.Count;
        Mesh.BoundingBox = bounds;
        Mesh.BoundingSphere = any ? BoundingSphere.FromBox(bounds) : default;

        if (_vertices.Count == 0)
            return;

        var commandList = _game.GraphicsContext.CommandList;
        // Pinned: see SetDataPinned for what the engine's Direct3D 11 upload does with an unpinned array
        _vertexBuffer.SetDataPinned(commandList, (ReadOnlySpan<VertexPositionNormalTexture>)CollectionsMarshal.AsSpan(_vertices));
        _indexBuffer.SetDataPinned(commandList, (ReadOnlySpan<int>)CollectionsMarshal.AsSpan(_indices));
    }

    // The run collected so far as one ribbon, in the chart plane; a single point has no segment to draw
    private void AppendRun()
    {
        if (_run.Count >= 2)
            PolylineMeshBuilder.AppendRibbon(_vertices, _indices, _run, Vector3.UnitZ, _halfWidth, closed: false);

        _run.Clear();
    }
}