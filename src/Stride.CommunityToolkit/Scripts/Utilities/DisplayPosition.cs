namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// Represents the possible positions on the screen where text or other UI elements can be displayed.
/// </summary>
public enum DisplayPosition
{
    /// <summary>
    /// Displays the element in the top-left corner of the screen.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Displays the element in the top-right corner of the screen.
    /// </summary>
    TopRight,

    /// <summary>
    /// Displays the element in the bottom-left corner of the screen.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Displays the element in the bottom-right corner of the screen.
    /// </summary>
    BottomRight,

    /// <summary>
    /// Does not display the element at all.
    /// </summary>
    /// <remarks>
    /// Lets a caller opt out up front, rather than only being able to hide the element at runtime.
    /// </remarks>
    None,

    /// <summary>
    /// Displays the element at an explicit pixel position rather than snapped to a corner.
    /// </summary>
    /// <remarks>
    /// The position itself is supplied separately - see <see cref="DebugOverlay.CustomPosition"/>.
    /// </remarks>
    Custom
}
