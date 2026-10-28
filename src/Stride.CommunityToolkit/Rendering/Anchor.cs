namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// A point of a rectangle: the four corners, the middle of each edge and the centre. The rectangle is whatever
/// is being anchored: the screen or a panel, for where a HUD element, an overlay or anchored text is pinned so
/// it stays put when the window is resized; a block of text, for which point of it sits on the position it is
/// drawn at; or a <see cref="TextureCanvas"/> destination, for how a source is aligned inside it.
/// </summary>
/// <remarks>
/// <para>
/// For text this is the setting people usually mean by "centre the text". It is not
/// <see cref="Stride.Graphics.TextAlignment"/>, which only decides how lines sit relative to each other inside
/// a multi-line block and does nothing at all to single-line text.
/// </para>
/// <para>
/// The names say where the anchor point sits, so the content extends away from it: <see cref="TopLeft"/> puts
/// the point at the top-left corner and the content runs right and down, while <see cref="BottomCenter"/> puts
/// the point under the middle, leaving the content above it, which is what a label floating over an object wants.
/// </para>
/// <para>
/// The members are laid out as a 3 by 3 grid row by row, so the ordinal gives the column and the row;
/// <see cref="AnchorExtensions.At"/> turns one into a position.
/// </para>
/// </remarks>
public enum Anchor
{
    /// <summary>The top-left corner. Content runs right and down.</summary>
    TopLeft,

    /// <summary>The middle of the top edge.</summary>
    TopCenter,

    /// <summary>The top-right corner. Content runs left and down.</summary>
    TopRight,

    /// <summary>The middle of the left edge.</summary>
    MiddleLeft,

    /// <summary>The centre, horizontally and vertically.</summary>
    MiddleCenter,

    /// <summary>The middle of the right edge.</summary>
    MiddleRight,

    /// <summary>The bottom-left corner. Content runs right and up.</summary>
    BottomLeft,

    /// <summary>The middle of the bottom edge, leaving the content above the point.</summary>
    BottomCenter,

    /// <summary>The bottom-right corner. Content runs left and up.</summary>
    BottomRight
}