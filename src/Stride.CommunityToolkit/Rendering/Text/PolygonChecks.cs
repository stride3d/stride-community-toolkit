namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Segment and loop tests shared by the glyph shape and the bevel: whether loops cross themselves or
/// each other, and whether one loop is a safe offset of another.
/// </summary>
internal static class PolygonChecks
{
    /// <summary>Whether two non-adjacent edges of the loop cross.</summary>
    internal static bool SelfIntersects(IReadOnlyList<Vector2> loop)
    {
        var count = loop.Count;

        for (var i = 0; i < count; i++)
        {
            var a = loop[i];
            var b = loop[(i + 1) % count];

            for (var j = i + 2; j < count; j++)
            {
                // The last edge is adjacent to the first
                if (i == 0 && j == count - 1) continue;

                if (Crosses(a, b, loop[j], loop[(j + 1) % count])) return true;
            }
        }

        return false;
    }

    /// <summary>Whether any edge of one loop crosses any edge of the other.</summary>
    internal static bool Cross(IReadOnlyList<Vector2> first, IReadOnlyList<Vector2> second)
    {
        for (var i = 0; i < first.Count; i++)
        {
            var a = first[i];
            var b = first[(i + 1) % first.Count];

            for (var j = 0; j < second.Count; j++)
            {
                if (Crosses(a, b, second[j], second[(j + 1) % second.Count])) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="moved"/> is <paramref name="loop"/> pushed sideways without any edge turning
    /// round: every edge of the moved loop still runs the way the original edge runs. An offset that pushed
    /// a corner through the far side of the shape reverses the edges around it, which a final area or
    /// crossing test can miss once the corners have swapped places.
    /// </summary>
    internal static bool KeepsEdgeDirections(IReadOnlyList<Vector2> loop, IReadOnlyList<Vector2> moved)
    {
        var count = loop.Count;

        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            var original = loop[next] - loop[i];
            var offset = moved[next] - moved[i];

            // An edge that shrank to nothing is allowed (a corner closing up); one that points back is not
            if (offset.LengthSquared() < 1e-12f * original.LengthSquared()) continue;
            if (Vector2.Dot(original, offset) <= 0f) return false;
        }

        return true;
    }

    /// <summary>Whether the segments a-b and c-d cross, touching ends not counted.</summary>
    private static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var d1 = Side(c, d, a);
        var d2 = Side(c, d, b);
        var d3 = Side(a, b, c);
        var d4 = Side(a, b, d);

        return d1 * d2 < 0f && d3 * d4 < 0f;

        static float Side(Vector2 p, Vector2 q, Vector2 r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }
}