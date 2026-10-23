namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// What kind of physics a created primitive gets from the engine the scene setup chose.
/// </summary>
/// <remarks>
/// Read by <c>CreatePrimitive</c>, the ground helpers and the physics packages' entity extensions
/// when the options supply no component of their own. A supplied component wins, and a supplied
/// component with <see cref="None"/> is refused, since the two contradict each other.
/// </remarks>
public enum BodyMode
{
    /// <summary>A body with the collider fitted to the shape, moved by forces and collisions. The default.</summary>
    Dynamic,

    /// <summary>An immovable collider fitted to the shape: floors, walls, the ground.</summary>
    Static,

    /// <summary>A body moved by its velocities alone, which pushes dynamic bodies and is never pushed back: lifts, doors, platforms.</summary>
    Kinematic,

    /// <summary>No physics at all: a model and nothing else, whatever the setup chose.</summary>
    None,
}