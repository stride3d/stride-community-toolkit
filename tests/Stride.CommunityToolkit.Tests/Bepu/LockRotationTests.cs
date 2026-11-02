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
/// <see cref="BodyComponentExtensions.LockRotation"/> against a real headless Bepu simulation: two cubes given
/// the same two off-centre impulses in no gravity, one with X and Z locked, and after a run the locked one is
/// still upright while turning about Y, and the free one has tilted. Impulses, not a velocity written to the
/// body: the lock is on torque, and a velocity set directly is kept whatever the inertia.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class LockRotationTests
{
    [Fact]
    public void LockRotation_KeepsTheLockedAxesStillAndTheFreeOneTurning()
    {
        using var game = new Game();
        Entity? locked = null;
        Entity? free = null;
        var frames = 0;
        var lockedUp = Vector3.Zero;
        var lockedForward = Vector3.Zero;
        var freeUp = Vector3.Zero;

        // No renderer and no scene furniture: a body needs only the simulation, which SceneSetup.None still gives it
        game.Run(SceneSetup.None, start: scene =>
        {
            locked = Cube(new Vector3(-3f, 5f, 0f));
            free = Cube(new Vector3(3f, 5f, 0f));
            locked.Scene = scene;
            free.Scene = scene;
        }, update: (_, _) =>
        {
            frames++;

            // Frame 2: both bodies are in the simulation; lock one, then shove both off centre twice, a torque
            // about Y and a torque about Z
            if (frames == 2)
            {
                locked!.Get<BodyComponent>().LockRotation(lockX: true, lockY: false, lockZ: true);

                foreach (var body in new[] { locked!.Get<BodyComponent>(), free!.Get<BodyComponent>() })
                {
                    body.ApplyImpulse(new Vector3(0f, 0f, 4f), new Vector3(1f, 0f, 0f));
                    body.ApplyImpulse(new Vector3(4f, 0f, 0f), new Vector3(0f, 1f, 0f));
                }

                return;
            }

            if (frames == 40)
            {
                var lockedBody = locked!.Get<BodyComponent>();
                var freeBody = free!.Get<BodyComponent>();

                lockedUp = Vector3.Transform(Vector3.UnitY, lockedBody.Orientation);
                lockedForward = Vector3.Transform(Vector3.UnitZ, lockedBody.Orientation);
                freeUp = Vector3.Transform(Vector3.UnitY, freeBody.Orientation);
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Assert.True(Vector3.Dot(lockedUp, Vector3.UnitY) > 0.99f, $"the locked cube tilted: its up is {lockedUp}");
        Assert.True(Vector3.Dot(lockedForward, Vector3.UnitZ) < 0.99f, "the locked cube did not turn about Y, so the free axis was locked too");
        Assert.True(Vector3.Dot(freeUp, Vector3.UnitY) < 0.99f, $"the free cube did not tilt, so the run proved nothing: its up is {freeUp}");
    }

    /// <summary>A cube body with no model and no gravity, so it turns in place and nothing else moves it.</summary>
    private static Entity Cube(Vector3 position)
    {
        var entity = new Entity("cube") { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, Gravity = false } };

        entity.Transform.Position = position;

        return entity;
    }
}