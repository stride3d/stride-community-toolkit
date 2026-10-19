namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

/// <summary>
/// One piece of a <see cref="GlyphContour"/>: a line or a Bézier curve from the previous segment's end to <see cref="End"/>.
/// </summary>
/// <param name="Kind">The kind of curve.</param>
/// <param name="Control1">The control point of a quadratic, or the first of a cubic; unused for a line.</param>
/// <param name="Control2">The second control point of a cubic; unused otherwise.</param>
/// <param name="End">Where the segment ends, in EM units.</param>
public readonly record struct OutlineSegment(OutlineSegmentKind Kind, Vector2 Control1, Vector2 Control2, Vector2 End)
{
    /// <summary>A straight line to <paramref name="end"/>.</summary>
    public static OutlineSegment Line(Vector2 end) => new(OutlineSegmentKind.Line, default, default, end);

    /// <summary>A quadratic Bézier through <paramref name="control"/> to <paramref name="end"/>.</summary>
    public static OutlineSegment Quadratic(Vector2 control, Vector2 end) => new(OutlineSegmentKind.Quadratic, control, default, end);

    /// <summary>A cubic Bézier through <paramref name="control1"/> and <paramref name="control2"/> to <paramref name="end"/>.</summary>
    public static OutlineSegment Cubic(Vector2 control1, Vector2 control2, Vector2 end) => new(OutlineSegmentKind.Cubic, control1, control2, end);
}