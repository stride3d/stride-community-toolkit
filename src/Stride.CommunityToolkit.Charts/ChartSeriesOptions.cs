using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The defaults a series takes when it is added without a style of its own. All but
/// <see cref="MarkerSize"/>, <see cref="MarkerWidth"/> and <see cref="Palette"/> are read every frame, so
/// changing one changes every series that has no value of its own.
/// </summary>
public sealed class ChartSeriesOptions
{
    /// <summary>Stroke width of curves, lines and trajectories, in pixels on a 100% display. Defaults to <c>2.5</c>.</summary>
    public float CurveWidth { get; set; } = 2.5f;

    /// <summary>
    /// The glow halo around each stroke, in pixels, fading out from the stroke's edge; <c>0</c> for none.
    /// Defaults to <c>6</c>. Cheap to animate: it is a number the batch reads when the stroke is submitted.
    /// </summary>
    public float Glow { get; set; } = 6f;

    /// <summary>
    /// How bright the halo is where it meets the stroke, from <c>0</c> to <c>1</c> of the stroke's own colour.
    /// Defaults to <c>0.4</c>: a soft neon rather than a thicker line. Live, like <see cref="Glow"/>.
    /// </summary>
    public float GlowStrength { get; set; } = 0.4f;

    /// <summary>
    /// Whether the glow adds light to what is behind it - neon on a dark ground, brighter where curves
    /// cross - rather than covering it. Defaults to <see langword="true"/>; a chart on a light ground with a
    /// little glow wants <see langword="false"/>.
    /// </summary>
    public bool AdditiveGlow { get; set; } = true;

    /// <summary>Size of a scatter marker glyph in pixels, the same at any zoom or distance. Defaults to <c>8</c>.</summary>
    public float MarkerSize { get; set; } = 8f;

    /// <summary>Stroke width of a scatter marker glyph in pixels. Defaults to <c>1.5</c>.</summary>
    public float MarkerWidth { get; set; } = 1.5f;

    /// <summary>
    /// How opaque a shaded region is, from <c>0</c> to <c>1</c>; it multiplies the alpha of the region's
    /// colour, given or from the palette. Defaults to <c>0.25</c>.
    /// </summary>
    public float AreaOpacity { get; set; } = 0.25f;

    /// <summary>
    /// The colours series take in turn when added without one. Defaults to seven bright colours that read
    /// against a dark ground.
    /// </summary>
    public IReadOnlyList<Color> Palette { get; set; } =
    [
        Color.Cyan, Color.Orange, Color.Magenta, Color.Yellow, Color.LightGreen, Color.HotPink, Color.DeepSkyBlue,
    ];
}