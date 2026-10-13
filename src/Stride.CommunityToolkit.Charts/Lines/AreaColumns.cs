using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Prepares the filled region between two polylines - the shaded area under a curve, or between two curves -
/// for drawing as a run of convex columns.
/// </summary>
/// <remarks>
/// The two polylines are sampled at the same positions, so they form <em>columns</em>: pairs of points, one
/// on each edge of the band. The strip between two consecutive columns is a convex quadrilateral (or two
/// triangles where the edges cross), which is all a filled area needs - no polygon triangulation, and
/// nothing the shape batch cannot fill. <see cref="Columns"/> clamps the pairs to a vertical range so the
/// fill stops at the chart's edge instead of spilling past it, and breaks the band wherever a point is
/// not finite or the column lies entirely outside the range - the same treatment
/// <see cref="PolylineClipping"/> gives a line. It is plain arithmetic on the points, so it is covered by
/// unit tests.
/// </remarks>
internal static class AreaColumns
{
    /// <summary>
    /// Turns two equal-length polylines into the runs of drawable columns between them: each column is a
    /// pair of points sharing a position along the band, clamped to <paramref name="yMin"/> and
    /// <paramref name="yMax"/>. A new run starts wherever the band is broken.
    /// </summary>
    /// <param name="upper">One edge of the band.</param>
    /// <param name="lower">The other edge, sampled at the same positions.</param>
    /// <param name="yMin">The bottom of the visible range; columns are clamped to it.</param>
    /// <param name="yMax">The top of the visible range; columns are clamped to it.</param>
    /// <returns>The runs of columns; each run has at least two, and there may be none at all.</returns>
    /// <exception cref="ArgumentNullException">If either edge is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If the edges differ in length, or the range is inverted.</exception>
    internal static List<List<(Vector3 Upper, Vector3 Lower)>> Columns(IReadOnlyList<Vector3> upper, IReadOnlyList<Vector3> lower, float yMin, float yMax)
    {
        ArgumentNullException.ThrowIfNull(upper);
        ArgumentNullException.ThrowIfNull(lower);

        if (upper.Count != lower.Count)
        {
            throw new ArgumentException("The two edges must have the same number of points.", nameof(lower));
        }

        if (yMax < yMin)
        {
            throw new ArgumentException("The range's maximum must not be smaller than its minimum.", nameof(yMax));
        }

        var runs = new List<List<(Vector3, Vector3)>>();
        var run = new List<(Vector3, Vector3)>();

        for (var i = 0; i < upper.Count; i++)
        {
            var a = upper[i];
            var b = lower[i];

            if (!IsFinite(a) || !IsFinite(b))
            {
                Flush(runs, run);
                continue;
            }

            // A column entirely above or below the visible range contributes nothing, and breaks the band
            // so the fill does not slide along the edge between two visible stretches
            var top = MathF.Max(a.Y, b.Y);
            var bottom = MathF.Min(a.Y, b.Y);

            if (bottom > yMax || top < yMin)
            {
                Flush(runs, run);
                continue;
            }

            a.Y = Math.Clamp(a.Y, yMin, yMax);
            b.Y = Math.Clamp(b.Y, yMin, yMax);

            run.Add((a, b));
        }

        Flush(runs, run);

        return runs;
    }

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    /// <summary>Ends a run; a lone column spans no strip, so it is dropped rather than kept.</summary>
    private static void Flush(List<List<(Vector3, Vector3)>> runs, List<(Vector3, Vector3)> run)
    {
        if (run.Count >= 2)
        {
            runs.Add([.. run]);
        }

        run.Clear();
    }
}