using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Physics;

/// <summary>
/// Provides extension methods for the <see cref="Simulation"/> class to perform raycasting operations in a game simulation.
/// </summary>
public static class SimulationExtensions
{
    /// <summary>
    /// Performs a raycast from the given entity's position in the direction the entity is facing, with the specified length.
    /// </summary>
    /// <param name="simulation">The <see cref="Simulation"/> instance in which the raycast is performed.</param>
    /// <param name="entityPosition">The <see cref="Entity"/> from which the ray starts. The ray is cast from the entity's current world position and direction.</param>
    /// <param name="length">The length of the ray, which determines how far it should extend from the entity. Defaults to 1.</param>
    /// <param name="collisionFlags">Specifies which collision groups to include in the raycast. Defaults to <see cref="CollisionFilterGroupFlags.AllFilter"/>.</param>
    /// <param name="eFlags">Additional raycasting flags for fine-tuning the behavior. Defaults to <see cref="EFlags.None"/>.</param>
    /// <returns>A <see cref="HitResult"/> that contains information about the first object hit by the ray, or an empty result if nothing is hit.</returns>
    /// <remarks>
    /// Ensure that you are using the actual rotating entity, as debugging with the wrong entity can lead to unexpected results.
    /// </remarks>
    public static HitResult Raycast(this Simulation simulation, Entity entityPosition, float length = 1, CollisionFilterGroupFlags collisionFlags = CollisionFilterGroupFlags.AllFilter, EFlags eFlags = EFlags.None)
    {
        var raycastStart = entityPosition.Transform.WorldMatrix.TranslationVector;
        var forward = entityPosition.Transform.WorldMatrix.Forward;
        var raycastEnd = raycastStart + forward * length;

        var result = simulation.Raycast(raycastStart, raycastEnd, filterFlags: collisionFlags, eFlags: eFlags);

        return result;
    }

    /// <summary>
    /// Performs a raycast from the given entity's position in the specified direction, with the specified length.
    /// </summary>
    /// <param name="simulation">The <see cref="Simulation"/> instance in which the raycast is performed.</param>
    /// <param name="entityPosition">The <see cref="Entity"/> from which the ray starts. The ray is cast from the entity's current world position.</param>
    /// <param name="direction">The direction in which the ray is cast.</param>
    /// <param name="length">The length of the ray, which determines how far it should extend from the entity. Defaults to 1.</param>
    /// <param name="collisionFlags">Specifies which collision groups to include in the raycast. Defaults to <see cref="CollisionFilterGroupFlags.AllFilter"/>.</param>
    /// <param name="eFlags">Additional raycasting flags for fine-tuning the behavior. Defaults to <see cref="EFlags.None"/>.</param>
    /// <returns>A <see cref="HitResult"/> that contains information about the first object hit by the ray, or an empty result if nothing is hit.</returns>
    public static HitResult Raycast(this Simulation simulation, Entity entityPosition, Vector3 direction, float length = 1, CollisionFilterGroupFlags collisionFlags = CollisionFilterGroupFlags.AllFilter, EFlags eFlags = EFlags.None)
    {
        var raycastStart = entityPosition.Transform.WorldMatrix.TranslationVector;
        var raycastEnd = raycastStart + direction * length;

        var result = simulation.Raycast(raycastStart, raycastEnd, filterFlags: collisionFlags, eFlags: eFlags);

        return result;
    }
}