namespace Stride.CommunityToolkit.Rendering;

/// <summary>Where each <see cref="ScreenAnchor"/> is for a rectangle of a given size.</summary>
public static class ScreenAnchorExtensions
{
    /// <summary>The anchor's position, in the coordinates of a rectangle whose top left is the origin.</summary>
    /// <param name="anchor">Which point of the rectangle.</param>
    /// <param name="size">The rectangle's width and height.</param>
    /// <returns>The point, in the rectangle's own coordinates: a corner, the middle of an edge or the centre.</returns>
    public static Vector2 At(this ScreenAnchor anchor, Vector2 size)
        => new(size.X * HorizontalFactor(anchor), size.Y * VerticalFactor(anchor));

    private static float HorizontalFactor(ScreenAnchor anchor) => anchor switch
    {
        ScreenAnchor.TopCenter or ScreenAnchor.MiddleCenter or ScreenAnchor.BottomCenter => 0.5f,
        ScreenAnchor.TopRight or ScreenAnchor.MiddleRight or ScreenAnchor.BottomRight => 1f,
        _ => 0f,
    };

    private static float VerticalFactor(ScreenAnchor anchor) => anchor switch
    {
        ScreenAnchor.MiddleLeft or ScreenAnchor.MiddleCenter or ScreenAnchor.MiddleRight => 0.5f,
        ScreenAnchor.BottomLeft or ScreenAnchor.BottomCenter or ScreenAnchor.BottomRight => 1f,
        _ => 0f,
    };
}