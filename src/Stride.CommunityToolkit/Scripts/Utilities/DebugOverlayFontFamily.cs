namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// The kind of system font a <see cref="DebugOverlay"/> looks for when no <see cref="DebugOverlay.FontName"/> is set.
/// </summary>
public enum DebugOverlayFontFamily
{
    /// <summary>
    /// Every character the same width, like Stride's own debug font: columns line up, numbers do not
    /// jitter as their digits change, and text padded with spaces keeps its shape. Consolas on Windows,
    /// Menlo on macOS, DejaVu Sans Mono or Liberation Mono on Linux, with Courier New as the last resort.
    /// </summary>
    Monospace,

    /// <summary>
    /// A proportional font, which fits more text in the same width. Segoe UI on Windows, Helvetica on
    /// macOS, Liberation Sans or DejaVu Sans on Linux, with Arial as the last resort.
    /// </summary>
    SansSerif,
}