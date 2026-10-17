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
    /// Sorts closed polygons into outsides and holes by how they nest: a contour inside no other is an
    /// outside, one inside exactly one is a hole of it, one inside two is an outside again (the dot
    /// inside a copyright sign), and so on. That is how fonts draw letters: nested contours that do not
    /// touch, each wound the other way from the one around it.
    /// </summary>
    /// <param name="contours">Closed polygons in either winding, without the first corner repeated.</param>
    /// <returns>The shape. Outsides come back counter-clockwise and holes clockwise, whatever they were.</returns>
    /// <exception cref="ArgumentNullException">The contours are null.</exception>
    /// <exception cref="InvalidDataException">
    /// Two contours cross, a contour crosses itself, or a contour nested inside another is wound the same
    /// way as it: shapes a nonzero fill would merge, which this does not do. Some composite glyphs and
    /// variable fonts have them; a reader that does should say which glyph.
    /// </exception>
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

        RejectCrossings(valid);

        var (depth, contains) = Nest(valid);

        return new GlyphShape(Assemble(valid, depth, contains));
    }

    /// <summary>Contours that cross each other, or themselves, are not a shape this can fill.</summary>
    private static void RejectCrossings(List<Vector2[]> contours)
    {
        for (var i = 0; i < contours.Count; i++)
        {
            if (PolygonChecks.SelfIntersects(contours[i])) throw new InvalidDataException("a contour crosses itself");

            for (var j = i + 1; j < contours.Count; j++)
            {
                if (PolygonChecks.Cross(contours[i], contours[j])) throw new InvalidDataException("two contours cross each other");
            }
        }
    }

    /// <summary>
    /// How the contours nest: for each, how many others enclose it (even means an outside, odd a hole of
    /// the one a level up), and which encloses which.
    /// </summary>
    private static (int[] Depth, bool[,] Contains) Nest(List<Vector2[]> contours)
    {
        var count = contours.Count;
        var depth = new int[count];
        var contains = new bool[count, count];

        for (var i = 0; i < count; i++)
        {
            for (var j = 0; j < count; j++)
            {
                if (i == j) continue;

                contains[j, i] = Encloses(contours[j], contours[i]);

                if (contains[j, i]) depth[i]++;
            }
        }

        return (depth, contains);
    }

    /// <summary>
    /// Each outside takes the holes one level deeper that it encloses. A hole wound the same way as its
    /// outside is not a hole in a font's fill: with the nonzero rule it is more of the same shape.
    /// </summary>
    private static GlyphPolygon[] Assemble(List<Vector2[]> contours, int[] depth, bool[,] contains)
    {
        var polygons = new List<GlyphPolygon>();

        for (var i = 0; i < contours.Count; i++)
        {
            if (depth[i] % 2 != 0) continue;

            var holes = new List<Vector2[]>();

            for (var h = 0; h < contours.Count; h++)
            {
                if (depth[h] != depth[i] + 1 || !contains[i, h]) continue;

                if (MathF.Sign(EarClipping.SignedArea(contours[h])) == MathF.Sign(EarClipping.SignedArea(contours[i])))
                    throw new InvalidDataException("a contour nested inside another is wound the same way, which overlaps rather than cuts a hole");

                holes.Add(Wind(contours[h], clockwise: true));
            }

            polygons.Add(new GlyphPolygon(Wind(contours[i], clockwise: false), holes));
        }

        return [.. polygons];
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