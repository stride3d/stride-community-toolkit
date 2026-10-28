using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// The spike for grabbers in 2D scenes: whether the 3D <see cref="GrabberScript"/> works as it is
/// under an orthographic camera on a <see cref="Body2DComponent"/>. Two things have to hold. The pick
/// ray of an orthographic camera must start at the cursor's point on the near plane and run along
/// the view, not fan out from the camera; and the linear servo must carry a 2D body across the plane
/// without lifting it out of it.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class Grabber2DTests
{
    private const float Aspect = 16f / 9f;
    private const float OrthographicSize = 10f;
    private const float CameraZ = 50f;

    [Fact]
    public void OrthographicPickRay_StartsAtTheCursorAndRunsAlongTheView()
    {
        var entity = new Entity("camera") { new CameraComponent { Projection = CameraProjectionMode.Orthographic, OrthographicSize = OrthographicSize } };
        entity.Transform.Position = new Vector3(0, 0, CameraZ);
        entity.Transform.UpdateWorldMatrix();

        var camera = entity.Get<CameraComponent>();
        camera.Update(Aspect);

        // A quarter of the way across, half way down: a quarter of the view's width left of centre
        var ray = camera.ScreenToWorldRay(new Vector2(0.25f, 0.5f));

        Assert.Equal(-OrthographicSize * Aspect / 4f, ray.Position.X, 0.01f);
        Assert.Equal(0f, ray.Position.Y, 0.01f);
        Assert.Equal(-1f, ray.Direction.Z, 0.001f);
        Assert.Equal(0f, ray.Direction.X, 0.001f);
        Assert.Equal(0f, ray.Direction.Y, 0.001f);
    }

    [Fact]
    public void Grab_CarriesA2DBodyAcrossThePlane_WithoutLeavingIt()
    {
        using var game = new Game();

        Entity? cameraEntity = null;
        Entity? square = null;
        GrabberScript? grabber = null;

        var frames = 0;
        var grabbed = false;
        var linearAttached = false;
        var start = Vector3.Zero;
        var target = Vector3.Zero;
        var end = Vector3.Zero;
        var maxAbsZ = 0f;
        var stillHeld = false;

        game.Run(SceneSetup.None, start: scene =>
        {
            grabber = new GrabberScript();

            cameraEntity = new Entity("camera") { new CameraComponent { Projection = CameraProjectionMode.Orthographic, OrthographicSize = OrthographicSize }, grabber };
            cameraEntity.Transform.Position = new Vector3(0, 0, CameraZ);
            cameraEntity.Scene = scene;

            square = new Entity("square") { new Body2DComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
            square.Scene = scene;
        }, update: (_, _) =>
        {
            frames++;

            // No compositor drives the camera here, so its matrices are refreshed by hand each frame
            // before the grabber's own update reads the pick ray
            var camera = cameraEntity!.Get<CameraComponent>();
            camera.Update(Aspect);

            var body = square!.Get<Body2DComponent>();

            if (frames == 2)
            {
                start = body.Position;

                // The front face of the box, at the distance a raycast from the near plane would report
                var holdDistance = CameraZ - camera.NearClipPlane - 0.5f;

                grabbed = grabber!.Grab(body, new Vector3(0, 0, 0.5f), holdDistance);
                return;
            }

            if (frames == 3)
            {
                linearAttached = cameraEntity.Get<OneBodyLinearServoConstraintComponent>()?.Attached == true;
            }

            // The headless game runs frames far faster than the physics clock, so the test is paced by
            // game time, not by frames: the servo gets a second to settle, then half a second is measured
            var seconds = game.UpdateTime.Total.TotalSeconds;

            if (frames > 3 && seconds >= 1.0)
            {
                maxAbsZ = MathF.Max(maxAbsZ, MathF.Abs(body.Position.Z));
            }

            if (frames > 3 && seconds >= 1.5)
            {
                // Where the carry pulls: the cursor's ray at the hold distance, read once the camera's
                // world matrix has been through the transform processor
                var ray = camera.ScreenToWorldRay(game.Input.MousePosition);
                target = ray.Position + ray.Direction * (CameraZ - camera.NearClipPlane - 0.5f);

                end = body.Position;
                stillHeld = ReferenceEquals(grabber!.Held, body);
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Assert.True(grabbed, "Grab returned false for a 2D body in the simulation.");
        Assert.True(linearAttached, "The linear servo never attached to the solver.");
        Assert.True(stillHeld, "The grab ended on its own.");
        Assert.True(maxAbsZ < 0.05f, $"The body left the plane: |z| reached {maxAbsZ}.");

        var startDistance = Vector3.Distance(start, target);
        var endDistance = Vector3.Distance(end, target);

        // With the cursor away from the centre the body is carried toward it; with the cursor at the
        // centre it is held there against gravity. Either way the servo, not the fall, decides.
        if (startDistance > 0.5f)
        {
            Assert.True(endDistance < startDistance * 0.5f, $"The body was not carried toward the cursor: {startDistance} to {endDistance}.");
        }
        else
        {
            Assert.True(endDistance < 0.5f, $"The body was not held in place: it is {endDistance} from the target.");
        }
    }
}