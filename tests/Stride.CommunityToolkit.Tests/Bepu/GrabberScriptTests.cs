using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
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
/// Pins the constraint lifecycle of <see cref="GrabberScript"/> against a real headless game with a
/// Bepu simulation: a grab puts the two servo constraints on the grabber's own entity and they
/// attach to the solver, a kinematic body is refused, and a release removes them again.
/// </summary>
/// <remarks>
/// The mouse path (raycast, wheel, rotation key) is not driven here; the tests call
/// <see cref="GrabberScript.Grab"/> and <see cref="GrabberScript.Release"/> directly, which is
/// also the API a game uses to hand the player something. Same collection as the run tests: one
/// headless game at a time.
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class GrabberScriptTests
{
    [Fact]
    public void Grab_AttachesServosOnTheGrabberEntity_AndReleaseRemovesThem()
    {
        using var game = new Game();

        Entity? camera = null;
        Entity? box = null;
        GrabberScript? grabber = null;

        var frames = 0;
        var grabbed = false;
        var heldAfterGrab = false;
        var linearAttached = false;
        var angularAttached = false;
        var linearHostedOnCamera = false;
        var boxUntouched = false;
        var heldAfterRelease = true;
        var constraintsAfterRelease = -1;
        var releasedEvents = 0;

        game.Run(SceneSetup.None, start: scene =>
        {
            grabber = new GrabberScript();
            grabber.Released += _ => releasedEvents++;

            camera = new Entity("camera") { new CameraComponent(), grabber };
            camera.Scene = scene;

            box = new Entity("box") { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
            box.Transform.Position = new Vector3(0, 1, 0);
            box.Scene = scene;
        }, update: (_, _) =>
        {
            frames++;

            var body = box!.Get<BodyComponent>();

            // Frame 1: the body has entered the simulation. Grab it half a unit off its centre.
            if (frames == 2)
            {
                grabbed = grabber!.Grab(body, body.Position + new Vector3(0.5f, 0, 0), 5);
                heldAfterGrab = ReferenceEquals(grabber.Held, body);
                return;
            }

            // Frame 3: the constraint processor has seen the components.
            if (frames == 3)
            {
                var linear = camera!.Get<OneBodyLinearServoConstraintComponent>();
                var angular = camera.Get<OneBodyAngularServoConstraintComponent>();

                linearHostedOnCamera = linear is not null;
                linearAttached = linear?.Attached == true;
                angularAttached = angular?.Attached == true;
                boxUntouched = box.Components.Count == 2;          // transform + body, nothing added by the grab

                grabber!.Release();
                heldAfterRelease = grabber.Held is not null;
                constraintsAfterRelease = camera.Components.Count(c => c is ConstraintComponentBase);
                return;
            }

            if (frames == 4)
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(grabbed, "Grab returned false for a dynamic body in the simulation.");
        Assert.True(heldAfterGrab);
        Assert.True(linearHostedOnCamera, "The linear servo should live on the grabber's entity.");
        Assert.True(linearAttached, "The linear servo never attached to the solver.");
        Assert.True(angularAttached, "The angular servo never attached to the solver.");
        Assert.True(boxUntouched, "The grab must not add components to the held entity.");
        Assert.False(heldAfterRelease);
        Assert.Equal(0, constraintsAfterRelease);
        Assert.Equal(1, releasedEvents);
    }

    [Fact]
    public void Grab_RefusesAKinematicBody()
    {
        using var game = new Game();

        Entity? box = null;
        GrabberScript? grabber = null;
        var frames = 0;
        var grabbed = true;

        game.Run(SceneSetup.None, start: scene =>
        {
            grabber = new GrabberScript();

            var camera = new Entity("camera") { new CameraComponent(), grabber };
            camera.Scene = scene;

            box = new Entity("box") { new BodyComponent { Kinematic = true, Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
            box.Scene = scene;
        }, update: (_, _) =>
        {
            if (++frames < 2)
                return;

            grabbed = grabber!.Grab(box!.Get<BodyComponent>(), Vector3.Zero, 5);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.False(grabbed);
        Assert.Null(grabber!.Held);
    }
}