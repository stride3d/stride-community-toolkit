using Stride.CommunityToolkit.Rendering.Text;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text.Meshes;

/// <summary>
/// How <see cref="TextMesh"/> sizes, extrudes and lays out a string.
/// </summary>
public sealed class TextMeshOptions
{
    /// <summary>
    /// World units per EM: the height a font's design grid maps to, so a capital letter comes out about
    /// 0.7 of this tall. Defaults to 1.
    /// </summary>
    public float Size { get; set; } = 1f;

    /// <summary>How far the lettering extends along Z, centred on zero. Defaults to 0.25.</summary>
    public float Depth { get; set; } = 0.25f;

    /// <summary>
    /// A rounded edge between the caps and the walls, front and back; <see langword="null"/>, the default,
    /// is a sharp edge. See <see cref="TextBevel"/> for the size, the thickness and the steps.
    /// </summary>
    public TextBevel? Bevel { get; set; }

    /// <summary>
    /// How many points a full turn of curve gets; see <see cref="GlyphContour.Flatten"/>. Defaults to 32,
    /// which is smooth at letter sizes. Lower it for far-away or tiny lettering, raise it for a logo
    /// the camera gets close to.
    /// </summary>
    public int CurveQuality { get; set; } = 32;

    /// <summary>
    /// Which point of the text block sits at the origin. <see langword="null"/>, the default, is the
    /// typographic origin: the baseline of the first line at y = 0 and the pen at x = 0. The block is
    /// the font's ascent to its descent over every line, so the anchor does not move with the letters
    /// used.
    /// </summary>
    public TextAnchor? Anchor { get; set; }

    /// <summary>Extra space after every glyph, in EM units; negative tightens. Defaults to 0.</summary>
    public float LetterSpacing { get; set; }

    /// <summary>The distance between lines as a multiple of the font's own line height. Defaults to 1.</summary>
    public float LineSpacing { get; set; } = 1f;

    /// <summary>How lines of different widths line up with each other. Defaults to left.</summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Whether to apply the font's pair kerning (AV, To). Defaults to on; it only takes effect for fonts
    /// with a kern table, see <see cref="MeshFont.GetKerning"/>.
    /// </summary>
    public bool Kerning { get; set; } = true;
}