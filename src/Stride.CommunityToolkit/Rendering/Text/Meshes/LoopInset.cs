namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

/// <summary>
/// Moves a closed loop into the material for a bevel, and says whether the result is still a loop
/// that can be capped. For <see cref="TextMesh"/>.
/// </summary>
internal static class LoopInset
{
    /// <summary>
    /// The loop moved <paramref name="distance"/> into the material: to the left of every edge, which is
    /// inside a counter-clockwise outside and outside a clockwise hole. Each corner moves along the
    /// bisector of its two edges' normals, far enough to keep both offset edges at the distance (the
    /// miter), capped so a needle-sharp corner does not shoot off.
    /// </summary>
    internal static Vector2[] Inset(Vector2[] loop, float distance)
    {
        var count = loop.Length;
        var moved = new Vector2[count];

        for (var i = 0; i < count; i++)
        {
            var previous = loop[(i + count - 1) % count];
            var current = loop[i];
            var next = loop[(i + 1) % count];
            var into = current - previous;
            var outOf = next - current;

            if (into.LengthSquared() < 1e-14f) into = outOf;
            if (outOf.LengthSquared() < 1e-14f) outOf = into;

            into.Normalize();
            outOf.Normalize();

            var leftIn = new Vector2(-into.Y, into.X);
            var leftOut = new Vector2(-outOf.Y, outOf.X);
            var bisector = leftIn + leftOut;

            // The two edges double back on each other: no bisector, so move off the first edge alone
            if (bisector.LengthSquared() < 1e-10f)
            {
                moved[i] = current + leftIn * distance;

                continue;
            }

            bisector.Normalize();

            var miter = MathF.Min(1f / MathF.Max(Vector2.Dot(bisector, leftIn), 1e-3f), 2.5f);

            moved[i] = current + bisector * (distance * miter);
        }

        return moved;
    }

    /// <summary>
    /// Whether every pulled-in loop is still the loop it came from, pushed sideways: the same way round,
    /// the outside smaller and each hole larger (the material shrinks from both sides), with some area
    /// left, every edge still running its original way, not crossing itself, and no loop crossing
    /// another. A loop pulled in further than its narrowest part fails one of these; the edge test is
    /// what catches a corner that went through the far side and came out with the right area.
    /// </summary>
    /// <param name="loops">The original loops: the outside first, then the holes.</param>
    /// <param name="inset">The same loops pulled in.</param>
    internal static bool Fits(IReadOnlyList<Vector2[]> loops, IReadOnlyList<Vector2[]> inset)
    {
        for (var l = 0; l < loops.Count; l++)
        {
            var before = EarClipping.SignedArea(loops[l]);
            var after = EarClipping.SignedArea(inset[l]);
            var sameWay = MathF.Sign(before) == MathF.Sign(after);
            var shrinks = l == 0 ? MathF.Abs(after) < MathF.Abs(before) && MathF.Abs(after) > 1e-4f * MathF.Abs(before) : MathF.Abs(after) > MathF.Abs(before);

            if (!sameWay || !shrinks) return false;
            if (!PolygonChecks.KeepsEdgeDirections(loops[l], inset[l])) return false;
            if (PolygonChecks.SelfIntersects(inset[l])) return false;

            for (var m = 0; m < l; m++)
            {
                if (PolygonChecks.Cross(inset[l], inset[m])) return false;
            }
        }

        return true;
    }
}