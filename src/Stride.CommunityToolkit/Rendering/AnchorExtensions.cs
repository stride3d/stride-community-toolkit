namespace Stride.CommunityToolkit.Rendering;

/// <summary>Where each <see cref="Anchor"/> is for a rectangle of a given size.</summary>
public static class AnchorExtensions
{
    /// <summary>The anchor's position, in the coordinates of a rectangle whose top left is the origin.</summary>
    /// <param name="anchor">Which point of the rectangle.</param>
    /// <param name="size">The rectangle's width and height.</param>
    /// <returns>The point, in the rectangle's own coordinates: a corner, the middle of an edge or the centre.</returns>
    /// <remarks>The enum is laid out as a 3 by 3 grid row by row, so the ordinal gives the column and the row.</remarks>
    public static Vector2 At(this Anchor anchor, Vector2 size)
        => new(size.X * ((int)anchor % 3) * 0.5f, size.Y * ((int)anchor / 3) * 0.5f);
}