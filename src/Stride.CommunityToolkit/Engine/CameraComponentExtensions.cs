using Stride.CommunityToolkit.Graphics;
using Stride.CommunityToolkit.Scripts;
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
    /// Calculates a ray from the camera's position through a specified point on the screen, projecting from screen space into the 3D world space.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used to generate the view and projection matrices for the calculation.</param>
    /// <param name="screenPosition">
    /// The normalized position on the screen (typically the mouse position), with coordinates ranging from (0,0) at the bottom-left to (1,1) at the top-right.
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
        var (nearPoint, farPoint) = camera.ScreenPointToRay(screenPosition);

        return (nearPoint.XYZ(), farPoint.XYZ());
    }

    /// <summary>
    /// Calculates a ray from the camera through a specified point on the screen, projecting into the 3D world space.
    /// </summary>
    /// <param name="camera">The <see cref="CameraComponent"/> used for the ray calculation.</param>
    /// <param name="screenPosition">
    /// The position on the screen, typically the mouse position, normalized between (0,0) (bottom-left) and (1,1) (top-right).
    /// </param>
    /// <returns>
    /// A <see cref="Ray"/> starting from the camera and pointing into the 3D world through the specified screen position.
    /// </returns>
    /// <remarks>
    /// This method is commonly used for object picking or raycasting operations, where interaction with 3D objects is based on screen space coordinates (e.g., mouse cursor).
    /// The ray is calculated by transforming the screen position into world space, creating a direction vector from the camera's near plane to its far plane.
    /// </remarks>
    ///
    public static Ray GetPickRay(this CameraComponent camera, Vector2 screenPosition)
    {
        var (nearPoint, farPoint) = camera.CalculateRayFromScreenPosition(screenPosition);

        var direction = farPoint - nearPoint;

        direction.Normalize();

        return new Ray(nearPoint, direction);
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
    /// Calculates the near and far vectors for a ray that starts at the camera and passes through a given screen point.
    /// The ray is in world space, starting at the near plane of the camera and extending through the specified pixel coordinates on the screen.
    /// </summary>
    /// <param name="camera">The camera component used to calculate the ray.</param>
    /// <param name="screenPosition">The screen position (in normalized coordinates) through which the ray passes.</param>
    /// <returns>A tuple containing the near vector (<c>VectorNear</c>) and the far vector (<c>VectorFar</c>) of the ray in world space.</returns>
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenPointToRay(this CameraComponent camera, Vector2 screenPosition)
    {
        // Invert the view-projection matrix to transform from screen space to world space
        var invertedMatrix = Matrix.Invert(camera.ViewProjectionMatrix);

        // Reconstruct the projection-space position in the (-1, +1) range.
        // The X coordinate maps directly from screen space (0,1) to projection space (-1,+1).
        // The Y coordinate is inverted because screen space is Y-down, while projection space is Y-up.
        Vector3 position;
        position.X = screenPosition.X * 2f - 1f;
        position.Y = 1f - screenPosition.Y * 2f;

        // Set Z = 0 for the near plane (the starting point of the ray)
        // Unproject the near point from projection space to world space
        position.Z = 0f;
        var vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        // Set Z = 1 for the far plane (the end point of the ray)
        // Unproject the far point from projection space to world space
        position.Z = 1f;
        var vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        return (vectorNear, vectorFar);
    }

    /// <summary>
    /// Converts the screen position to a point in world coordinates relative to <paramref name="cameraComponent"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from the near camera plane. Passed by reference, allowing for potential optimizations in memory usage.</param>
    /// <returns>The world position calculated from the screen position.</returns>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// <para>This method takes the <paramref name="position"/> parameter by reference (<c>ref</c>), which may optimize memory usage and prevent unnecessary copies of the vector.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        cameraComponent.ScreenToWorldPoint(ref position, out var result);

        return result;
    }

    /// <summary>
    /// Converts the screen position to a point in world coordinates relative to <paramref name="cameraComponent"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from the near camera plane. Passed by value, which may be simpler to use but less efficient in memory-constrained environments.</param>
    /// <returns>The world position calculated from the screen position.</returns>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// <para>This method takes the <paramref name="position"/> parameter by value, which is simpler to use but may involve a copy of the vector, which could be less efficient for large or frequent transformations.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static void ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        var position2D = position.XY();

        cameraComponent.ScreenToWorldRaySegment(ref position2D, out var ray);

        var direction = ray.End - ray.Start;
        direction.Normalize();

        Vector3.TransformNormal(ref direction, ref cameraComponent.ViewMatrix, out var viewSpaceDir);

        float rayDistance = position.Z / viewSpaceDir.Z;

        result = ray.Start + (direction * rayDistance);
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
    /// Transforms a world-space position to clip space coordinates relative to camera,
    /// using the camera's view-projection matrix.
    /// The result is returned as a <see cref="Vector3"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component whose view-projection matrix will be used.</param>
    /// <param name="position">The world-space position to be transformed.</param>
    /// <returns>The position in clip space as a <see cref="Vector3"/>.</returns>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static Vector3 WorldToClip(this CameraComponent cameraComponent, ref Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewProjectionMatrix, out var result);

        return result;
    }

    /// <summary>
    /// Transforms a world-space position to clip space coordinates relative to camera,
    /// using the camera's view-projection matrix.
    /// The result is returned via the <paramref name="result"/> parameter.
    /// </summary>
    /// <param name="cameraComponent">The camera component whose view-projection matrix will be used.</param>
    /// <param name="position">The world-space position to be transformed.</param>
    /// <param name="result">The resulting position in clip space.</param>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static void WorldToClip(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewProjectionMatrix, out result);
    }

    /// <summary>
    /// Converts the world position to screen space coordinates relative to <paramref name="cameraComponent"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position to be converted to screen space. Passed by reference, allowing for potential optimizations in memory usage.</param>
    /// <returns>
    /// The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.
    /// </returns>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// <para>This method takes the <paramref name="position"/> parameter by reference (<c>ref</c>), which may optimize memory usage and prevent unnecessary copies of the vector.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        cameraComponent.WorldToScreenPoint(ref position, out var result);

        return result;
    }

    /// <summary>
    /// Converts the world position to screen space coordinates relative to <paramref name="cameraComponent"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position to be converted to screen space. Passed by value, which may be simpler to use but less efficient in memory-constrained environments.</param>
    /// <returns>
    /// The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.
    /// </returns>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or its containing <see cref="Entity"/> <see cref="TransformComponent"/> has been modified since the last frame, you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// <para>This method takes the <paramref name="position"/> parameter by value, which is simpler to use but may involve a copy of the vector, which could be less efficient for large or frequent transformations.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="cameraComponent"/> is null.</exception>
    public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        cameraComponent.WorldToScreenPoint(ref position, out var result);

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
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
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

    /// <summary>
    /// Converts the world position to screen space coordinates relative to <paramref name="cameraComponent"/> and the window size of the <paramref name="graphicsDevice"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position to be converted to screen space.</param>
    /// <param name="graphicsDevice">The graphics device providing information about the window size.</param>
    /// <returns>
    /// The screen position as  normalized X * <paramref name="graphicsDevice"/> width, normalized Y * <paramref name="graphicsDevice"/> height. Z is always 0.
    /// </returns>
    /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
    /// </remarks>
    public static Vector2 WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position, GraphicsDevice graphicsDevice)
    {
        var worldPoint = cameraComponent.WorldToScreenPoint(ref position);
        var windowSize = graphicsDevice.GetWindowSize();

        return new Vector2
        {
            X = worldPoint.X * windowSize.X,
            Y = worldPoint.Y * windowSize.Y
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

    private static Vector3 ScreenToClipSpace(Vector2 position)
    {
        ScreenToClipSpace(ref position, out var result);

        return result;
    }
}