namespace Stride.CommunityToolkit.Rendering.Utilities;

/// <summary>
/// Triangulates a simple 2D polygon by ear clipping.
/// </summary>
/// <remarks>
/// <para>
/// Ear clipping repeatedly finds a corner whose triangle lies entirely inside the polygon (an
/// "ear"), emits that triangle, and removes the corner - always terminating with n-2 triangles for
/// an n-vertex polygon. It handles any <em>simple</em> polygon, convex or concave, which is what the
/// extruded letter glyphs need: X, Y and Z are all concave.
/// </para>
/// <para>
/// It does not handle self-intersecting polygons or holes. A glyph with a hole, such as O or 8, is
/// built from several abutting polygons; see <see cref="LetterMeshFactory"/>.
/// </para>
/// </remarks>
public static class EarClipping
{
    private const float Epsilon = 1e-6f;

    /// <summary>
    /// Triangulates a simple polygon into a list of vertex-index triples.
    /// </summary>
    /// <param name="polygon">The polygon's corners, in order, without repeating the first corner at the end. Either winding is accepted.</param>
    /// <returns>
    /// Indices into <paramref name="polygon"/>, three per triangle, (corner count - 2) triangles in
    /// total. Triangles are always wound counter-clockwise, whichever way the input was wound.
    /// </returns>
    /// <exception cref="ArgumentNullException">The polygon is null.</exception>
    /// <exception cref="ArgumentException">The polygon has fewer than three corners.</exception>
    /// <exception cref="InvalidOperationException">No ear could be found, which means the polygon self-intersects.</exception>
    public static List<int> Triangulate(IReadOnlyList<Vector2> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.Count < 3)
            throw new ArgumentException("A polygon needs at least three corners", nameof(polygon));

        // Working on counter-clockwise indices makes "convex corner" a single sign test below; the
        // caller's winding is restored by emitting whatever order the working list holds
        var indices = new List<int>(polygon.Count);

        if (SignedArea(polygon) >= 0)
        {
            for (var i = 0; i < polygon.Count; i++) indices.Add(i);
        }
        else
        {
            for (var i = polygon.Count - 1; i >= 0; i--) indices.Add(i);
        }

        var triangles = new List<int>((polygon.Count - 2) * 3);

        while (indices.Count > 3)
        {
            var earFound = false;

            for (var i = 0; i < indices.Count; i++)
            {
                var previous = indices[(i - 1 + indices.Count) % indices.Count];
                var current = indices[i];
                var next = indices[(i + 1) % indices.Count];

                if (!IsEar(polygon, indices, previous, current, next)) continue;

                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
                indices.RemoveAt(i);

                earFound = true;
                break;
            }

            if (!earFound)
            {
                // A simple polygon always has at least two ears (the two-ears theorem), so reaching
                // here means the input crosses itself
                throw new InvalidOperationException(
                    "No ear could be clipped; the polygon is probably self-intersecting");
            }
        }

        triangles.Add(indices[0]);
        triangles.Add(indices[1]);
        triangles.Add(indices[2]);

        return triangles;
    }

    /// <summary>
    /// Returns the polygon's signed area: positive for counter-clockwise winding.
    /// </summary>
    /// <param name="polygon">The polygon's corners, in order.</param>
    /// <returns>The signed area, by the shoelace formula.</returns>
    public static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        var area = 0f;

        for (var i = 0; i < polygon.Count; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Count];

            area += current.X * next.Y - next.X * current.Y;
        }

        return area * 0.5f;
    }

    private static bool IsEar(IReadOnlyList<Vector2> polygon, List<int> indices, int previous, int current, int next)
    {
        var a = polygon[previous];
        var b = polygon[current];
        var c = polygon[next];

        // A reflex corner can never be an ear; its triangle lies partly outside the polygon.
        // Collinear corners (cross ~ 0) are skipped too - their zero-area triangle would add nothing
        // and can wedge the algorithm.
        if (Cross(a, b, c) <= Epsilon) return false;

        // The triangle must contain no other remaining corner, or clipping it would swallow part of
        // the polygon
        foreach (var index in indices)
        {
            if (index == previous || index == current || index == next) continue;

            if (ContainsPoint(a, b, c, polygon[index])) return false;
        }

        return true;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool ContainsPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 point)
        => Cross(a, b, point) >= -Epsilon
        && Cross(b, c, point) >= -Epsilon
        && Cross(c, a, point) >= -Epsilon;
}