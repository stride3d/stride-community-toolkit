using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>Where each <see cref="ScreenCorner"/> is for a rectangle of a given size.</summary>
internal static class ScreenCornerExtensions
{
    /// <summary>The corner's position, in the coordinates of a rectangle whose top left is the origin.</summary>
    /// <param name="corner">Which corner.</param>
    /// <param name="size">The rectangle's width and height.</param>
    /// <returns>The corner, in the rectangle's own coordinates.</returns>
    internal static Vector2 At(this ScreenCorner corner, Vector2 size) => corner switch
    {
        ScreenCorner.TopRight => new Vector2(size.X, 0f),
        ScreenCorner.BottomLeft => new Vector2(0f, size.Y),
        ScreenCorner.BottomRight => size,
        _ => Vector2.Zero,
    };
}