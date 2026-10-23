using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Engine;

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
    /// Calculates a 2D point where the camera ray intersects the Z=0 plane.
    /// Useful for 2D physics engines like Box2D that operate in the XY plane.
    /// </summary>
    /// <param name="camera">The camera component used for the ray calculation.</param>
    /// <param name="screenPosition">The normalized screen position (typically mouse position).</param>
    /// <returns>A Vector2 representing the intersection point on the Z=0 plane, or null if no intersection.</returns>
    public static Vector2? CalculateRayPlaneIntersectionPoint(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);
        var direction = ray.End - ray.Start;

        // Parallel to the plane: no crossing
        if (Math.Abs(direction.Z) < 1e-6f) return null;

        // Where along the segment the ray reaches Z = 0, and whether that is within it
        var t = -ray.Start.Z / direction.Z;

        if (t < 0 || t > 1) return null;

        var intersection = ray.Start + t * direction;

        return new Vector2(intersection.X, intersection.Y);
    }

    /// <summary>
    /// Calculates a ray from the camera's position through a specified point on the screen, projecting from screen space into the 3D world space.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used to generate the view and projection matrices for the calculation.</param>
    /// <param name="screenPosition">
    /// The normalized screen position (typically the mouse position), from (0,0) at the top-left to (1,1) at the bottom-right.
    /// </param>
    /// <returns>
    /// A tuple containing two points:
    /// <c>nearPoint</c>, which is the world-space position on the near plane, and
    /// <c>farPoint</c>, which is the world-space position on the far plane.
    /// These two points define the ray from the camera into the 3D world through the specified screen position.
    /// </returns>
    /// <remarks>
    /// This method is typically used for raycasting and object picking in 3D space, where you need to determine what objects lie under a particular screen-space position, such as the mouse cursor.
    /// The ray is defined by transforming the screen position into world space, calculating points on the near and far planes of the camera's view frustum.
    /// </remarks>
    public static (Vector3 nearPoint, Vector3 farPoint) CalculateRayFromScreenPosition(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);

        return (ray.Start, ray.End);
    }

    /// <summary>
    /// Calculates a ray from the camera through a specified point on the screen, projecting into the 3D world space.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used for the ray calculation.</param>
    /// <param name="screenPosition">
    /// The normalized screen position, typically the mouse position, from (0,0) at the top-left to (1,1) at the bottom-right.
    /// </param>
    /// <returns>
    /// A <see cref="Ray"/> starting from the camera and pointing into the 3D world through the specified screen position.
    /// </returns>
    /// <remarks>
    /// This method is commonly used for object picking or raycasting operations, where interaction with 3D objects is based on screen space coordinates (e.g., mouse cursor).
    /// The ray is calculated by transforming the screen position into world space, creating a direction vector from the camera's near plane to its far plane.
    /// </remarks>
    public static Ray GetPickRay(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);
        var direction = ray.End - ray.Start;

        direction.Normalize();

        return new Ray(ray.Start, direction);
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
        => camera.LogicDirectionToWorldDirection(logicDirection, Vector3.UnitY);

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
    /// Calculates the near and far vectors for a ray that starts at the camera and passes through a given screen point.
    /// The ray is in world space, from the near plane to the far plane, through the specified normalized screen position.
    /// </summary>
    /// <param name="camera">The camera component used to calculate the ray.</param>
    /// <param name="screenPosition">The screen position (in normalized coordinates) through which the ray passes.</param>
    /// <returns>A tuple containing the near vector (<c>VectorNear</c>) and the far vector (<c>VectorFar</c>) of the ray in world space, each with <c>W</c> of 1.</returns>
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenPointToRay(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);

        return (new Vector4(ray.Start, 1f), new Vector4(ray.End, 1f));
    }

    /// <summary>
    /// Converts the screen position to a point in world coordinates relative to <paramref name="cameraComponent"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from the near camera plane.</param>
    /// <param name="result">Position in world coordinates.</param>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        var position2D = position.XY();

        cameraComponent.ScreenToWorldRaySegment(ref position2D, out var ray);

        var direction = ray.End - ray.Start;
        direction.Normalize();

        Vector3.TransformNormal(ref direction, ref cameraComponent.ViewMatrix, out var viewSpaceDir);

        var rayDistance = position.Z / viewSpaceDir.Z;

        result = ray.Start + (direction * rayDistance);
    }

    /// <inheritdoc cref="ScreenToWorldPoint(CameraComponent, ref Vector3, out Vector3)"/>
    /// <returns>The world position calculated from the screen position.</returns>
    public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position)
    {
        cameraComponent.ScreenToWorldPoint(ref position, out var result);

        return result;
    }

    /// <inheritdoc cref="ScreenToWorldPoint(CameraComponent, ref Vector3)"/>
    public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, Vector3 position)
        => cameraComponent.ScreenToWorldPoint(ref position);

    /// <summary>
    /// Converts the screen position to a <see cref="RaySegment"/> in world coordinates.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1).</param>
    /// <param name="result"><see cref="RaySegment"/>, starting at the near plane and ending at the far plane.</param>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void ScreenToWorldRaySegment(this CameraComponent cameraComponent, ref Vector2 position, out RaySegment result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        Matrix.Invert(ref cameraComponent.ViewProjectionMatrix, out var inverseViewProjection);

        // Screen space is (0,0) top-left, Y down; clip space is (-1,-1) bottom-left, Y up. Z = 0 is the
        // near plane and Z = 1 the far plane, and TransformCoordinate does the perspective divide
        var clipSpace = new Vector3(position.X * 2f - 1f, 1f - position.Y * 2f, 0f);

        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var near);

        clipSpace.Z = 1f;

        Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var far);

        result = new RaySegment(near, far);
    }

    /// <inheritdoc cref="ScreenToWorldRaySegment(CameraComponent, ref Vector2, out RaySegment)"/>
    /// <returns><see cref="RaySegment"/>, starting at the near plane and ending at the far plane.</returns>
    public static RaySegment ScreenToWorldRaySegment(this CameraComponent cameraComponent, Vector2 position)
    {
        cameraComponent.ScreenToWorldRaySegment(ref position, out var result);

        return result;
    }

    /// <summary>
    /// Transforms a world-space position to clip space coordinates relative to camera,
    /// using the camera's view-projection matrix.
    /// </summary>
    /// <param name="cameraComponent">The camera component whose view-projection matrix will be used.</param>
    /// <param name="position">The world-space position to be transformed.</param>
    /// <param name="result">The resulting position in clip space.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static void WorldToClip(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewProjectionMatrix, out result);
    }

    /// <inheritdoc cref="WorldToClip(CameraComponent, ref Vector3, out Vector3)"/>
    /// <returns>The position in clip space as a <see cref="Vector3"/>.</returns>
    public static Vector3 WorldToClip(this CameraComponent cameraComponent, ref Vector3 position)
    {
        cameraComponent.WorldToClip(ref position, out var result);

        return result;
    }

    /// <summary>
    /// Converts the world position to screen space coordinates relative to camera.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position to be converted to screen space.</param>
    /// <param name="result">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static void WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        cameraComponent.WorldToClip(ref position, out var clipSpace);

        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewMatrix, out var viewSpace);

        result = new Vector3
        {
            X = (clipSpace.X + 1f) / 2f,
            Y = 1f - (clipSpace.Y + 1f) / 2f,
            Z = viewSpace.Z + cameraComponent.NearClipPlane,
        };
    }

    /// <inheritdoc cref="WorldToScreenPoint(CameraComponent, ref Vector3, out Vector3)"/>
    /// <returns>
    /// The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.
    /// </returns>
    public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position)
    {
        cameraComponent.WorldToScreenPoint(ref position, out var result);

        return result;
    }

    /// <inheritdoc cref="WorldToScreenPoint(CameraComponent, ref Vector3)"/>
    public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position)
        => cameraComponent.WorldToScreenPoint(ref position);

    /// <summary>
    /// Converts the world position to screen space coordinates relative to <paramref name="cameraComponent"/> and the window size of the <paramref name="graphicsDevice"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position to be converted to screen space.</param>
    /// <param name="graphicsDevice">The graphics device providing information about the window size.</param>
    /// <returns>
    /// The screen position in pixels: normalized X times the window width, normalized Y times the window height.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static Vector2 WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position, GraphicsDevice graphicsDevice)
    {
        var screenPoint = cameraComponent.WorldToScreenPoint(ref position);
        var windowSize = graphicsDevice.GetWindowSize();

        return new Vector2(screenPoint.X * windowSize.X, screenPoint.Y * windowSize.Y);
    }
}
