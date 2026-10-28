using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// <see cref="BodyComponentExtensions.TeleportAtRest"/> against a real headless Bepu simulation: a body that
/// has been falling and spinning is put at a pose with both velocities zero and awake, in the same frame.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class BodyComponentExtensionsTests
{
    [Fact]
    public void TeleportAtRest_PutsTheBodyAtThePoseStillAndAwake()
    {
        using var game = new Game();

        var target = new Vector3(3f, 5f, -2f);
        var turned = Quaternion.RotationY(MathUtil.PiOverTwo);

        Entity? box = null;
        var frames = 0;
        var movingBefore = false;
        var position = Vector3.Zero;
        var rotation = Quaternion.Identity;
        var linear = Vector3.One;
        var angular = Vector3.One;
        var awake = false;

        game.Run(SceneSetup.None, start: scene =>
        {
            box = new Entity("box") { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
            box.Transform.Position = new Vector3(0, 10, 0);
            box.Scene = scene;
        }, update: (_, _) =>
        {
            frames++;

            var body = box!.Get<BodyComponent>();

            // Frame 2: the body is in the simulation; set it tumbling
            if (frames == 2)
            {
                body.LinearVelocity = new Vector3(4f, 0f, 0f);
                body.AngularVelocity = new Vector3(0f, 6f, 0f);
                return;
            }

            // Frame 6: still moving on its own, then put back at rest
            if (frames == 6)
            {
                movingBefore = body.LinearVelocity.LengthSquared() > 0f && body.AngularVelocity.LengthSquared() > 0f;

                body.TeleportAtRest(target, turned);

                position = body.Position;
                rotation = body.Orientation;
                linear = body.LinearVelocity;
                angular = body.AngularVelocity;
                awake = body.Awake;

                game.Exit();
            }
        }, context: new GameContextHeadless());

        Assert.True(movingBefore, "the body was not moving before the reset, so the reset proved nothing");
        Assert.Equal(target, position);
        Assert.Equal(turned, rotation);
        Assert.Equal(Vector3.Zero, linear);
        Assert.Equal(Vector3.Zero, angular);
        Assert.True(awake);
    }
}