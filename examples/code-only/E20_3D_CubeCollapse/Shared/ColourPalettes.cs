using Example.Common;
using Stride.Core.Mathematics;

namespace CubeCollapse.Shared;

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

    /// <summary>
    /// The colour theme of the first examples and the playgrounds, from Example.Common: its blue,
    /// orange, red and green. Its purple is left out, as a board has four colours.
    /// </summary>
    public static readonly ColourPalette Examples = new("Examples",
        [ColorThemes.Default.Blue, ColorThemes.Default.Orange, ColorThemes.Default.Red, ColorThemes.Default.Green]);

    /// <summary>
    /// The material gallery's clear glass, and the same glass tinted pink, mint and amber. Harder to play:
    /// a glass cube shows the cubes behind it, so colours mix where they overlap.
    /// </summary>
    public static readonly ColourPalette Glass = new("Glass",
        [new Color(255, 205, 205), new Color(205, 255, 210), new Color(245, 250, 255), new Color(255, 240, 185)], Glass: true);

    /// <summary>
    /// The material gallery's frosted glass, its own pale grey and the same glass tinted rose, sage and
    /// sand: rough enough that the cubes behind blur into the light.
    /// </summary>
    public static readonly ColourPalette FrostedGlass = new("Frosted glass",
        [new Color(240, 205, 205), new Color(205, 240, 210), new Color(225, 230, 235), new Color(240, 230, 195)], Glass: true, Glossiness: 0.45f);


    /// <summary>Every palette, in the order the dropdown offers them.</summary>
    public static readonly IReadOnlyList<ColourPalette> All = [Classic, Soft, HighVisibility, Examples, Glass, FrostedGlass];
}