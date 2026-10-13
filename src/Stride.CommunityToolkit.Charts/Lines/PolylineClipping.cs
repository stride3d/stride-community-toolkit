using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Cuts a sampled polyline into the pieces that can be drawn: the runs that stay inside a rectangle or box,
/// with breaks at points that are not finite and, optionally, at jumps too large to be part of a continuous
/// curve.
/// </summary>
/// <remarks>
/// <para>
/// A function sampled over a range produces points a plotter cannot draw as they are. <c>log(x)</c> is
/// <c>NaN</c> for negative <c>x</c>, <c>1/x</c> is huge next to zero, and <c>tan(x)</c> jumps from a large
/// positive value to a large negative one across each asymptote - which, joined by a straight segment, draws
/// a spurious near-vertical line through the chart. Everything here is plain geometry on the points; nothing
/// touches the GPU, so it is covered by unit tests.
/// </para>
/// <para>
/// Clipping is Liang-Barsky per segment. A segment that leaves the region ends its run exactly on the edge
/// and a segment that enters starts a new one there, so curves meet the chart border cleanly instead of
/// stopping at the last sample inside it. <c>Z</c> is interpolated along with <c>X</c> and <c>Y</c>, so a
/// parametric curve that leaves the chart plane keeps its depth.
/// </para>
/// </remarks>
internal static class PolylineClipping
{
    /// <summary>
    /// A half-extent large enough to make an axis effectively unbounded in a clip box while staying far from
    /// float overflow - how the rectangle overloads express "any Z".
    /// </summary>
    internal const float UnboundedZ = 1e30f;

    /// <summary>
    /// The runs of <paramref name="points"/> that lie inside the rectangle
    /// [<paramref name="xMin"/>, <paramref name="xMax"/>] × [<paramref name="yMin"/>, <paramref name="yMax"/>].
    /// Points that are not finite break the line; runs shorter than two points are dropped.
    /// </summary>
    /// <param name="points">The polyline, in order.</param>
    /// <param name="xMin">The left edge.</param>
    /// <param name="xMax">The right edge.</param>
    /// <param name="yMin">The bottom edge.</param>
    /// <param name="yMax">The top edge.</param>
    /// <returns>Zero or more runs, each with at least two points, in the original order.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If a maximum is smaller than its minimum.</exception>
    internal static List<Vector3[]> Clip(IReadOnlyList<Vector3> points, float xMin, float xMax, float yMin, float yMax)
        => Clip(points, xMin, xMax, yMin, yMax, -UnboundedZ, UnboundedZ);

    /// <summary>
    /// The runs of <paramref name="points"/> that lie inside the box
    /// [<paramref name="xMin"/>, <paramref name="xMax"/>] × [<paramref name="yMin"/>, <paramref name="yMax"/>]
    /// × [<paramref name="zMin"/>, <paramref name="zMax"/>] - the 3D counterpart of the rectangle overload,
    /// for charts whose Z range is not degenerate. Points that are not finite break the line; runs shorter
    /// than two points are dropped.
    /// </summary>
    /// <param name="points">The polyline, in order.</param>
    /// <param name="xMin">The left face.</param>
    /// <param name="xMax">The right face.</param>
    /// <param name="yMin">The bottom face.</param>
    /// <param name="yMax">The top face.</param>
    /// <param name="zMin">The near face.</param>
    /// <param name="zMax">The far face.</param>
    /// <returns>Zero or more runs, each with at least two points, in the original order.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If a maximum is smaller than its minimum.</exception>
    internal static List<Vector3[]> Clip(IReadOnlyList<Vector3> points, float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (xMax < xMin || yMax < yMin || zMax < zMin)
        {
            throw new ArgumentException("The region's maximum must not be smaller than its minimum.");
        }

        var box = new BoundingBox(new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax));
        var runs = new List<Vector3[]>();
        var run = new List<Vector3>();

        for (var i = 0; i + 1 < points.Count; i++)
        {
            var a = points[i];
            var b = points[i + 1];

            if (!IsFinite(a) || !IsFinite(b) || !ClipSegment(a, b, in box, out var t0, out var t1))
            {
                Flush(runs, run);
                continue;
            }

            // Entering from outside starts a new run on the edge
            if (t0 > 0f)
            {
                Flush(runs, run);
            }

            if (run.Count == 0)
            {
                run.Add(Vector3.Lerp(a, b, t0));
            }

            Append(run, Vector3.Lerp(a, b, t1));

            // Leaving ends the run on the edge
            if (t1 < 1f)
            {
                Flush(runs, run);
            }
        }

        Flush(runs, run);

        return runs;
    }

    /// <summary>
    /// Breaks <paramref name="points"/> wherever a point is not finite - <c>NaN</c> or infinite, which is what
    /// a function returns outside its domain. Runs shorter than two points are dropped.
    /// </summary>
    /// <param name="points">The polyline, in order.</param>
    /// <returns>Zero or more runs, each with at least two finite points, in the original order.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    internal static List<Vector3[]> SplitAtNonFinite(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        var runs = new List<Vector3[]>();
        var run = new List<Vector3>();

        foreach (var point in points)
        {
            if (IsFinite(point))
                Append(run, point);
            else
                Flush(runs, run);
        }

        Flush(runs, run);

        return runs;
    }

    /// <summary>
    /// Breaks <paramref name="points"/> between two consecutive points whose vertical distance exceeds
    /// <paramref name="maxJump"/> while jumping across zero - the signature of an odd asymptote like those
    /// of <c>tan(x)</c> or <c>1/x</c> - and at points that are not finite. A big jump that stays on one side
    /// of zero is a genuinely steep stretch of the same branch and is kept connected. Runs shorter than two
    /// points are dropped.
    /// </summary>
    /// <remarks>
    /// A sampled function cannot tell an asymptote from a steep slope; all it sees is a big jump between two
    /// samples. Passing the visible height of the chart as <paramref name="maxJump"/> is the usual heuristic:
    /// a genuine segment that spans more than the whole chart in one sample step would be invisible anyway,
    /// while the jump across an asymptote of <c>tan(x)</c> or <c>1/x</c> always exceeds it.
    /// </remarks>
    /// <param name="points">The polyline, in order.</param>
    /// <param name="maxJump">The largest <c>|Δy|</c> between consecutive points that is still drawn as a segment.</param>
    /// <param name="extendEnds">
    /// When <see langword="true"/>, the branch before a jump is extended past it and the branch after it
    /// starts before it, both at the midpoint <c>x</c> - so once the runs are clipped to a chart, branches
    /// cut by an asymptote reach the chart edge instead of stopping at the last sample.
    /// </param>
    /// <returns>Zero or more runs, each with at least two finite points, in the original order.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="maxJump"/> is negative.</exception>
    internal static List<Vector3[]> SplitAtJumps(IReadOnlyList<Vector3> points, float maxJump, bool extendEnds = false)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentOutOfRangeException.ThrowIfNegative(maxJump);

        var runs = new List<Vector3[]>();
        var run = new List<Vector3>();

        foreach (var point in points)
        {
            if (!IsFinite(point))
            {
                Flush(runs, run);
                continue;
            }

            if (run.Count > 0 && MathF.Abs(point.Y - run[^1].Y) > maxJump && MathF.Sign(point.Y) != MathF.Sign(run[^1].Y))
            {
                // A jump this size is an asymptote the samples straddle. Optionally extend the ending
                // branch and the starting one vertically towards it, far enough that clipping to the
                // chart cuts them at the edge - so the branches of tan(x) or 1/x always span the full
                // view, however sparse the sampling near the pole happens to be.
                var previous = run[^1];
                var jumpSign = MathF.Sign(point.Y - previous.Y);
                var midX = (previous.X + point.X) * 0.5f;

                if (extendEnds)
                {
                    Append(run, new Vector3(midX, previous.Y - jumpSign * 2f * maxJump, previous.Z));
                }

                Flush(runs, run);

                if (extendEnds)
                {
                    Append(run, new Vector3(midX, point.Y + jumpSign * 2f * maxJump, point.Z));
                }
            }

            Append(run, point);
        }

        Flush(runs, run);

        return runs;
    }

    /// <summary>
    /// Liang-Barsky against a box: the parametric range [<paramref name="t0"/>, <paramref name="t1"/>] of
    /// the segment <paramref name="a"/>-<paramref name="b"/> that lies inside it, or <see langword="false"/>
    /// when the segment misses it. For a purely 2D clip, make the box unbounded in Z with
    /// ±<see cref="UnboundedZ"/>.
    /// </summary>
    /// <param name="a">The segment's start.</param>
    /// <param name="b">The segment's end.</param>
    /// <param name="box">The region to clip against.</param>
    /// <param name="t0">The parameter where the clipped piece starts; <c>0</c> when <paramref name="a"/> is inside.</param>
    /// <param name="t1">The parameter where the clipped piece ends; <c>1</c> when <paramref name="b"/> is inside.</param>
    /// <returns><see langword="true"/> when some piece of the segment lies inside the box.</returns>
    internal static bool ClipSegment(Vector3 a, Vector3 b, in BoundingBox box, out float t0, out float t1)
    {
        t0 = 0f;
        t1 = 1f;

        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var dz = b.Z - a.Z;

        return Narrow(-dx, a.X - box.Minimum.X, ref t0, ref t1)
            && Narrow(dx, box.Maximum.X - a.X, ref t0, ref t1)
            && Narrow(-dy, a.Y - box.Minimum.Y, ref t0, ref t1)
            && Narrow(dy, box.Maximum.Y - a.Y, ref t0, ref t1)
            && Narrow(-dz, a.Z - box.Minimum.Z, ref t0, ref t1)
            && Narrow(dz, box.Maximum.Z - a.Z, ref t0, ref t1);
    }

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    /// <summary>
    /// Adds a point unless it coincides with the previous one; a zero-length segment has no direction for
    /// the ribbon builder to work with.
    /// </summary>
    private static void Append(List<Vector3> run, Vector3 point)
    {
        if (run.Count > 0 && (point - run[^1]).LengthSquared() <= MathUtil.ZeroTolerance * MathUtil.ZeroTolerance)
            return;

        run.Add(point);
    }

    private static void Flush(List<Vector3[]> runs, List<Vector3> run)
    {
        if (run.Count >= 2)
        {
            runs.Add(run.ToArray());
        }

        run.Clear();
    }

    /// <summary>
    /// Narrows [<paramref name="t0"/>, <paramref name="t1"/>] against one edge: <c>p</c> is how fast the
    /// segment approaches the edge, <c>q</c> how far from it the segment starts.
    /// </summary>
    private static bool Narrow(float p, float q, ref float t0, ref float t1)
    {
        if (p == 0f)
        {
            // Parallel to this edge: entirely inside or entirely outside of it
            return q >= 0f;
        }

        var t = q / p;

        if (p < 0f)
        {
            if (t > t1) return false;
            if (t > t0) t0 = t;
        }
        else
        {
            if (t < t0) return false;
            if (t < t1) t1 = t;
        }

        return true;
    }
}