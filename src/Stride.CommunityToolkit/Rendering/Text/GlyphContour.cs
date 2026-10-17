namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// One closed curve of a glyph: a start point and the segments that lead back to it.
/// </summary>
/// <remarks>
/// Coordinates are EM units with the baseline at y = 0 and the pen at x = 0, y up: a capital letter is
/// about 0.7 high. The last segment always ends at <see cref="Start"/>. Whether the contour is the outside
/// of a shape or a hole in one is not known here; <see cref="GlyphOutline.Flatten"/> works it out from
/// how the contours nest.
/// </remarks>
public sealed class GlyphContour
{
    private readonly OutlineSegment[] _segments;

    /// <summary>
    /// Creates a contour.
    /// </summary>
    /// <param name="start">Where the first segment begins.</param>
    /// <param name="segments">The segments, the last of which ends at <paramref name="start"/>.</param>
    /// <exception cref="ArgumentNullException">The segments are null.</exception>
    /// <exception cref="ArgumentException">There are no segments, or the last one does not end at <paramref name="start"/>.</exception>
    public GlyphContour(Vector2 start, IEnumerable<OutlineSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _segments = [.. segments];

        if (_segments.Length == 0)
            throw new ArgumentException("A contour needs at least one segment.", nameof(segments));

        if (_segments[^1].End != start)
            throw new ArgumentException("The last segment of a contour must end where the contour starts.", nameof(segments));

        Start = start;
    }

    /// <summary>Where the first segment begins, and the last one ends.</summary>
    public Vector2 Start { get; }

    /// <summary>The segments in drawing order.</summary>
    public IReadOnlyList<OutlineSegment> Segments => _segments;

    /// <summary>
    /// Turns the curves into a closed polygon.
    /// </summary>
    /// <param name="curveQuality">
    /// How many points a full turn of curve gets, so a tight bowl gets more points than a shallow one and a
    /// straight run gets none: a curve that bends through a quarter turn gets a quarter of this many
    /// segments, and never fewer than one. 32 is smooth at letter sizes; 8 is visibly faceted.
    /// </param>
    /// <returns>
    /// The corners in order, without repeating the first at the end. Duplicate and collinear points are
    /// dropped, so a contour can come back with fewer than three points when it is degenerate.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The quality is not positive.</exception>
    public Vector2[] Flatten(int curveQuality = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(curveQuality);

        var points = new List<Vector2>(_segments.Length * 4) { Start };
        var previous = Start;

        foreach (var segment in _segments)
        {
            switch (segment.Kind)
            {
                case OutlineSegmentKind.Line:
                    points.Add(segment.End);
                    break;

                case OutlineSegmentKind.Quadratic:
                {
                    var steps = StepCount(segment.Control1 - previous, segment.End - segment.Control1, segment.End - previous, curveQuality);

                    for (var k = 1; k <= steps; k++)
                    {
                        var t = k / (float)steps;
                        var u = 1f - t;

                        points.Add(u * u * previous + 2f * u * t * segment.Control1 + t * t * segment.End);
                    }

                    break;
                }

                case OutlineSegmentKind.Cubic:
                {
                    var steps = CubicStepCount(previous, segment.Control1, segment.Control2, segment.End, curveQuality);

                    for (var k = 1; k <= steps; k++)
                    {
                        var t = k / (float)steps;
                        var u = 1f - t;

                        points.Add(u * u * u * previous + 3f * u * u * t * segment.Control1 + 3f * u * t * t * segment.Control2 + t * t * t * segment.End);
                    }

                    break;
                }
            }

            previous = segment.End;
        }

        return Clean(points);
    }

    /// <summary>
    /// The number of straight pieces a curve gets from the angle its tangents turn through, the rule
    /// ttf2mesh uses: a full turn gets <paramref name="quality"/> pieces, a straight run gets one.
    /// </summary>
    private static int StepCount(Vector2 tangentIn, Vector2 tangentOut, Vector2 chord, int quality)
    {
        // A control point on top of an end point gives no tangent; the chord stands in
        if (tangentIn.LengthSquared() < 1e-12f) tangentIn = chord;
        if (tangentOut.LengthSquared() < 1e-12f) tangentOut = chord;

        if (tangentIn.LengthSquared() < 1e-12f || tangentOut.LengthSquared() < 1e-12f) return 1;

        tangentIn.Normalize();
        tangentOut.Normalize();

        var angle = MathF.Acos(Math.Clamp(Vector2.Dot(tangentIn, tangentOut), -1f, 1f));

        return Math.Max(1, (int)MathF.Ceiling(angle / MathUtil.TwoPi * quality));
    }

    /// <summary>
    /// The number of straight pieces a cubic gets from the turn its tangent makes along the whole curve,
    /// sampled at eight places: a cubic can leave and arrive on the same heading and bend both ways
    /// between (a tilde), which the angle between its end tangents alone cannot see.
    /// </summary>
    private static int CubicStepCount(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int quality)
    {
        const int Samples = 8;

        var turn = 0f;
        var previous = Vector2.Zero;
        var hasPrevious = false;

        for (var k = 0; k <= Samples; k++)
        {
            var t = k / (float)Samples;
            var u = 1f - t;

            // The derivative of the cubic: the tangent at t
            var tangent = 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);

            if (tangent.LengthSquared() < 1e-12f) continue;

            tangent.Normalize();

            if (hasPrevious) turn += MathF.Acos(Math.Clamp(Vector2.Dot(previous, tangent), -1f, 1f));

            previous = tangent;
            hasPrevious = true;
        }

        return Math.Max(1, (int)MathF.Ceiling(turn / MathUtil.TwoPi * quality));
    }

    /// <summary>Drops the closing repeat of the start, then duplicate and collinear points.</summary>
    private static Vector2[] Clean(List<Vector2> points)
    {
        const float Epsilon = 1e-5f;

        var cleaned = new List<Vector2>(points.Count);

        foreach (var point in points)
        {
            if (cleaned.Count > 0 && (point - cleaned[^1]).LengthSquared() < Epsilon * Epsilon) continue;

            cleaned.Add(point);
        }

        while (cleaned.Count > 1 && (cleaned[^1] - cleaned[0]).LengthSquared() < Epsilon * Epsilon) cleaned.RemoveAt(cleaned.Count - 1);

        // A point on the straight line between its neighbours adds nothing but a wall of zero width
        for (var i = 0; i < cleaned.Count && cleaned.Count > 3;)
        {
            var previous = cleaned[(i + cleaned.Count - 1) % cleaned.Count];
            var next = cleaned[(i + 1) % cleaned.Count];
            var a = cleaned[i] - previous;
            var b = next - cleaned[i];

            if (MathF.Abs(a.X * b.Y - a.Y * b.X) < Epsilon * (a.Length() + b.Length()) && Vector2.Dot(a, b) > 0f)
            {
                cleaned.RemoveAt(i);

                if (i > 0) i--;

                continue;
            }

            i++;
        }

        return [.. cleaned];
    }
}