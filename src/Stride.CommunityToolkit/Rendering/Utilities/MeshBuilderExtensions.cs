namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Higher-level shapes composed on top of <see cref="MeshBuilder"/>'s per-vertex API.
/// </summary>
public static class MeshBuilderExtensions
{
    /// <summary>
    /// Adds a 2D polygon extruded along Z: a front cap, a back cap, and flat-shaded side walls.
    /// </summary>
    /// <param name="builder">The builder to add to. Must have an index type configured, and position and normal elements registered.</param>
    /// <param name="outline">The polygon's corners in the XY plane, in order, without repeating the first corner. Either winding is accepted. Concave outlines are fine; self-intersecting ones are not.</param>
    /// <param name="depth">How far the shape extends along Z, centred on zero.</param>
    /// <param name="positionElement">The element index returned by <c>WithPosition</c>.</param>
    /// <param name="normalElement">The element index returned by <c>WithNormal</c>.</param>
    /// <param name="offset">An XY offset applied to every corner, so several shapes can be laid out side by side in one mesh.</param>
    /// <exception cref="ArgumentNullException">The builder or outline is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The depth is not positive.</exception>
    /// <remarks>
    /// <para>
    /// The caps share their corner vertices, but every side wall gets four vertices of its own: a
    /// flat normal per wall is what keeps an extruded shape's edges crisp, and that is only possible
    /// when walls do not share vertices with each other or with the caps. An n-corner outline
    /// therefore costs 6n vertices and 4(n-1) triangles.
    /// </para>
    /// <para>
    /// A shape with a hole is built by calling this once per piece with pieces that touch along their
    /// edges without overlapping - abutting walls seal invisibly inside the solid, while overlapping
    /// pieces would put two caps on the same plane and shimmer. Lettering from a font, holes and all,
    /// is <see cref="Text.Meshes.TextMesh"/>'s job.
    /// </para>
    /// </remarks>
    public static void AddExtrudedPolygon(
        this MeshBuilder builder,
        IReadOnlyList<Vector2> outline,
        float depth,
        int positionElement,
        int normalElement,
        Vector2 offset = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);

        // Everything below assumes counter-clockwise: cap winding for front faces, and the
        // "rotate the edge right" outward normal for the walls
        var corners = EarClipping.SignedArea(outline) >= 0
            ? outline
            : [.. outline.Reverse()];

        var triangles = EarClipping.Triangulate(corners);
        var halfDepth = depth * 0.5f;

        // Stride's default rasterizer state follows Direct3D: front faces are CLOCKWISE as seen on
        // screen (RasterizerStateDescription.DefaultFrontFaceCounterClockwise = false). The
        // triangulation comes back counter-clockwise, so the cap facing the viewer must be emitted
        // reversed. Getting this backwards turns the whole solid inside-out - the front cap culls
        // away and the camera sees the interior of every wall at once, which reads as a hollow mould
        // whose parallax moves the wrong way.

        // Front cap: viewed from +Z, so its triangles must wind clockwise in the XY plane
        var frontStart = AddCapVertices(builder, corners, offset, halfDepth, Vector3.UnitZ, positionElement, normalElement);

        for (var i = 0; i < triangles.Count; i += 3)
        {
            builder.AddIndex(frontStart + triangles[i + 2]);
            builder.AddIndex(frontStart + triangles[i + 1]);
            builder.AddIndex(frontStart + triangles[i]);
        }

        // Back cap: viewed from -Z, where the counter-clockwise triangulation already appears clockwise
        var backStart = AddCapVertices(builder, corners, offset, -halfDepth, -Vector3.UnitZ, positionElement, normalElement);

        for (var i = 0; i < triangles.Count; i += 3)
        {
            builder.AddIndex(backStart + triangles[i]);
            builder.AddIndex(backStart + triangles[i + 1]);
            builder.AddIndex(backStart + triangles[i + 2]);
        }

        // Side walls, one flat-shaded quad per outline edge
        for (var i = 0; i < corners.Count; i++)
        {
            var from = corners[i] + offset;
            var to = corners[(i + 1) % corners.Count] + offset;

            // For a counter-clockwise outline the interior lies to an edge's left, so rotating the
            // edge direction right points away from the shape
            var edge = to - from;
            var length = edge.Length();

            if (MathUtil.IsZero(length)) continue;

            var normal = new Vector3(edge.Y / length, -edge.X / length, 0);

            var a = AddVertex(builder, new Vector3(from, halfDepth), normal, positionElement, normalElement);
            var b = AddVertex(builder, new Vector3(to, halfDepth), normal, positionElement, normalElement);
            var c = AddVertex(builder, new Vector3(to, -halfDepth), normal, positionElement, normalElement);
            var d = AddVertex(builder, new Vector3(from, -halfDepth), normal, positionElement, normalElement);

            // Clockwise as seen from outside the wall, matching the caps above
            builder.AddIndex(a);
            builder.AddIndex(b);
            builder.AddIndex(c);

            builder.AddIndex(a);
            builder.AddIndex(c);
            builder.AddIndex(d);
        }
    }

    /// <summary>One vertex per corner at depth <paramref name="z"/>, all with the cap's normal; returns the first one's index.</summary>
    private static int AddCapVertices(MeshBuilder builder, IReadOnlyList<Vector2> corners, Vector2 offset, float z, Vector3 normal, int positionElement, int normalElement)
    {
        var start = builder.VertexCount;

        foreach (var corner in corners)
        {
            AddVertex(builder, new Vector3(corner + offset, z), normal, positionElement, normalElement);
        }

        return start;
    }

    private static int AddVertex(MeshBuilder builder, Vector3 position, Vector3 normal, int positionElement, int normalElement)
    {
        var index = builder.AddVertex();

        builder.SetElement(positionElement, position);
        builder.SetElement(normalElement, normal);

        return index;
    }
}