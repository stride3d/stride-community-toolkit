namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Triangulates 2D polygons by ear clipping, with or without holes.
/// </summary>
/// <remarks>
/// <para>
/// Ear clipping repeatedly finds a corner whose triangle lies entirely inside the polygon (an
/// "ear"), emits that triangle, and removes the corner - always terminating with n-2 triangles for
/// an n-vertex polygon. It handles any <em>simple</em> polygon, convex or concave.
/// </para>
/// <para>
/// A polygon with holes is first made simple: each hole is joined to the outside by a bridge, two
/// edges that run between a corner of the hole and a corner of the outside that can see it, and the
/// result is clipped as one polygon. That is how a font's O or B becomes triangles; see
/// <see cref="Triangulate(IReadOnlyList{Vector2}, IReadOnlyList{IReadOnlyList{Vector2}})"/>.
/// Self-intersecting polygons are not handled by either overload.
/// </para>
/// <para>
/// The tolerances scale with the polygon: a corner counts as flat, or a point as on a triangle's
/// edge, relative to the polygon's extent, so a glyph a centimetre across triangulates the same way
/// as one a metre across.
/// </para>
/// </remarks>
public static class EarClipping
{
    /// <summary>
    /// Triangulates a simple polygon into a list of vertex-index triples.
    /// </summary>
    /// <param name="polygon">The polygon's corners, in order, without repeating the first corner at the end. Either winding is accepted.</param>
    /// <returns>
    /// Indices into <paramref name="polygon"/>, three per triangle, (corner count - 2) triangles in
    /// total for a polygon with no flat corners. Triangles are always wound counter-clockwise,
    /// whichever way the input was wound.
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
        var ring = new List<int>(polygon.Count);

        if (SignedArea(polygon) >= 0)
        {
            for (var i = 0; i < polygon.Count; i++) ring.Add(i);
        }
        else
        {
            for (var i = polygon.Count - 1; i >= 0; i--) ring.Add(i);
        }

        return ClipRing(ring, polygon, ToleranceFor(polygon));
    }

    /// <summary>
    /// Triangulates a polygon with holes into a list of vertex-index triples.
    /// </summary>
    /// <param name="outer">The outside, in order, without repeating the first corner. Either winding is accepted.</param>
    /// <param name="holes">The holes, each in order without repeating its first corner, each lying inside the outside and not touching it or each other. Either winding is accepted.</param>
    /// <returns>
    /// Indices into the corners of <paramref name="outer"/> followed by those of each hole in order,
    /// three per triangle, wound counter-clockwise. A polygon of n corners in all with h holes gives
    /// up to n + 2h - 2 triangles; a flat corner made by a bridge is clipped without one.
    /// </returns>
    /// <exception cref="ArgumentNullException">The outside or the holes are null.</exception>
    /// <exception cref="ArgumentException">The outside has fewer than three corners.</exception>
    /// <exception cref="InvalidOperationException">A hole lies outside the outside, or the polygon self-intersects.</exception>
    /// <remarks>
    /// Each hole is bridged to the outside at its rightmost corner, towards the first edge a ray to
    /// the right meets, the method of Eberly's "Triangulation by Ear Clipping". Holes are bridged
    /// rightmost first, so a bridge never crosses a hole bridged later. A hole with fewer than three
    /// corners is ignored.
    /// </remarks>
    public static List<int> Triangulate(IReadOnlyList<Vector2> outer, IReadOnlyList<IReadOnlyList<Vector2>> holes)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentNullException.ThrowIfNull(holes);

        if (holes.Count == 0) return Triangulate(outer);

        if (outer.Count < 3)
            throw new ArgumentException("A polygon needs at least three corners", nameof(outer));

        // Every corner in one list, the outside first and then each hole, which is what the indices mean
        var corners = new List<Vector2>(outer);
        var holeStarts = new int[holes.Count];

        for (var h = 0; h < holes.Count; h++)
        {
            ArgumentNullException.ThrowIfNull(holes[h], nameof(holes));

            holeStarts[h] = corners.Count;
            corners.AddRange(holes[h]);
        }

        var tolerance = ToleranceFor(corners);

        // Counter-clockwise outside, clockwise holes: a bridge then keeps the merged ring simple
        var ring = new List<int>(corners.Count + holes.Count * 2);

        if (SignedArea(outer) >= 0)
        {
            for (var i = 0; i < outer.Count; i++) ring.Add(i);
        }
        else
        {
            for (var i = outer.Count - 1; i >= 0; i--) ring.Add(i);
        }

        // Rightmost hole first: its bridge runs to the right of every other hole, which a later bridge cannot cross
        var order = Enumerable.Range(0, holes.Count)
            .Where(h => holes[h].Count >= 3)
            .OrderByDescending(h => holes[h].Max(c => c.X))
            .ToList();

        foreach (var h in order)
        {
            var hole = holes[h];
            var holeRing = new int[hole.Count];
            var clockwise = SignedArea(hole) < 0;

            for (var i = 0; i < hole.Count; i++) holeRing[i] = holeStarts[h] + (clockwise ? i : hole.Count - 1 - i);

            // The hole's rightmost corner, which can always see some corner of the outside to its right
            var rightmost = 0;

            for (var i = 1; i < holeRing.Length; i++)
            {
                var candidate = corners[holeRing[i]];
                var best = corners[holeRing[rightmost]];

                if (candidate.X > best.X || (candidate.X == best.X && candidate.Y > best.Y)) rightmost = i;
            }

            var m = holeRing[rightmost];
            var bridgeAt = PolygonBridging.FindBridge(ring, corners, corners[m], tolerance);

            // Walk the hole from its rightmost corner all the way round and back, then return to the outside
            var splice = new List<int>(holeRing.Length + 2);

            for (var i = 0; i < holeRing.Length; i++) splice.Add(holeRing[(rightmost + i) % holeRing.Length]);

            splice.Add(m);
            splice.Add(ring[bridgeAt]);

            ring.InsertRange(bridgeAt + 1, splice);
        }

        return ClipRing(ring, corners, tolerance);
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

    /// <summary>
    /// The tolerance for the corners' size: a cross product below it is flat, a point within it of a
    /// triangle's edge is on it. Relative to the polygon's extent, so the result does not depend on the
    /// unit the corners are in.
    /// </summary>
    private static float ToleranceFor(IReadOnlyList<Vector2> corners)
    {
        var min = new Vector2(float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity);

        foreach (var corner in corners)
        {
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }

        var extent = MathF.Max(max.X - min.X, max.Y - min.Y);

        return extent > 0f ? 1e-6f * extent * extent : 1e-12f;
    }

    /// <summary>Clips a counter-clockwise ring of corner indices, which may hold the same corner twice where holes were bridged in.</summary>
    private static List<int> ClipRing(List<int> ring, IReadOnlyList<Vector2> corners, float tolerance)
    {
        var triangles = new List<int>((ring.Count - 2) * 3);

        while (ring.Count > 3)
        {
            var clipped = TryClipEar(ring, corners, triangles, tolerance, strict: true) || TryClipEar(ring, corners, triangles, tolerance, strict: false);

            if (!clipped)
            {
                // A simple polygon always has at least two ears (the two-ears theorem), so reaching
                // here means the input crosses itself
                throw new InvalidOperationException(
                    "No ear could be clipped; the polygon is probably self-intersecting");
            }
        }

        var last = Cross(corners[ring[0]], corners[ring[1]], corners[ring[2]]);

        if (last > 0f)
        {
            triangles.Add(ring[0]);
            triangles.Add(ring[1]);
            triangles.Add(ring[2]);
        }
        else if (last < -tolerance)
        {
            // The last three corners of a correctly clipped counter-clockwise ring turn left or lie
            // flat; turning right means an ear swallowed a corner and the triangles do not cover the polygon
            throw new InvalidOperationException("The last triangle winds the wrong way; the polygon is probably self-intersecting");
        }

        return triangles;
    }

    private static bool TryClipEar(List<int> ring, IReadOnlyList<Vector2> corners, List<int> triangles, float tolerance, bool strict)
    {
        for (var i = 0; i < ring.Count; i++)
        {
            var previous = ring[(i - 1 + ring.Count) % ring.Count];
            var current = ring[i];
            var next = ring[(i + 1) % ring.Count];

            if (!IsEar(corners, ring, previous, current, next, tolerance, strict)) continue;

            // Any area at all is a triangle the cap needs, however thin: the walls keep its edges, so
            // dropping it would leave a slit. Only a corner with no area is clipped without one
            if (Cross(corners[previous], corners[current], corners[next]) > 0f)
            {
                triangles.Add(previous);
                triangles.Add(current);
                triangles.Add(next);
            }

            ring.RemoveAt(i);

            return true;
        }

        return false;
    }

    private static bool IsEar(IReadOnlyList<Vector2> corners, List<int> ring, int previous, int current, int next, float tolerance, bool strict)
    {
        var a = corners[previous];
        var b = corners[current];
        var c = corners[next];
        var cross = Cross(a, b, c);

        // A reflex corner can never be an ear; its triangle lies partly outside the polygon.
        // Collinear corners (cross ~ 0) are skipped on the strict pass - their zero-area triangle
        // would add nothing and can wedge the algorithm - and clipped away on the relaxed one
        if (strict ? cross <= tolerance : cross < -tolerance) return false;

        // The triangle must contain no other remaining corner, or clipping it would swallow part of
        // the polygon. A corner at exactly the same point as one of the triangle's own is the other
        // end of a bridge, the same corner twice, and does not count; a corner merely near one is a
        // corner of its own, however close, and the tolerance is not used for that
        foreach (var index in ring)
        {
            if (index == previous || index == current || index == next) continue;

            var point = corners[index];

            if (point == a || point == b || point == c) continue;

            if (ContainsPoint(a, b, c, point, tolerance)) return false;
        }

        return true;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool ContainsPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 point, float tolerance)
        => Cross(a, b, point) >= -tolerance
        && Cross(b, c, point) >= -tolerance
        && Cross(c, a, point) >= -tolerance;
}