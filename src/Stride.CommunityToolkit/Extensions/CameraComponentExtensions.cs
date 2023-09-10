using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Extensions;

public static class CameraComponentExtensions
{
    /// <summary>
    /// Returns near and far vector based on a ray going from camera through a screen point. The ray is in world space, starting on the near plane of the camera and going through position's (x,y) pixel coordinates on the screen.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenPointToRay(this CameraComponent cameraComponent, Vector2 mousePosition)
    {
        var validMousePosition = mousePosition;

        var invertedMatrix = Matrix.Invert(cameraComponent.ViewProjectionMatrix);

        Vector3 position;
        position.X = validMousePosition.X * 2f - 1f;
        position.Y = 1f - validMousePosition.Y * 2f;
        position.Z = 0f;

        Vector4 vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        position.Z = 1f;

        Vector4 vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        return (vectorNear, vectorFar);
    }
    /// <summary>
    /// Performs a raycasting operation from the specified CameraComponent's position through the specified screen position in world coordinates,
    /// and returns information about the hit result.
    /// </summary>
    /// <param name="Camera">The CameraComponent from which the ray should be cast.</param>
    /// <param name="component">The ScriptComponent which has the Simulation to run the Cast in.</param>
    /// <param name="screenPos">The screen position (in world coordinates) where the ray should be cast through.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is CollisionFilterGroups.DefaultFilter.</param>
    /// <param name="collisionFlags">Optional. The collision filter group flags to consider during the raycasting. Default is CollisionFilterGroupFlags.DefaultFilter.</param>
    /// <returns>A HitResult containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult RayCast(this CameraComponent Camera, ScriptComponent component, Vector2 screenPos, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        Matrix invViewProj = Matrix.Invert(Camera.ViewProjectionMatrix);
        // Reconstruct the projection-space position in the (-1, +1) range.
        //    Don't forget that Y is down in screen coordinates, but up in projection space
        Vector3 sPos;
        sPos.X = screenPos.X * 2f - 1f;
        sPos.Y = 1f - screenPos.Y * 2f;

        // Compute the near (start) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 0 (lying on the near plane)
        // We need to unproject it to world space
        sPos.Z = 0f;
        var vectorNear = Vector3.Transform(sPos, invViewProj);
        vectorNear /= vectorNear.W;

        // Compute the far (end) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 1 (lying on the far plane)
        // We need to unproject it to world space
        sPos.Z = 1f;
        var vectorFar = Vector3.Transform(sPos, invViewProj);
        vectorFar /= vectorFar.W;

        // Raycast from the point on the near plane to the point on the far plane and get the collision result
        return component.GetSimulation().Raycast(vectorNear.XYZ(), vectorFar.XYZ(), collisionGroups, collisionFlags);
    }
}