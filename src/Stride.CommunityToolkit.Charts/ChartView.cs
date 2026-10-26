using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// What one frame's drawing needs to know about where the chart is and how the camera sees it: the
/// root's world matrix, and the conversion between a length in screen pixels and a length in chart
/// units at any point of the chart. Built once per <see cref="Chart.Update(CameraComponent)"/>.
/// </summary>
/// <remarks>
/// Under an orthographic camera the conversion is one number for the whole chart. Under a perspective
/// camera it depends on how far the point is from the eye, so a tick that is eight pixels long is a
/// different length in chart units at the near and far ends of a 3D chart, and each is measured where it
/// is. The pixel scale it is built with is the display scale the batch draws pixels at, so a length
/// that the batch will multiply by it is measured in the same pixels.
/// </remarks>
internal readonly struct ChartView
{
    private readonly bool _orthographic;
    private readonly float _orthographicSize;
    private readonly float _tanHalfFov;
    private readonly float _pixelHeight;
    private readonly float _pixelScale;
    private readonly Vector2 _scale;
    private readonly Vector3 _eye;
    private readonly Vector3 _forward;

    /// <summary>The chart root's world matrix, refreshed for this frame.</summary>
    internal Matrix World { get; }

    /// <summary>Whether the root is drawn at unit scale, so chart units are world units and no point needs scaling.</summary>
    internal bool IsUnitScale { get; }

    internal ChartView(Entity root, CameraComponent camera, float pixelHeight, float pixelScale)
    {
        root.Transform.UpdateWorldMatrix();
        World = root.Transform.WorldMatrix;

        // A scaled root shrinks every chart unit on screen: the axis vectors' lengths are the scales, and
        // the up vector's is the one the vertical pixel measure runs along
        _scale = new Vector2(World.Right.Length(), World.Up.Length());
        IsUnitScale = MathF.Abs(_scale.X - 1f) < 1e-4f && MathF.Abs(_scale.Y - 1f) < 1e-4f;

        _pixelHeight = MathF.Max(pixelHeight, 1f);
        _pixelScale = pixelScale;

        var transform = camera.Entity.Transform;
        transform.UpdateWorldMatrix();
        _eye = transform.WorldMatrix.TranslationVector;
        _forward = Vector3.Normalize(transform.WorldMatrix.Forward);

        _orthographic = camera.Projection == CameraProjectionMode.Orthographic;
        _orthographicSize = camera.OrthographicSize;
        _tanHalfFov = MathF.Tan(MathUtil.DegreesToRadians(camera.VerticalFieldOfView) * 0.5f);
    }

    /// <summary>A chart-local point in world space.</summary>
    internal Vector3 ToWorld(Vector3 local) => Vector3.TransformCoordinate(local, World);

    /// <summary>
    /// The plane strokes are drawn in at a chart-local depth: its origin in world space and the root's X
    /// and Y axes, which the batch normalizes. Points in that plane are chart-local <c>x</c> and <c>y</c>
    /// through <see cref="ToPlane"/>.
    /// </summary>
    internal ChartPlane PlaneAt(float z) => new(ToWorld(new Vector3(0f, 0f, z)), World.Right, World.Up);

    /// <summary>
    /// A chart-local point as a coordinate in a plane from <see cref="PlaneAt"/>: the root's scale applied,
    /// since the plane's axes are unit length however the root is scaled.
    /// </summary>
    internal Vector2 ToPlane(Vector2 local) => local * _scale;

    /// <summary>How long one screen pixel is in chart units at a chart-local point.</summary>
    internal float UnitsPerPixel(Vector3 local)
    {
        float worldPerPixel;

        if (_orthographic)
        {
            worldPerPixel = _orthographicSize / _pixelHeight;
        }
        else
        {
            // The visible height at the point's depth, spread over the window's pixels; a point at or
            // behind the eye gets a floor so nothing divides by zero
            var depth = MathF.Max(Vector3.Dot(ToWorld(local) - _eye, _forward), 1e-3f);
            worldPerPixel = 2f * depth * _tanHalfFov / _pixelHeight;
        }

        return worldPerPixel * _pixelScale / MathF.Max(_scale.Y, MathUtil.ZeroTolerance);
    }

    /// <summary>A length in screen pixels as a length in chart units at a chart-local point.</summary>
    internal float ToUnits(float pixels, Vector3 local) => pixels * UnitsPerPixel(local);
}