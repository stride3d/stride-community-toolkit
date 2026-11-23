using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Engine.Processors;
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
    /// The world point where the ray through a screen position meets the XY plane, Z = 0: a mouse position as a
    /// 2D world position, which is what a 2D physics world such as Box2D works in.
    /// </summary>
    /// <param name="camera">The camera the ray starts from.</param>
    /// <param name="screenPosition">The normalized screen position, typically the mouse position, from (0,0) at the top-left to (1,1) at the bottom-right.</param>
    /// <returns>The point on the plane, or <see langword="null"/> when the ray is parallel to the plane or the plane lies outside the camera's near and far planes.</returns>
    public static Vector2? ScreenToWorldPointOnXYPlane(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);
        var direction = ray.End - ray.Start;

        // Parallel to the plane: no crossing
        if (MathUtil.IsZero(direction.Z)) return null;

        // Where along the segment the ray reaches Z = 0, and whether that is within it
        var t = -ray.Start.Z / direction.Z;

        if (t < 0 || t > 1) return null;

        var intersection = ray.Start + t * direction;

        return new Vector2(intersection.X, intersection.Y);
    }

    /// <summary>
    /// A <see cref="Ray"/> from the camera through a screen position, into the world: the starting point of picking
    /// and of a physics ray cast.
    /// </summary>
    /// <param name="camera">The camera the ray starts from.</param>
    /// <param name="screenPosition">The normalized screen position, typically the mouse position, from (0,0) at the top-left to (1,1) at the bottom-right.</param>
    /// <returns>The ray, starting on the camera's near plane with a unit direction towards the far plane.</returns>
    public static Ray ScreenToWorldRay(this CameraComponent camera, Vector2 screenPosition)
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
    public static (Vector4 VectorNear, Vector4 VectorFar) ScreenToWorldRayEnds(this CameraComponent camera, Vector2 screenPosition)
    {
        var ray = camera.ScreenToWorldRaySegment(screenPosition);

        return (new Vector4(ray.Start, 1f), new Vector4(ray.End, 1f));
    }

    /// <summary>
    /// The width and height of the world an orthographic camera shows, in world units: the orthographic size is
    /// the height, and the width follows the aspect ratio the projection was built with.
    /// </summary>
    /// <param name="camera">The orthographic camera.</param>
    /// <returns>The visible width and height, in world units.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="camera"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If the camera is not orthographic.</exception>
    /// <remarks>
    /// Read from <see cref="CameraComponent.ProjectionMatrix"/>, so it reflects a custom aspect ratio when one is
    /// set, and is right once the camera has been updated for a frame.
    /// </remarks>
    public static Vector2 GetOrthographicVisibleSize(this CameraComponent camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        if (camera.Projection != CameraProjectionMode.Orthographic) throw new InvalidOperationException("The camera is not orthographic.");

        var projection = camera.ProjectionMatrix;

        return new Vector2(2f / projection.M11, 2f / projection.M22);
    }

    /// <summary>
    /// The rectangle of the XY plane an orthographic camera looking along Z shows, as its lower-left and upper-right
    /// corners in world units: <see cref="GetOrthographicVisibleSize"/> centred on the camera's position. The edges of
    /// a 2D scene's view, for placing things at them or keeping things inside them.
    /// </summary>
    /// <param name="camera">The orthographic camera.</param>
    /// <returns>The lower-left and the upper-right corner of the visible rectangle, in world units.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="camera"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If the camera is not orthographic.</exception>
    public static (Vector2 Min, Vector2 Max) GetOrthographicVisibleBounds(this CameraComponent camera)
    {
        var half = camera.GetOrthographicVisibleSize() * 0.5f;
        var position = camera.Entity?.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;
        var centre = new Vector2(position.X, position.Y);

        return (centre - half, centre + half);
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

    /// <summary>
    /// Where a world position lands on screen when it is in view: in front of the camera, between its near and far
    /// planes and inside the sides of the view. A label hung on an object, or a marker on a target, drawn only while
    /// there is something on screen to point at.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position.</param>
    /// <param name="screenPoint">The screen position in normalized X, Y coordinates, top-left (0,0) to bottom-right (1,1), with Z in world units from the near plane, when the method returns <see langword="true"/>; otherwise default.</param>
    /// <returns><see langword="true"/> when the position is in view.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="cameraComponent"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
    /// </remarks>
    public static bool TryWorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position, out Vector3 screenPoint)
    {
        ArgumentNullException.ThrowIfNull(cameraComponent);

        // In clip space the view volume is |x|, |y| <= w and 0 <= z <= w; w <= 0 is behind the camera
        var clip = Vector4.Transform(new Vector4(position, 1f), cameraComponent.ViewProjectionMatrix);

        if (clip.W <= 0f || MathF.Abs(clip.X) > clip.W || MathF.Abs(clip.Y) > clip.W || clip.Z < 0f || clip.Z > clip.W)
        {
            screenPoint = default;

            return false;
        }

        // X and Y come from the clip position already in hand; only Z needs the view transform
        Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewMatrix, out var viewSpace);

        screenPoint = new Vector3
        {
            X = (clip.X / clip.W + 1f) / 2f,
            Y = 1f - (clip.Y / clip.W + 1f) / 2f,
            Z = viewSpace.Z + cameraComponent.NearClipPlane,
        };

        return true;
    }

    /// <summary>
    /// Where a world position lands on screen, in pixels of the <paramref name="graphicsDevice"/>'s window, when it is
    /// in view; see <see cref="TryWorldToScreenPoint(CameraComponent, Vector3, out Vector3)"/>.
    /// </summary>
    /// <param name="cameraComponent">The camera component used to perform the calculation.</param>
    /// <param name="position">The world space position.</param>
    /// <param name="graphicsDevice">The graphics device providing the window size.</param>
    /// <param name="screenPoint">The screen position in pixels when the method returns <see langword="true"/>; otherwise default.</param>
    /// <returns><see langword="true"/> when the position is in view.</returns>
    public static bool TryWorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position, GraphicsDevice graphicsDevice, out Vector2 screenPoint)
    {
        if (!cameraComponent.TryWorldToScreenPoint(position, out var normalized))
        {
            screenPoint = default;

            return false;
        }

        var windowSize = graphicsDevice.GetWindowSize();

        screenPoint = new Vector2(normalized.X * windowSize.X, normalized.Y * windowSize.Y);

        return true;
    }

    /// <summary>
    /// Whether a world position lands inside a rectangle of the screen: the test a marquee selection makes for every
    /// candidate once the mouse is released. The corners are normalized screen points, top-left (0,0) to bottom-right
    /// (1,1), in either order; a position behind the camera or off the sides of the view is never inside.
    /// </summary>
    /// <param name="cameraComponent">The camera the screen belongs to.</param>
    /// <param name="position">The world space position.</param>
    /// <param name="cornerA">One corner of the rectangle, normalized.</param>
    /// <param name="cornerB">The opposite corner, normalized.</param>
    /// <returns><see langword="true"/> when the position is in view and inside the rectangle.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="cameraComponent"/> is <see langword="null"/>.</exception>
    public static bool IsInScreenRectangle(this CameraComponent cameraComponent, Vector3 position, Vector2 cornerA, Vector2 cornerB)
    {
        if (!cameraComponent.TryWorldToScreenPoint(position, out var screen)) return false;

        return screen.X >= MathF.Min(cornerA.X, cornerB.X) && screen.X <= MathF.Max(cornerA.X, cornerB.X)
            && screen.Y >= MathF.Min(cornerA.Y, cornerB.Y) && screen.Y <= MathF.Max(cornerA.Y, cornerB.Y);
    }
}