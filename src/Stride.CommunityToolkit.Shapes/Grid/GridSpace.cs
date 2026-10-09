namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The coordinates a <see cref="ReferenceGrid"/> shows.
/// </summary>
public enum GridSpace
{
    /// <summary>
    /// World coordinates: the units models and physics bodies are placed in. The origin is the
    /// world's origin and Y points up.
    /// </summary>
    World,

    /// <summary>
    /// Screen coordinates: the pixels a <see cref="ShapeBatch"/> draws in with
    /// <see cref="ShapeBatch.Screen"/> on. The origin is the top left corner of the window and Y
    /// points down.
    /// </summary>
    Screen,
}