namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

/// <summary>
/// The vector shape of one character in a <see cref="MeshFont"/>: its contours as curves, how far the pen
/// advances after it, and its bounds. Everything is in EM units, baseline at y = 0, pen at x = 0, y up.
/// </summary>
public sealed class GlyphOutline
{
    private readonly GlyphContour[] _contours;

    /// <summary>
    /// Creates an outline.
    /// </summary>
    /// <param name="codepoint">The Unicode code point the outline was looked up for.</param>
    /// <param name="glyphIndex">The font's own index of the glyph; 0 is the font's "missing glyph" box.</param>
    /// <param name="advance">How far the pen moves right after this glyph, in EM units.</param>
    /// <param name="boundsMin">The bottom-left of the outline's bounds, in EM units.</param>
    /// <param name="boundsMax">The top-right of the outline's bounds, in EM units.</param>
    /// <param name="contours">The contours; none for a space.</param>
    /// <exception cref="ArgumentNullException">The contours are null.</exception>
    public GlyphOutline(int codepoint, uint glyphIndex, float advance, Vector2 boundsMin, Vector2 boundsMax, IEnumerable<GlyphContour> contours)
    {
        ArgumentNullException.ThrowIfNull(contours);

        Codepoint = codepoint;
        GlyphIndex = glyphIndex;
        Advance = advance;
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
        _contours = [.. contours];
    }

    /// <summary>The Unicode code point the outline was looked up for.</summary>
    public int Codepoint { get; }

    /// <summary>The font's own index of the glyph.</summary>
    public uint GlyphIndex { get; }

    /// <summary>Whether the font has no glyph for the code point and this is its "missing glyph" shape instead.</summary>
    public bool IsMissing => GlyphIndex == 0;

    /// <summary>How far the pen moves right after this glyph, in EM units.</summary>
    public float Advance { get; }

    /// <summary>The bottom-left of the outline's bounds, in EM units; zero when there is no outline.</summary>
    public Vector2 BoundsMin { get; }

    /// <summary>The top-right of the outline's bounds, in EM units; zero when there is no outline.</summary>
    public Vector2 BoundsMax { get; }

    /// <summary>The contours, as curves. A space has none; an A has two, the second being its hole.</summary>
    public IReadOnlyList<GlyphContour> Contours => _contours;

    /// <summary>Whether there is nothing to draw: a space, or a control character.</summary>
    public bool IsEmpty => _contours.Length == 0;

    /// <summary>
    /// Turns the curves into polygons and sorts out which contour is a hole in which: the filled shape of
    /// the glyph, ready to triangulate.
    /// </summary>
    /// <param name="curveQuality">How many points a full turn of curve gets; see <see cref="GlyphContour.Flatten"/>.</param>
    /// <returns>The glyph as outer polygons, each with its holes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The quality is not positive.</exception>
    public GlyphShape Flatten(int curveQuality = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(curveQuality);

        var polygons = new Vector2[_contours.Length][];

        for (var i = 0; i < _contours.Length; i++) polygons[i] = _contours[i].Flatten(curveQuality);

        return GlyphShape.FromContours(polygons);
    }
}