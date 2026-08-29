namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Which point of a block of text is placed on the position it is drawn at.
/// </summary>
/// <remarks>
/// <para>
/// This is the setting people usually mean by "centre the text". It is not
/// <see cref="Stride.Graphics.TextAlignment"/>, which only decides how lines sit relative to each
/// other inside a multi-line block and does nothing at all to single-line text.
/// </para>
/// <para>
/// The names say where the anchor point sits on the text, so the text extends away from it:
/// <see cref="TopLeft"/> puts the point at the text's top-left corner and the text runs right and
/// down, while <see cref="BottomCenter"/> puts the point under the middle of the text, leaving the
/// text sitting above it - which is what a label floating over an object wants.
/// </para>
/// </remarks>
public enum TextAnchor
{
    /// <summary>Anchor at the text's top-left corner. The text runs right and down.</summary>
    TopLeft,

    /// <summary>Anchor at the middle of the text's top edge.</summary>
    TopCenter,

    /// <summary>Anchor at the text's top-right corner. The text runs left and down.</summary>
    TopRight,

    /// <summary>Anchor at the middle of the text's left edge.</summary>
    MiddleLeft,

    /// <summary>Anchor at the centre of the text, horizontally and vertically.</summary>
    MiddleCenter,

    /// <summary>Anchor at the middle of the text's right edge.</summary>
    MiddleRight,

    /// <summary>Anchor at the text's bottom-left corner. The text runs right and up.</summary>
    BottomLeft,

    /// <summary>Anchor under the middle of the text, leaving the text above the point.</summary>
    BottomCenter,

    /// <summary>Anchor at the text's bottom-right corner. The text runs left and up.</summary>
    BottomRight
}