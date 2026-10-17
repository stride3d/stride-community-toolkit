namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Reads FreeType's outline representation (points, a tag per point, the index of each contour's last
/// point) into <see cref="GlyphContour"/>s, by the three rules of its glyph conventions: two on-curve
/// points in a row make a line, one conic control between them makes a quadratic, two cubic controls
/// make a cubic. Two conic controls in a row imply an on-curve point half way between them, which is
/// how TrueType keeps round shapes short. This is what <c>FT_Outline_Decompose</c> does, without the
/// native callbacks.
/// </summary>
internal static class OutlineDecoder
{
    /// <summary>
    /// Decodes every contour.
    /// </summary>
    /// <param name="points">The points, already scaled to EM units.</param>
    /// <param name="tags">The tag of each point; only the low two bits are read.</param>
    /// <param name="contourEnds">The index of the last point of each contour.</param>
    /// <exception cref="InvalidDataException">The outline breaks the rules: a contour starts with a cubic control, or a cubic control stands alone.</exception>
    internal static GlyphContour[] Decode(ReadOnlySpan<Vector2> points, ReadOnlySpan<byte> tags, ReadOnlySpan<ushort> contourEnds)
    {
        var contours = new List<GlyphContour>(contourEnds.Length);
        var first = 0;

        foreach (var end in contourEnds)
        {
            if (end >= points.Length) throw new InvalidDataException("A contour ends past the last point of the outline.");

            if (end >= first && DecodeContour(points, tags, first, end) is { } contour) contours.Add(contour);

            first = end + 1;
        }

        return [.. contours];
    }

    /// <summary>One contour, or <see langword="null"/> for a contour of a single point, which some fonts carry and nothing can fill.</summary>
    private static GlyphContour? DecodeContour(ReadOnlySpan<Vector2> points, ReadOnlySpan<byte> tags, int first, int last)
    {
        var walk = new Walk(points, tags, first, last);
        var segments = new List<OutlineSegment>(last - first + 2);

        // Each turn reads one segment, from the point after the cursor to the next on-curve point, real
        // or implied; the walk says when the contour closed on its start inside a curve
        while (walk.Index < walk.Limit && !walk.Closed)
        {
            walk.Index++;

            switch (walk.Tag(walk.Index))
            {
                case FreeType.TagOn:
                    segments.Add(OutlineSegment.Line(walk.Point(walk.Index)));
                    break;

                case FreeType.TagConic:
                    ReadConics(ref walk, segments);
                    break;

                default:
                    ReadCubic(ref walk, segments);
                    break;
            }
        }

        // A single on-curve point makes no segment at all; it is a mark some fonts leave (Consolas has 364 of them)
        if (segments.Count == 0) return null;

        // A contour closes with a line back to its start unless a curve already ended there
        if (!walk.Closed && segments[^1].End != walk.Start) segments.Add(OutlineSegment.Line(walk.Start));

        return new GlyphContour(walk.Start, segments);
    }

    /// <summary>
    /// A conic control at the cursor, then as many as follow it: each pair of conic controls implies an
    /// on-curve point half way between them, and an on-curve point ends the run. A run that reaches the
    /// contour's last point ends on the start point instead.
    /// </summary>
    private static void ReadConics(ref Walk walk, List<OutlineSegment> segments)
    {
        var control = walk.Point(walk.Index);

        while (true)
        {
            if (walk.Index >= walk.Limit)
            {
                segments.Add(OutlineSegment.Quadratic(control, walk.Start));
                walk.Closed = true;
                return;
            }

            walk.Index++;

            var next = walk.Point(walk.Index);
            var tag = walk.Tag(walk.Index);

            if (tag == FreeType.TagOn)
            {
                segments.Add(OutlineSegment.Quadratic(control, next));
                return;
            }

            if (tag != FreeType.TagConic) throw new InvalidDataException("A cubic control point follows a conic one.");

            segments.Add(OutlineSegment.Quadratic(control, (control + next) * 0.5f));
            control = next;
        }
    }

    /// <summary>
    /// Two cubic controls at the cursor, then the on-curve point after them, or the contour's start when
    /// the controls are the contour's last two points.
    /// </summary>
    private static void ReadCubic(ref Walk walk, List<OutlineSegment> segments)
    {
        if (walk.Index + 1 > walk.Limit || walk.Tag(walk.Index + 1) != FreeType.TagCubic)
            throw new InvalidDataException("A cubic control point must be followed by a second one.");

        walk.Index += 2;

        var control1 = walk.Point(walk.Index - 2);
        var control2 = walk.Point(walk.Index - 1);

        if (walk.Index <= walk.Limit)
        {
            segments.Add(OutlineSegment.Cubic(control1, control2, walk.Point(walk.Index)));
            return;
        }

        segments.Add(OutlineSegment.Cubic(control1, control2, walk.Start));
        walk.Closed = true;
    }

    /// <summary>The cursor over one contour's points, with the start point FreeType's rules choose for it.</summary>
    private ref struct Walk
    {
        private readonly ReadOnlySpan<Vector2> _points;
        private readonly ReadOnlySpan<byte> _tags;

        internal Walk(ReadOnlySpan<Vector2> points, ReadOnlySpan<byte> tags, int first, int last)
        {
            _points = points;
            _tags = tags;
            Limit = last;
            Start = points[first];
            Index = first;

            var startTag = Tag(first);

            if (startTag == FreeType.TagCubic) throw new InvalidDataException("A contour cannot start with a cubic control point.");

            // The contour must begin on the curve. When it begins with a conic control, FreeType starts
            // from the last point if that is on the curve, or else from the implied point half way to
            // it, and the cursor steps back so the first control is read by the first turn
            if (startTag == FreeType.TagConic)
            {
                if (Tag(last) == FreeType.TagOn)
                {
                    Start = points[last];
                    Limit--;
                }
                else
                {
                    Start = (Start + points[last]) * 0.5f;
                }

                Index = first - 1;
            }
        }

        /// <summary>Where the contour starts and ends.</summary>
        internal Vector2 Start { get; }

        /// <summary>The last point index the walk may read.</summary>
        internal int Limit { get; private set; }

        /// <summary>The point last read.</summary>
        internal int Index { get; set; }

        /// <summary>Whether a curve already ended on <see cref="Start"/>.</summary>
        internal bool Closed { get; set; }

        internal readonly Vector2 Point(int index) => _points[index];

        internal readonly byte Tag(int index) => (byte)(_tags[index] & 3);
    }
}