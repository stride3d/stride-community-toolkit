namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// A corner of the screen, or of the rectangle a batch is drawing into - what
/// <see cref="ShapeBatch.Corner"/> hands back a pixel position for, so a widget is placed as a corner
/// plus an offset rather than by a hardcoded resolution.
/// </summary>
public enum ScreenCorner
{
    /// <summary>The origin of screen coordinates.</summary>
    TopLeft,

    /// <summary>The top-right corner.</summary>
    TopRight,

    /// <summary>The bottom-left corner.</summary>
    BottomLeft,

    /// <summary>The bottom-right corner.</summary>
    BottomRight,
}