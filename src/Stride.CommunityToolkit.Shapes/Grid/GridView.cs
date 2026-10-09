using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// What a reference grid needs to know about the camera for one frame: how to get from the world
/// to the screen and back. Screen positions are in the display's scaled pixels, the units a
/// <see cref="ShapeBatch"/> draws in with <see cref="ShapeBatch.Screen"/> on.
/// </summary>
internal readonly struct GridView
{
    /// <summary>At or below this clip-space W a point is on or behind the camera's plane and has no place on the screen.</summary>
    private const float SmallestW = 0.00001f;

    /// <summary>
    /// What <see cref="WorldPerPixel"/> answers for a point behind the camera, where there is no
    /// scale to measure: the scale at which an automatic major step comes out as one world unit.
    /// </summary>
    private const float UnmeasuredWorldPerPixel = 1f / GridSteps.TargetMajorPixels;

    private readonly Matrix _viewProjection;
    private readonly Matrix _inverse;

    internal GridView(Matrix viewProjection, Vector2 size, bool orthographic)
    {
        _viewProjection = viewProjection;
        _inverse = Matrix.Invert(viewProjection);
        Size = size;
        Orthographic = orthographic;
    }

    /// <summary>The window's size in scaled pixels.</summary>
    internal Vector2 Size { get; }

    internal bool Orthographic { get; }

    /// <summary>The screen position of a world point, or <see langword="false"/> when it is behind the camera.</summary>
    internal bool Project(Vector3 world, out Vector2 screen)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), _viewProjection);

        if (clip.W <= SmallestW)
        {
            screen = default;

            return false;
        }

        screen = new Vector2((clip.X / clip.W * 0.5f + 0.5f) * Size.X, (0.5f - clip.Y / clip.W * 0.5f) * Size.Y);

        return true;
    }

    /// <summary>
    /// Where the ray through a screen position, normalised (0,0) top left to (1,1) bottom right,
    /// meets the plane Z = 0, as its X and Y.
    /// </summary>
    internal bool OnPlaneZ(Vector2 normalised, out Vector2 point)
    {
        var (origin, direction) = RayAt(normalised);

        var meets = Meet(origin.Z, direction.Z, out var t);

        point = new Vector2(origin.X + direction.X * t, origin.Y + direction.Y * t);

        return meets;
    }

    /// <summary>
    /// Where the ray through a screen position meets the ground plane Y = 0, as its X and Z.
    /// </summary>
    internal bool OnPlaneY(Vector2 normalised, out Vector2 point)
    {
        var (origin, direction) = RayAt(normalised);

        var meets = Meet(origin.Y, direction.Y, out var t);

        point = new Vector2(origin.X + direction.X * t, origin.Z + direction.Z * t);

        return meets;
    }

    /// <summary>
    /// World units per scaled pixel at a world point's depth. A point behind the camera has no
    /// depth on the screen, so it gets the scale that gives a grid a major step of one unit.
    /// </summary>
    internal float WorldPerPixel(Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), _viewProjection);

        if (clip.W <= SmallestW) return UnmeasuredWorldPerPixel;

        // Two points one pixel apart on the screen, at the depth of the world point
        var x = clip.X / clip.W;
        var y = clip.Y / clip.W;
        var depth = clip.Z / clip.W;
        var here = Vector3.TransformCoordinate(new Vector3(x, y, depth), _inverse);
        var next = Vector3.TransformCoordinate(new Vector3(x, y + 2f / Size.Y, depth), _inverse);

        return Vector3.Distance(here, next);
    }

    private (Vector3 Origin, Vector3 Direction) RayAt(Vector2 normalised)
    {
        var clip = new Vector3(normalised.X * 2f - 1f, 1f - normalised.Y * 2f, 0f);
        var near = Vector3.TransformCoordinate(clip, _inverse);

        clip.Z = 1f;

        var far = Vector3.TransformCoordinate(clip, _inverse);

        return (near, Vector3.Normalize(far - near));
    }

    /// <summary>Solves <c>origin + direction * t = 0</c> on one axis, for a ray that reaches the plane ahead of it.</summary>
    private static bool Meet(float origin, float direction, out float t)
    {
        t = 0f;

        if (MathF.Abs(direction) < 0.000001f) return false;

        t = -origin / direction;

        return t >= 0f;
    }
}