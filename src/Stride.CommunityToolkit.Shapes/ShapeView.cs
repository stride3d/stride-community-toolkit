using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// What the render feature knew about the view when it drew a batch: the figures the vertex stage
/// placed every shape with, kept so a pick can place the mouse the same way. The feature sets one
/// per draw; the last view to draw a batch is the one its picks answer for.
/// </summary>
/// <param name="ViewProjection">The view's view-projection matrix.</param>
/// <param name="InverseViewProjection">Its inverse, for casting a ray from a screen position.</param>
/// <param name="ViewSize">The view's size in physical pixels.</param>
/// <param name="PixelScale">Pixels per world unit at clip w = 1, already divided by the display scale - the shader's <c>PixelScale</c>.</param>
/// <param name="ScreenScale">Physical pixels per scaled pixel: the display scale the batch draws at.</param>
/// <param name="CameraRight">The camera's right axis in world space, which a screen-aligned billboard lies along.</param>
/// <param name="CameraUp">The camera's up axis in world space.</param>
/// <param name="EyePosition">Where the camera is, which an axial billboard turns to face.</param>
internal readonly record struct ShapeView(
    Matrix ViewProjection,
    Matrix InverseViewProjection,
    Vector2 ViewSize,
    float PixelScale,
    float ScreenScale,
    Vector3 CameraRight,
    Vector3 CameraUp,
    Vector3 EyePosition)
{
    /// <summary>The ray through a screen position, normalised (0,0) top left to (1,1) bottom right, from the near plane into the scene.</summary>
    public Ray RayAt(Vector2 screenPosition)
    {
        var clip = new Vector3(screenPosition.X * 2f - 1f, 1f - screenPosition.Y * 2f, 0f);
        var near = Vector3.TransformCoordinate(clip, InverseViewProjection);

        clip.Z = 1f;

        var far = Vector3.TransformCoordinate(clip, InverseViewProjection);

        return new Ray(near, Vector3.Normalize(far - near));
    }

    /// <summary>The clip w of a world point: its distance term, 1 under an orthographic projection.</summary>
    public float ClipW(Vector3 world) => Vector4.Transform(new Vector4(world, 1f), ViewProjection).W;

    /// <summary>The depth a world point rasterises to, 0 at the near plane and 1 at the far one.</summary>
    public float Depth(Vector3 world)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), ViewProjection);

        return clip.Z / MathF.Max(clip.W, 0.0001f);
    }

    /// <summary>World units per scaled pixel at a world point's depth - the relation the border width uses.</summary>
    public float WorldPerPixel(Vector3 world) => MathF.Max(ClipW(world), 0.0001f) / PixelScale;
}