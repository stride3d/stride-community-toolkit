namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// The kind of curve an <see cref="OutlineSegment"/> draws.
/// </summary>
public enum OutlineSegmentKind
{
    /// <summary>A straight line to <see cref="OutlineSegment.End"/>.</summary>
    Line,

    /// <summary>A quadratic Bézier through <see cref="OutlineSegment.Control1"/>, which is what TrueType glyphs are made of.</summary>
    Quadratic,

    /// <summary>A cubic Bézier through <see cref="OutlineSegment.Control1"/> and <see cref="OutlineSegment.Control2"/>, which is what CFF (OpenType) glyphs are made of.</summary>
    Cubic,
}