using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// <see cref="CameraComponentExtensions.TryWorldToScreenPoint(CameraComponent, Vector3, out Vector3)"/>: a position
/// in front of the camera lands where <c>WorldToScreenPoint</c> puts it, and one behind the camera or off the sides
/// of the view is refused rather than projected to a point that is not there. And the orthographic helpers: the
/// visible size is the orthographic size by the aspect ratio, the bounds are centred on the camera.
/// </summary>
public class CameraComponentExtensionsTests
{
    private const float Aspect = 16f / 9f;

    [Fact]
    public void TryWorldToScreenPoint_InFrontOfTheCamera_IsTheScreenCentre()
    {
        var camera = Camera();

        Assert.True(camera.TryWorldToScreenPoint(Vector3.Zero, out var screen));
        Assert.Equal(0.5f, screen.X, 0.001f);
        Assert.Equal(0.5f, screen.Y, 0.001f);
    }

    [Fact]
    public void TryWorldToScreenPoint_BehindTheCamera_IsFalse()
    {
        var camera = Camera();

        Assert.False(camera.TryWorldToScreenPoint(new Vector3(0, 0, 20), out _));
    }

    [Fact]
    public void TryWorldToScreenPoint_OffTheSideOfTheView_IsFalse()
    {
        var camera = Camera();

        Assert.False(camera.TryWorldToScreenPoint(new Vector3(100, 0, 0), out _));
        Assert.True(camera.TryWorldToScreenPoint(new Vector3(1, 0, 0), out var screen));
        Assert.True(screen.X > 0.5f);
    }

    [Fact]
    public void GetOrthographicVisibleSize_IsTheOrthographicSizeByTheAspectRatio()
    {
        var camera = OrthographicCamera(size: 10f, position: new Vector3(3, 4, 10));

        var visible = camera.GetOrthographicVisibleSize();

        Assert.Equal(10f * Aspect, visible.X, 0.001f);
        Assert.Equal(10f, visible.Y, 0.001f);
    }

    [Fact]
    public void GetOrthographicVisibleBounds_IsCentredOnTheCamera()
    {
        var camera = OrthographicCamera(size: 10f, position: new Vector3(3, 4, 10));

        var (min, max) = camera.GetOrthographicVisibleBounds();

        Assert.Equal(3f - 5f * Aspect, min.X, 0.001f);
        Assert.Equal(-1f, min.Y, 0.001f);
        Assert.Equal(3f + 5f * Aspect, max.X, 0.001f);
        Assert.Equal(9f, max.Y, 0.001f);
    }

    [Fact]
    public void GetOrthographicVisibleSize_OnAPerspectiveCamera_Throws()
    {
        var camera = Camera();

        Assert.Throws<InvalidOperationException>(() => camera.GetOrthographicVisibleSize());
    }

    /// <summary>An orthographic camera of the given view height at <paramref name="position"/>, looking down the Z axis.</summary>
    private static CameraComponent OrthographicCamera(float size, Vector3 position)
    {
        var entity = new Entity("camera") { new CameraComponent { Projection = CameraProjectionMode.Orthographic, OrthographicSize = size } };

        entity.Transform.Position = position;
        entity.Transform.UpdateWorldMatrix();

        var camera = entity.Get<CameraComponent>();

        camera.Update(Aspect);

        return camera;
    }

    /// <summary>A perspective camera ten units up the Z axis, looking down it at the origin.</summary>
    private static CameraComponent Camera()
    {
        var entity = new Entity("camera") { new CameraComponent() };

        entity.Transform.Position = new Vector3(0, 0, 10);
        entity.Transform.UpdateWorldMatrix();

        var camera = entity.Get<CameraComponent>();

        camera.Update(Aspect);

        return camera;
    }
}