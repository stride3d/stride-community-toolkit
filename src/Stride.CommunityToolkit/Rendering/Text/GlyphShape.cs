namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// A flattened glyph: its outer polygons, each with the holes inside it. What a triangulator takes.
/// </summary>
public sealed class GlyphShape
{
    private readonly GlyphPolygon[] _polygons;

    private GlyphShape(GlyphPolygon[] polygons)
    {
        _polygons = polygons;
    }

    /// <summary>The filled pieces of the glyph: one for an O, two for an i.</summary>
    public IReadOnlyList<GlyphPolygon> Polygons => _polygons;

    /// <summary>Whether there is nothing to fill.</summary>
    public bool IsEmpty => _polygons.Length == 0;

    /// <summary>
    /// Sorts closed polygons into outsides and holes by how they nest, the way a font's even-odd fill
    /// rule reads them: a contour inside no other is an outside, one inside exactly one is a hole of it,
    /// one inside two is an outside again (the dot inside a copyright sign), and so on.
    /// </summary>
    /// <param name="contours">Closed polygons in either winding, without the first corner repeated.</param>
    /// <returns>The shape. Outsides come back counter-clockwise and holes clockwise, whatever they were.</returns>
    /// <exception cref="ArgumentNullException">The contours are null.</exception>
    /// <remarks>
    /// Whether one contour lies inside another is decided by a vote of three of its points, so two
    /// contours that touch at a corner still sort correctly. Contours with fewer than three corners or
    /// no area are ignored.
    /// </remarks>
    public static GlyphShape FromContours(IReadOnlyList<Vector2[]> contours)
    {
        ArgumentNullException.ThrowIfNull(contours);

        // Only contours that can be filled take part; a line or a point would skew the nesting below
        var valid = new List<Vector2[]>(contours.Count);

        foreach (var contour in contours)
        {
            if (contour is { Length: >= 3 } && MathF.Abs(EarClipping.SignedArea(contour)) > 1e-10f) valid.Add(contour);
        }

        // Depth is how many other contours enclose a contour: even means an outside, odd a hole of the
        // enclosing contour one level up
        var count = valid.Count;
        var depth = new int[count];
        var contains = new bool[count, count];

        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                if (i == j) continue;

                contains[j, i] = Encloses(valid[j], valid[i]);

                if (contains[j, i]) depth[i]++;
            }
        }

        // Each outside takes the holes one level deeper that it encloses
        var polygons = new List<GlyphPolygon>();

        for (var i = 0; i < count; i++)
        {
            if (depth[i] % 2 != 0) continue;

            var holes = new List<Vector2[]>();

            for (var h = 0; h < count; h++)
            {
                if (depth[h] == depth[i] + 1 && contains[i, h]) holes.Add(Wind(valid[h], clockwise: true));
            }

            polygons.Add(new GlyphPolygon(Wind(valid[i], clockwise: false), holes));
        }

        return new GlyphShape([.. polygons]);
    }

    /// <summary>Whether <paramref name="outer"/> encloses <paramref name="inner"/>, by a vote of three of the inner points.</summary>
    private static bool Encloses(Vector2[] outer, Vector2[] inner)
    {
        var votes = 0;

        for (var k = 0; k < 3; k++)
        {
            if (Contains(outer, inner[k * inner.Length / 3])) votes++;
        }

        return votes >= 2;
    }

    /// <summary>Even-odd point-in-polygon by ray casting.</summary>
    private static bool Contains(Vector2[] polygon, Vector2 point)
    {
        var inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];

            if (a.Y > point.Y != b.Y > point.Y && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }

        return inside;
    }

    private static Vector2[] Wind(Vector2[] polygon, bool clockwise)
    {
        var isClockwise = EarClipping.SignedArea(polygon) < 0f;

        if (isClockwise == clockwise) return polygon;

        var reversed = new Vector2[polygon.Length];

        for (var i = 0; i < polygon.Length; i++) reversed[i] = polygon[polygon.Length - 1 - i];

        return reversed;
    }
}