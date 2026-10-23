namespace Stride.CommunityToolkit.Mathematics;

/// <summary>
/// The part of the colour space <see cref="RandomExtensions.NextColor"/> draws from. The hue is always
/// spread evenly round the wheel; the tone sets the saturation and the brightness.
/// </summary>
public enum ColorTone
{
    /// <summary>Saturated and bright: the colours that read well on a grey ground or against a dark background.</summary>
    Vivid,

    /// <summary>Pastels: light and lightly saturated, for fills and backgrounds behind dark text.</summary>
    Light,

    /// <summary>Deep and dim: for text and outlines on a light ground.</summary>
    Dark,

    /// <summary>Anywhere in the colour space, greys and muddy tones included.</summary>
    Any,
}