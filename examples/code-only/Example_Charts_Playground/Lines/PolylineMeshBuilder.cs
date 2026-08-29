using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Rendering.Lines;

/// <summary>
/// Builds ribbon <see cref="Mesh"/>es from points, so a line can have real thickness.
/// </summary>
/// <remarks>
/// <para>
/// Every point becomes two vertices, offset by half the width to either side of the line's direction
/// within the plane given by <see cref="PolylineOptions.Normal"/>. Consecutive pairs are joined with two
/// triangles. At interior points the offset direction is the average of the incoming and outgoing
/// directions, which gives a mitred join; very sharp corners therefore pinch, which is acceptable for
/// sampled curves and axes and avoids the extra geometry a rounded join would need.
/// </para>
/// <para>
/// The mesh carries positions, the plane normal and texture coordinates: <c>U</c> runs from 0 at the first
/// point to 1 at the last in proportion to distance travelled, <c>V</c> is 0 on one edge and 1 on the other.
/// Both are there for materials that want to fade the edges or animate along the line.
/// </para>
/// </remarks>
public static class PolylineMeshBuilder
{
    /// <summary>
    /// Builds one continuous ribbon through <paramref name="points"/>.
    /// </summary>
    /// <param name="device">The device to create the vertex and index buffers on.</param>
    /// <param name="points">The line's points, in order. At least two are required.</param>
    /// <param name="options">Width, plane and closure of the ribbon.</param>
    /// <returns>A mesh whose bounds are set, ready to be put in a <see cref="Model"/>.</returns>
    /// <exception cref="ArgumentNullException">If any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If there are fewer than two points, or <see cref="PolylineOptions.Normal"/> has no length.</exception>
    public static Mesh Build(GraphicsDevice device, IReadOnlyList<Vector3> points, PolylineOptions options)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(options);

        if (points.Count < 2)
        {
            throw new ArgumentException("A polyline needs at least two points.", nameof(points));
        }

        var normal = NormalOf(options);
        var vertices = new List<VertexPositionNormalTexture>(points.Count * 2);
        var indices = new List<int>(points.Count * 6);

        AppendRibbon(vertices, indices, points, normal, options.Width * 0.5f, options.Closed);

        return ToMesh(device, vertices, indices);
    }

    /// <summary>
    /// Builds many separate two-point ribbons as a single mesh - for tick marks, grids and anything else
    /// made of disconnected straight segments, where one mesh is far cheaper than one entity per segment.
    /// </summary>
    /// <param name="device">The device to create the vertex and index buffers on.</param>
    /// <param name="segments">The segments, each a start and an end point. At least one is required.</param>
    /// <param name="options">Width and plane of the ribbons. <see cref="PolylineOptions.Closed"/> is ignored.</param>
    /// <returns>A mesh whose bounds are set, ready to be put in a <see cref="Model"/>.</returns>
    /// <exception cref="ArgumentNullException">If any argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If there are no segments, or <see cref="PolylineOptions.Normal"/> has no length.</exception>
    public static Mesh BuildSegments(GraphicsDevice device, IReadOnlyList<(Vector3 Start, Vector3 End)> segments, PolylineOptions options)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(options);

        if (segments.Count == 0)
        {
            throw new ArgumentException("At least one segment is required.", nameof(segments));
        }

        var normal = NormalOf(options);
        var halfWidth = options.Width * 0.5f;
        var vertices = new List<VertexPositionNormalTexture>(segments.Count * 4);
        var indices = new List<int>(segments.Count * 6);
        var pair = new Vector3[2];

        foreach (var (start, end) in segments)
        {
            pair[0] = start;
            pair[1] = end;
            AppendRibbon(vertices, indices, pair, normal, halfWidth, closed: false);
        }

        return ToMesh(device, vertices, indices);
    }

    private static Vector3 NormalOf(PolylineOptions options)
    {
        var normal = options.Normal;

        if (normal.LengthSquared() <= MathUtil.ZeroTolerance)
        {
            throw new ArgumentException("The plane normal must not be zero.", nameof(options));
        }

        normal.Normalize();

        return normal;
    }

    /// <summary>
    /// Appends the ribbon for one polyline to the shared vertex and index lists.
    /// </summary>
    private static void AppendRibbon(List<VertexPositionNormalTexture> vertices, List<int> indices, IReadOnlyList<Vector3> points, Vector3 normal, float halfWidth, bool closed)
    {
        var count = points.Count;
        var segmentCount = closed ? count : count - 1;
        var baseIndex = vertices.Count;

        // Distance along the line at each point, for the U texture coordinate
        var distances = new float[count];
        for (var i = 1; i < count; i++)
        {
            distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
        }

        var totalLength = distances[count - 1];

        if (closed)
        {
            totalLength += Vector3.Distance(points[count - 1], points[0]);
        }

        for (var i = 0; i < count; i++)
        {
            var tangent = TangentAt(points, i, closed);
            var side = Vector3.Cross(normal, tangent);
            side.Normalize();
            side *= halfWidth;

            var u = totalLength > 0 ? distances[i] / totalLength : 0f;

            vertices.Add(new VertexPositionNormalTexture(points[i] - side, normal, new Vector2(u, 0f)));
            vertices.Add(new VertexPositionNormalTexture(points[i] + side, normal, new Vector2(u, 1f)));
        }

        for (var s = 0; s < segmentCount; s++)
        {
            var a = baseIndex + s * 2;
            var b = baseIndex + ((s + 1) % count) * 2;

            indices.Add(a);
            indices.Add(a + 1);
            indices.Add(b);
            indices.Add(a + 1);
            indices.Add(b + 1);
            indices.Add(b);
        }
    }

    private static Mesh ToMesh(GraphicsDevice device, List<VertexPositionNormalTexture> vertexList, List<int> indexList)
    {
        var vertices = vertexList.ToArray();
        var indices = indexList.ToArray();

        var vertexBuffer = Buffer.New(device, vertices, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
        var indexBuffer = Buffer.New(device, indices, BufferFlags.IndexBuffer, GraphicsResourceUsage.Default);

        var meshDraw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertices.Length)],
            IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: true, indices.Length),
            DrawCount = indices.Length,
        };

        var positions = new Vector3[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            positions[i] = vertices[i].Position;
        }

        var boundingBox = BoundingBox.FromPoints(positions);

        return new Mesh
        {
            Draw = meshDraw,
            BoundingBox = boundingBox,
            BoundingSphere = BoundingSphere.FromBox(boundingBox),
        };
    }

    /// <summary>
    /// The unit direction of the line at point <paramref name="i"/>: one-sided at open ends, otherwise the
    /// average of the directions into and out of the point.
    /// </summary>
    private static Vector3 TangentAt(IReadOnlyList<Vector3> points, int i, bool closed)
    {
        var count = points.Count;

        Vector3 incoming, outgoing;

        if (closed)
        {
            incoming = points[i] - points[(i - 1 + count) % count];
            outgoing = points[(i + 1) % count] - points[i];
        }
        else if (i == 0)
        {
            incoming = outgoing = points[1] - points[0];
        }
        else if (i == count - 1)
        {
            incoming = outgoing = points[count - 1] - points[count - 2];
        }
        else
        {
            incoming = points[i] - points[i - 1];
            outgoing = points[i + 1] - points[i];
        }

        incoming.Normalize();
        outgoing.Normalize();

        var tangent = incoming + outgoing;

        // The line doubles back on itself: average is zero, fall back to the outgoing direction
        if (tangent.LengthSquared() <= MathUtil.ZeroTolerance)
        {
            tangent = outgoing;
        }

        tangent.Normalize();

        return tangent;
    }
}