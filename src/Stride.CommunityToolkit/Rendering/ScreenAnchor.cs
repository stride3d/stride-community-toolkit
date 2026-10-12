namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// A point of the screen, or of any rectangle, named the way <see cref="Text.TextAnchor"/> names the point of a text
/// block: the four corners, the middle of each edge and the centre. Where a HUD element, an overlay or anchored text is
/// pinned, so it stays put when the window is resized.
/// </summary>
public enum ScreenAnchor
{
    /// <summary>The top-left corner.</summary>
    TopLeft,

    /// <summary>The middle of the top edge.</summary>
    TopCenter,

    /// <summary>The top-right corner.</summary>
    TopRight,

    /// <summary>The middle of the left edge.</summary>
    MiddleLeft,

    /// <summary>The centre.</summary>
    MiddleCenter,

    /// <summary>The middle of the right edge.</summary>
    MiddleRight,

    /// <summary>The bottom-left corner.</summary>
    BottomLeft,

    /// <summary>The middle of the bottom edge.</summary>
    BottomCenter,

    /// <summary>The bottom-right corner.</summary>
    BottomRight
}