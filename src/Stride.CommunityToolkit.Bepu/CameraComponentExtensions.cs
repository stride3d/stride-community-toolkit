using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides extension methods for <see cref="CameraComponent"/> to facilitate raycasting functionality.
/// </summary>
public static class CameraComponentExtensions
{
    /// <summary>
    /// Performs a raycast using the mouse's screen position.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used to calculate the ray.</param>
    /// <param name="component">The <see cref="ScriptComponent"/> providing access to the mouse position.</param>
    /// <param name="maxDistance">The maximum distance for the raycast.</param>
    /// <param name="hit">
    /// When this method returns, contains the <see cref="HitInfo"/> if the raycast hits a collider, otherwise <c>null</c>.
    /// </param>
    /// <param name="collisionMask">
    /// Specifies the collision mask to filter which objects the raycast can hit. Defaults to <see cref="CollisionMask.Everything"/>.
    /// </param>
    /// <returns><c>true</c> if the raycast hit a collider; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="camera"/> or <paramref name="component"/> is <c>null</c>.</exception>

    public static bool RaycastMouse(this CameraComponent camera, ScriptComponent component, float maxDistance, out HitInfo hit, CollisionMask collisionMask = CollisionMask.Everything)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(component);

        return camera.Raycast(component.Input.MousePosition, maxDistance, out hit, collisionMask);
    }

    /// <summary>
    /// Performs a raycast using a screen position.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used to calculate the ray.</param>
    /// <param name="screenPosition">The screen position in normalized device coordinates (NDC).</param>
    /// <param name="maxDistance">The maximum distance for the raycast.</param>
    /// <param name="hit">
    /// When this method returns, contains the <see cref="HitInfo"/> if the raycast hits a collider, otherwise <c>null</c>.
    /// </param>
    /// <param name="collisionMask">
    /// Specifies the collision mask to filter which objects the raycast can hit. Defaults to <see cref="CollisionMask.Everything"/>.
    /// </param>
    /// <returns><c>true</c> if the raycast hit a collider; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="camera"/> is <c>null</c>.</exception>

    public static bool Raycast(this CameraComponent camera, Vector2 screenPosition, float maxDistance, out HitInfo hit, CollisionMask collisionMask = CollisionMask.Everything)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var (nearPoint, farPoint) = camera.CalculateRayFromScreenPosition(screenPosition);

        return camera.Entity.GetSimulation().RayCast(nearPoint, farPoint, maxDistance, out hit, collisionMask);
    }
}