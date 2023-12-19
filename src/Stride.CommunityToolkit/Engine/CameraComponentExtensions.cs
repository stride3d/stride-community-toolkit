using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Engine;

public static class CameraComponentExtensions
{
    /// <summary>
    /// Calculates a ray from the camera through a point on the screen in world space.
    /// </summary>
    /// <param name="camera">The camera component used for the calculation.</param>
    /// <param name="screenPosition">The position on the screen, typically the mouse position, normalized between (0,0) and (1,1).</param>
    /// <returns>A <see cref="Ray"/> starting from the camera and pointing into the scene through the specified screen position.</returns>
    /// <remarks>
    /// This method is useful for implementing features like object picking where you want to select or interact with objects in the 3D world based on screen coordinates.
    /// </remarks>
    public static Ray GetPickRay(this CameraComponent camera, Vector2 screenPosition)
    {
        var invertedMatrix = Matrix.Invert(camera.ViewProjectionMatrix);

        Vector3 position;
        position.X = screenPosition.X * 2f - 1f;
        position.Y = 1f - screenPosition.Y * 2f;
        position.Z = 0f;

        var vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        position.Z = 1f;

        var vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        Vector3 dir = vectorFar.XYZ() - vectorNear.XYZ();

        dir.Normalize();

        return new Ray(vectorNear.XYZ(), dir);
    }

    /// <summary>
    /// Converts a 2D logical direction into a 3D world direction relative to the camera's orientation.
    /// </summary>
    /// <param name="camera">The camera component used for the calculation.</param>
    /// <param name="logicDirection">The 2D logical direction (e.g., input from a joystick or keyboard), typically normalized between -1 and 1.</param>
    /// <returns>The corresponding 3D world direction vector.</returns>
    /// <remarks>
    /// This method is useful for converting 2D input commands into 3D movements in the world, taking into account the camera's current orientation.
    /// </remarks>
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
    /// Converts a 2D logical direction into a 3D world direction relative to the camera's orientation, using a specified up vector.
    /// </summary>
    /// <param name="camera">The camera component used for the calculation.</param>
    /// <param name="logicDirection">The 2D logical direction (e.g., input from a joystick or keyboard), typically normalized between -1 and 1.</param>
    /// <param name="upVector">The up vector to be used for the calculation, defining the vertical direction in world space.</param>
    /// <returns>The corresponding 3D world direction vector.</returns>
    /// <remarks>
    /// This method is useful for converting 2D input commands into 3D movements in the world, taking into account the camera's current orientation and a custom vertical orientation.
    /// </remarks>
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

    /// <summary>
    /// Performs a raycasting operation from the specified CameraComponent's position through the specified screen position in world coordinates,
    /// and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The CameraComponent from which the ray should be cast.</param>
    /// <param name="component">The ScriptComponent which has the Simulation to run the Cast in.</param>
    /// <param name="screenPosition">The screen position (in world coordinates) where the ray should be cast through.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is CollisionFilterGroups.DefaultFilter.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is CollisionFilterGroupFlags.DefaultFilter.</param>
    /// <returns>A HitResult containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult Raycast(this CameraComponent camera, ScriptComponent component, Vector2 screenPosition, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        var invertedMatrix = Matrix.Invert(camera.ViewProjectionMatrix);

        // Reconstruct the projection-space position in the (-1, +1) range.
        //    Don't forget that Y is down in screen coordinates, but up in projection space
        Vector3 position;
        position.X = screenPosition.X * 2f - 1f;
        position.Y = 1f - screenPosition.Y * 2f;

        // Compute the near (start) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 0 (lying on the near plane)
        // We need to unproject it to world space
        position.Z = 0f;

        var vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        // Compute the far (end) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 1 (lying on the far plane)
        // We need to unproject it to world space
        position.Z = 1f;

        var vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        // Raycast from the point on the near plane to the point on the far plane and get the collision result
        return component.GetSimulation().Raycast(vectorNear.XYZ(), vectorFar.XYZ(), collisionGroups, collisionFilterGroupFlags);
    }

    /// <summary>
    /// Performs a raycasting operation from the specified CameraComponent's position through the mouse cursor position in screen coordinates,
    /// and returns information about the hit result.
    /// </summary>
    /// <param name="camera">The CameraComponent from which the ray should be cast.</param>
    /// <param name="component">The ScriptComponent from which the Input.MousePosition should be taken.</param>
    /// <param name="collisionGroups">Optional. The collision filter group to consider during the raycasting. Default is CollisionFilterGroups.DefaultFilter.</param>
    /// <param name="collisionFilterGroupFlags">Optional. The collision filter group flags to consider during the raycasting. Default is CollisionFilterGroupFlags.DefaultFilter.</param>
    /// <returns>A HitResult containing information about the hit result, including the hit location and other collision data.</returns>
    public static HitResult RaycastMouse(this CameraComponent camera, ScriptComponent component, CollisionFilterGroups collisionGroups = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags collisionFilterGroupFlags = CollisionFilterGroupFlags.DefaultFilter)
    {
        return camera.Raycast(component, component.Input.MousePosition, collisionGroups, collisionFilterGroupFlags);
    }

    /// <summary>
    /// Calculates the near and far vectors for a ray that starts at the camera and passes through a given screen point.
    /// The ray is in world space, starting at the near plane of the camera and extending through the specified pixel coordinates on the screen.
    /// </summary>
    /// <param name="camera">The camera component used to calculate the ray.</param>
    /// <param name="screenPosition">The screen position (in normalized coordinates) through which the ray passes.</param>
    /// <returns>A tuple containing the near vector (<c>VectorNear</c>) and the far vector (<c>VectorFar</c>) of the ray in world space.</returns>
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenPointToRay(this CameraComponent camera, Vector2 screenPosition)
    {
        var invertedMatrix = Matrix.Invert(camera.ViewProjectionMatrix);

        Vector3 position;
        position.X = screenPosition.X * 2f - 1f;
        position.Y = 1f - screenPosition.Y * 2f;
        position.Z = 0f;

        var vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        position.Z = 1f;

        var vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        return (vectorNear, vectorFar);
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
    public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position)
    {
        var position2D = position.XY();

        cameraComponent.ScreenToWorldRaySegment(ref position2D, out var ray);

        var direction = ray.End - ray.Start;
        direction.Normalize();

        Vector3.TransformNormal(ref direction, ref cameraComponent.ViewMatrix, out var viewSpaceDir);

        float rayDistance = position.Z / viewSpaceDir.Z;

        return ray.Start + direction * rayDistance;
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
        ArgumentNullException.ThrowIfNull(cameraComponent);

        Matrix.Invert(ref cameraComponent.ViewProjectionMatrix, out var inverseViewProjection);

        ScreenToClipSpace(ref position, out var clipSpace);

        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var near);

        clipSpace.Z = 1f;

        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var far);

        result = new RaySegment(near, far);
    }

    /// <summary>
    /// Converts the world position to clip space coordinates relative to camera.
    /// </summary>
    /// <param name="cameraComponent">The camera component used for the transformation.</param>
    /// <param name="position">The position in world space to be transformed.</param>
    /// <returns>The position in clip space.</returns>
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

    private static void ScreenToClipSpace(ref Vector2 position, out Vector3 clipSpace)
    {
        clipSpace = new Vector3
        {
            X = position.X * 2f - 1f,
            Y = 1f - position.Y * 2f,
            Z = 0f
        };
    }
}