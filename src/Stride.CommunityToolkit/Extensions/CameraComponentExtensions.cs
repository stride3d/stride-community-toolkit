using Stride.CommunityToolkit.Scripts;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System;

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

    public static Vector3 LogicDirectionToWorldDirection(this CameraComponent camera, Vector2 logicDirection, Vector3 upVector)
    {
        camera.Update();
        var inverseView = Matrix.Invert(camera.ViewMatrix);

        var forward = Vector3.Cross(upVector, inverseView.Right);
        forward.Normalize();

        var right = Vector3.Cross(forward, upVector);
        var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
        worldDirection.Normalize();
        return worldDirection;
    }

    public static Vector3 LogicDirectionToWorldDirection(this CameraComponent camera, Vector2 logicDirection)
    {
        camera.Update();
        var upVector = Vector3.UnitY;
        var inverseView = Matrix.Invert(camera.ViewMatrix);

        var forward = Vector3.Cross(upVector, inverseView.Right);
        forward.Normalize();

        var right = Vector3.Cross(forward, upVector);
        var worldDirection = forward * logicDirection.Y + right * logicDirection.X;
        worldDirection.Normalize();
        return worldDirection;
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

    /// <summary>
    /// Performs a raycasting operation from the specified CameraComponent's position through the mouse cursor position in screen coordinates,
    /// and returns information about the hit result.
    /// </summary>
    /// <param name="Camera">The CameraComponent from which the ray should be cast.</param>
    /// <param name="component">The ScriptComponent from which the Input.MousePosition should be taken.</param>
    /// <param name="collisionGroup">Optional. The collision filter group to consider during the raycasting. Default is CollisionFilterGroups.DefaultFilter.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is CollisionFilterGroupFlags.DefaultFilter.</param>
    /// <returns>A HitResult containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult RayCastMouse(this CameraComponent Camera, ScriptComponent component, CollisionFilterGroups collisionGroup = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        return Camera.RayCast(component, component.Input.MousePosition, collisionGroup, collisionFilterGroupFlags);
    }

    /// <summary>
    /// Converts the world position to clip space coordinates relative to camera.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position"></param>
    /// <param name="result">The position in clip space.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static Vector3 WorldToClipSpace(this CameraComponent cameraComponent, ref Vector3 position)
    {
        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewProjectionMatrix, out var result);
        return result;
    }
    /// <summary>
    /// Converts the world position to screen space coordinates relative to camera.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position"></param>
    /// <returns>
    /// The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position)
    {
        cameraComponent.WorldToScreenPoint(ref position, out var result);
        return result;
    }
    /// <summary>
    /// Converts the world position to screen space coordinates relative to camera.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position"></param>
    /// <param name="result">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        var clipSpace = cameraComponent.WorldToClipSpace(ref position);

        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewMatrix, out var viewSpace);

        result = new Vector3
        {
            X = (clipSpace.X + 1f) / 2f,
            Y = 1f - (clipSpace.Y + 1f) / 2f,
            Z = viewSpace.Z + cameraComponent.NearClipPlane,
        };
    }
    /// <summary>
    /// Converts the screen position to a <see cref="RaySegment"/> in world coordinates.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position"></param>
    /// <returns><see cref="RaySegment"/>, starting at near plain and ending at the far plain.</returns>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static RaySegment ScreenToWorldRaySegment(this CameraComponent cameraComponent, Vector2 position)
    {
        cameraComponent.ScreenToWorldRaySegment(ref position, out var result);
        return result;
    }
    /// <summary>
    /// Converts the screen position to a <see cref="RaySegment"/> in world coordinates.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position"></param>
    /// <param name="result"><see cref="RaySegment"/>, starting at near plain and ending at the far plain.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void ScreenToWorldRaySegment(this CameraComponent cameraComponent, ref Vector2 position, out RaySegment result)
    {
        if (cameraComponent == null)
        {
            throw new ArgumentNullException(nameof(cameraComponent));
        }
        Matrix.Invert(ref cameraComponent.ViewProjectionMatrix, out var inverseViewProjection);

        ScreenToClipSpace(ref position, out var clipSpace);

        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var near);

        clipSpace.Z = 1f;
        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var far);

        result = new RaySegment(near, far);
    }
    private static void ScreenToClipSpace(ref Vector2 position, out Vector3 clipSpace)
    {
        clipSpace = new Vector3
        {
            X = position.X * 2f - 1f,
            Y = 1f - position.Y * 2f,
            Z = 0f
        };
    }

    /// <summary>
    /// Converts the screen position to a point in world coordinates.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
    /// <returns>Position in world coordinates.</returns>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, Vector3 position)
    {
        cameraComponent.ScreenToWorldPoint(ref position, out var result);

        return result;
    }

    /// <summary>
    /// Converts the screen position to a point in world coordinates.
    /// </summary>
    /// <param name="cameraComponent"></param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
    /// <param name="result">Position in world coordinates.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        var position2D = position.XY();

        cameraComponent.ScreenToWorldRaySegment(ref position2D, out var ray);

        var direction = ray.End - ray.Start;
        direction.Normalize();

        Vector3.TransformNormal(ref direction, ref cameraComponent.ViewMatrix, out var viewSpaceDir);

        float rayDistance = (position.Z / viewSpaceDir.Z);

        result = ray.Start + (direction * rayDistance);
    }
}