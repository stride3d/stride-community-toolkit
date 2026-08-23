using Stride.Core.Mathematics;

namespace Example_CubicleCalamity.Shared;

/// <summary>
/// A named set of cube colours.
/// </summary>
/// <param name="Name">Shown in the palette dropdown. Printable ASCII only - the debug text renderer
/// silently blanks anything else.</param>
/// <param name="Colours">The cube colours, in a fixed order.</param>
public sealed record ColourPalette(string Name, IReadOnlyList<Color> Colours);

/// <summary>
/// The palettes the board can be painted with, switchable at any time from the in-game dropdown.
/// </summary>
/// <remarks>
/// Every palette must hold the same number of colours, in a stable order: a live palette switch
/// repaints each cube by <em>index</em> - the third colour of the old palette becomes the third
/// colour of the new one - so groups survive the switch untouched. The colours should stay
/// distinguishable as emissive faces, which is a harder test than on paper: strong hue differences
/// work, brightness differences alone do not.
/// </remarks>
public static class ColourPalettes
{
    /// <summary>The original board: saturated primaries plus gold.</summary>
    public static readonly ColourPalette Classic = new("Classic",
        [Color.Red, Color.Green, Color.Blue, Color.DarkGoldenrod]);

    /// <summary>A gentler board: coral, mint, periwinkle and sand.</summary>
    public static readonly ColourPalette Soft = new("Soft",
        [new Color(255, 145, 140), new Color(140, 220, 170), new Color(135, 175, 255), new Color(250, 215, 120)]);

    /// <summary>
    /// Four colours from the Okabe-Ito palette, designed to stay distinguishable under the common
    /// forms of colour-vision deficiency: orange, sky blue, bluish green and vermillion.
    /// </summary>
    public static readonly ColourPalette HighVisibility = new("High visibility",
        [new Color(230, 159, 0), new Color(86, 180, 233), new Color(0, 158, 115), new Color(213, 94, 0)]);

    /// <summary>Every palette, in the order the dropdown offers them.</summary>
    public static readonly IReadOnlyList<ColourPalette> All = [Classic, Soft, HighVisibility];
}