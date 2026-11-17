namespace Stride.CommunityToolkit.Rendering.Overlay;

/// <summary>
/// The kind of system font a <see cref="TextOverlay"/> looks for when no <see cref="TextOverlay.Font"/> is set.
/// </summary>
public enum TextOverlayFontFamily
{
    /// <summary>
    /// Every character the same width, like Stride's own debug font: columns line up, numbers do not
    /// jitter as their digits change, and text padded with spaces keeps its shape. Consolas on Windows,
    /// Menlo on macOS, DejaVu Sans Mono or Liberation Mono on Linux; <see cref="Text.SystemFonts.MonospaceCandidates"/>
    /// is the whole list, in the order it is tried.
    /// </summary>
    Monospace,

    /// <summary>
    /// A proportional font, which fits more text in the same width. Segoe UI on Windows, Helvetica on
    /// macOS, Liberation Sans or DejaVu Sans on Linux; <see cref="Text.SystemFonts.SansSerifCandidates"/> is the
    /// whole list, in the order it is tried.
    /// </summary>
    SansSerif,
}