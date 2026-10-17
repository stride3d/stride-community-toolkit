namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// Finds where a hole joins the ring of a polygon, for <see cref="EarClipping"/>: the corner of the
/// ring that the hole's rightmost corner can see along a ray to the right, by Eberly's "Triangulation
/// by Ear Clipping". The ring is counter-clockwise and may hold the same corner twice where an earlier
/// hole was bridged in.
/// </summary>
internal static class PolygonBridging
{
    /// <summary>
    /// The position in <paramref name="ring"/> of the corner that <paramref name="m"/>, a hole's rightmost
    /// corner, is bridged to.
    /// </summary>
    /// <exception cref="InvalidOperationException">No ring edge lies to the right of <paramref name="m"/>: the hole is outside the polygon.</exception>
    internal static int FindBridge(List<int> ring, IReadOnlyList<Vector2> corners, Vector2 m, float tolerance)
    {
        var hit = CastRay(ring, corners, m, tolerance);

        // The ray met a corner itself: that corner is visible, bridge to it
        if (hit.Corner >= 0) return CopyFacing(ring, corners, hit.Corner, m, tolerance);

        var count = ring.Count;
        var first = hit.Edge;
        var second = (hit.Edge + 1) % count;
        var firstCorner = corners[ring[first]];
        var secondCorner = corners[ring[second]];
        var point = new Vector2(hit.X, m.Y);

        if ((point - firstCorner).LengthSquared() < tolerance) return CopyFacing(ring, corners, first, m, tolerance);
        if ((point - secondCorner).LengthSquared() < tolerance) return CopyFacing(ring, corners, second, m, tolerance);

        // The ray crossed the edge between its corners: the one further right is the candidate, unless a
        // reflex corner of the ring sits in the way
        var p = firstCorner.X > secondCorner.X ? first : second;

        return CopyFacing(ring, corners, NearestReflexInTheWay(ring, corners, m, point, p, tolerance), m, tolerance);
    }

    /// <summary>The first ring edge, or ring corner, a ray from <paramref name="m"/> to the right meets.</summary>
    private static RayHit CastRay(List<int> ring, IReadOnlyList<Vector2> corners, Vector2 m, float tolerance)
    {
        var count = ring.Count;
        var hit = new RayHit(-1, -1, float.PositiveInfinity);

        for (var k = 0; k < count; k++)
        {
            var a = corners[ring[k]];
            var b = corners[ring[(k + 1) % count]];

            // A corner sitting on the ray is a hit in itself: the half-open test below only sees a corner
            // whose two edges leave it on opposite sides of the ray, and misses one whose edges both stay
            // on or under it, such as the top corner of a hole level with m
            if (MathF.Abs(a.Y - m.Y) <= tolerance && a.X >= m.X - tolerance && a.X < hit.X) hit = new RayHit(-1, k, a.X);

            // Half-open on Y, so a ring corner exactly at the ray's height counts for one of its two edges only
            if (a.Y > m.Y == b.Y > m.Y) continue;

            var x = a.X + (m.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);

            if (x >= m.X - tolerance && x < hit.X) hit = new RayHit(k, -1, x);
        }

        if (hit.Edge < 0 && hit.Corner < 0)
            throw new InvalidOperationException("A hole lies outside the polygon it was given as a hole of");

        return hit;
    }

    /// <summary>
    /// A reflex corner inside the triangle (m, hit point, p) would be crossed by the bridge from m to p;
    /// of those, the one closest in angle to the ray is the one m can see, by Eberly's argument. Without
    /// one, p itself.
    /// </summary>
    private static int NearestReflexInTheWay(List<int> ring, IReadOnlyList<Vector2> corners, Vector2 m, Vector2 hitPoint, int p, float tolerance)
    {
        var pCorner = corners[ring[p]];
        var best = p;
        var bestCos = float.NegativeInfinity;
        var bestDistance = float.PositiveInfinity;

        for (var k = 0; k < ring.Count; k++)
        {
            if (k == p) continue;

            var r = corners[ring[k]];

            if ((r - pCorner).LengthSquared() < tolerance) continue;
            if (!IsReflex(ring, corners, k, tolerance)) continue;
            if (!ContainsPoint(m, hitPoint, pCorner, r, tolerance) && !ContainsPoint(m, pCorner, hitPoint, r, tolerance)) continue;

            var d = r - m;
            var distance = d.Length();

            if (distance < tolerance) continue;

            var cos = d.X / distance;

            if (cos > bestCos || (cos == bestCos && distance < bestDistance))
            {
                best = k;
                bestCos = cos;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// An earlier bridge may have put the corner at <paramref name="position"/> in the ring twice; the
    /// copy whose inside angle faces <paramref name="m"/> is the one a bridge from m can reach without
    /// crossing the ring.
    /// </summary>
    private static int CopyFacing(List<int> ring, IReadOnlyList<Vector2> corners, int position, Vector2 m, float tolerance)
    {
        var chosen = ring[position];

        if (SectorContains(ring, corners, position, m, tolerance)) return position;

        for (var k = 0; k < ring.Count; k++)
        {
            if (ring[k] == chosen && SectorContains(ring, corners, k, m, tolerance)) return k;
        }

        return position;
    }

    private static bool IsReflex(List<int> ring, IReadOnlyList<Vector2> corners, int k, float tolerance)
    {
        var count = ring.Count;
        var previous = corners[ring[(k + count - 1) % count]];
        var current = corners[ring[k]];
        var next = corners[ring[(k + 1) % count]];

        return Cross(previous, current, next) < -tolerance;
    }

    /// <summary>Whether the direction from the ring corner at <paramref name="k"/> to <paramref name="target"/> lies inside the polygon's angle at that corner.</summary>
    private static bool SectorContains(List<int> ring, IReadOnlyList<Vector2> corners, int k, Vector2 target, float tolerance)
    {
        var count = ring.Count;
        var current = corners[ring[k]];
        var a = corners[ring[(k + count - 1) % count]] - current;
        var b = corners[ring[(k + 1) % count]] - current;
        var d = target - current;
        var leftOfNext = b.X * d.Y - b.Y * d.X >= 0f;
        var rightOfPrevious = d.X * a.Y - d.Y * a.X >= 0f;

        return IsReflex(ring, corners, k, tolerance) ? leftOfNext || rightOfPrevious : leftOfNext && rightOfPrevious;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool ContainsPoint(Vector2 a, Vector2 b, Vector2 c, Vector2 point, float tolerance)
        => Cross(a, b, point) >= -tolerance
        && Cross(b, c, point) >= -tolerance
        && Cross(c, a, point) >= -tolerance;

    /// <summary>What the ray met first: an edge (by its first corner's ring position) or a corner, and how far right.</summary>
    private readonly record struct RayHit(int Edge, int Corner, float X);
}