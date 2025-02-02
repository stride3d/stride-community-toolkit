using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Provides a set of static methods for working with <see cref="CameraComponent"/> instances.
/// </summary>
/// <remarks>
/// This class includes extension methods for performing various operations with <see cref="CameraComponent"/> instances,
/// such as raycasting, converting screen positions to world positions, and more. These methods are useful for implementing
/// features like object picking, camera control, and coordinate transformations in a 3D environment.
/// </remarks>
public static class CameraComponentExtensions
{
    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through the mouse cursor position in screen coordinates,
    /// using input from the specified <see cref="ScriptComponent"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="component">The <see cref="ScriptComponent"/> from which the mouse position should be taken.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is <see cref="CollisionFilterGroups.DefaultFilter"/>.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is <see cref="CollisionFilterGroupFlags.DefaultFilter"/>.</param>
    /// <returns>A <see cref="HitResult"/> containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult RaycastMouse(this CameraComponent camera, ScriptComponent component, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        return camera.Raycast(component, component.Input.MousePosition, collisionGroups, collisionFilterGroupFlags);
    }

    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through a specified screen position,
    /// using the provided <see cref="Simulation"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="simulation">The <see cref="Simulation"/> used to perform the raycasting operation.</param>
    /// <param name="screenPosition">The screen position in screen coordinates (e.g., mouse position) from which the ray should be cast.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is <see cref="CollisionFilterGroups.DefaultFilter"/>.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is <see cref="CollisionFilterGroupFlags.DefaultFilter"/>.</param>
    /// <returns>A <see cref="HitResult"/> containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult RaycastMouse(this CameraComponent camera, Simulation simulation, Vector2 screenPosition, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        return camera.Raycast(simulation, screenPosition, collisionGroups, collisionFilterGroupFlags);
    }

    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through the specified screen position in world coordinates,
    /// using the <see cref="Simulation"/> from the specified <see cref="ScriptComponent"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="component">The <see cref="ScriptComponent"/> that contains the <see cref="Simulation"/> used for raycasting.</param>
    /// <param name="screenPosition">The screen position in world coordinates through which the ray should be cast.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is <see cref="CollisionFilterGroups.DefaultFilter"/>.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is <see cref="CollisionFilterGroupFlags.DefaultFilter"/>.</param>
    /// <returns>A <see cref="HitResult"/> containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult Raycast(this CameraComponent camera, ScriptComponent component, Vector2 screenPosition, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
        => Raycast(camera, component.GetSimulation(), screenPosition, collisionGroups, collisionFilterGroupFlags);

    /// <summary>
    /// Performs a raycasting operation from the specified <see cref="CameraComponent"/>'s position through a specified screen position,
    /// using the provided <see cref="Simulation"/>, and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> from which the ray should be cast.</param>
    /// <param name="simulation">The <see cref="Simulation"/> used to perform the raycasting operation.</param>
    /// <param name="screenPosition">The screen position in normalized screen coordinates (e.g., mouse position) where the ray should be cast.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is <see cref="CollisionFilterGroups.DefaultFilter"/>.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is <see cref="CollisionFilterGroupFlags.DefaultFilter"/>.</param>
    /// <returns>A <see cref="HitResult"/> containing information about the raycasting hit, including the hit location and other collision data.</returns>
    /// <remarks>
    /// This method is useful for implementing features like object picking, where you want to select or interact with objects in the 3D world based on screen coordinates.
    /// </remarks>///
    public static HitResult Raycast(this CameraComponent camera, Simulation simulation, Vector2 screenPosition, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        var (nearPoint, farPoint) = camera.CalculateRayFromScreenPosition(screenPosition);

        // Perform the raycast from the near point to the far point and return the result
        return simulation.Raycast(nearPoint, farPoint, collisionGroups, collisionFilterGroupFlags);
    }
}