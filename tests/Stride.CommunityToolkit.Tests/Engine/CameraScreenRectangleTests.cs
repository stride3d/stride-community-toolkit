using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// <see cref="CameraComponentExtensions.IsInScreenRectangle"/>: a point in view is inside a rectangle that covers
/// where it lands and outside one that does not, the corners may come in any order, and a point behind the camera
/// is never inside, whatever the rectangle.
/// </summary>
public class CameraScreenRectangleTests
{
    private const float Aspect = 16f / 9f;

    [Fact]
    public void IsInScreenRectangle_PointAtTheCentre_IsInsideARectangleRoundTheCentre()
    {
        var camera = Camera();

        Assert.True(camera.IsInScreenRectangle(Vector3.Zero, new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.6f)));
        Assert.True(camera.IsInScreenRectangle(Vector3.Zero, new Vector2(0.6f, 0.6f), new Vector2(0.4f, 0.4f)));
    }

    [Fact]
    public void IsInScreenRectangle_PointAtTheCentre_IsOutsideARectangleInTheCorner()
    {
        var camera = Camera();

        Assert.False(camera.IsInScreenRectangle(Vector3.Zero, new Vector2(0.7f, 0.7f), new Vector2(0.95f, 0.95f)));
    }

    [Fact]
    public void IsInScreenRectangle_PointBehindTheCamera_IsNeverInside()
    {
        var camera = Camera();

        Assert.False(camera.IsInScreenRectangle(new Vector3(0f, 0f, 20f), Vector2.Zero, Vector2.One));
    }

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