namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// How an <see cref="EntityTextComponent"/> decides where on the screen to draw.
/// </summary>
public enum TextPositionMode
{
    /// <summary>
    /// Follows the entity, by projecting its world position into screen space. The text is hidden
    /// when the entity is behind the camera or outside the view.
    /// </summary>
    /// <remarks>
    /// This is the mode for labelling things in the scene - a name over a character, a coordinate on
    /// a vertex, a damage number above whatever took the hit.
    /// </remarks>
    World,

    /// <summary>
    /// Draws at <see cref="EntityTextComponent.ScreenPosition"/>, in pixels from the top-left of the
    /// window, ignoring where the entity is.
    /// </summary>
    /// <remarks>
    /// The entity's position is not consulted at all, so the text is never culled for being out of
    /// view. Fixed pixels do not move when the window is resized, which makes this the wrong choice
    /// for anything anchored to an edge - use <see cref="Anchored"/> for that.
    /// </remarks>
    Screen,

    /// <summary>
    /// Snaps to a corner of the window, offset by <see cref="EntityTextComponent.Offset"/>.
    /// </summary>
    /// <remarks>
    /// This is the mode for a HUD. Because the corner is resolved against the window each frame, the
    /// text stays put when the window is resized, which fixed pixel positions do not.
    /// </remarks>
    Anchored
}