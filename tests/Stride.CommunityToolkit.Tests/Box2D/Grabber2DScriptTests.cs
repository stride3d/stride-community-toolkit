using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Xunit;
using static Box2D.NET.B2Bodies;
using static Stride.CommunityToolkit.Tests.Box2D.Box2DFixtures;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins the lifecycle of <see cref="Grabber2DScript"/> in a headless game: a grab holds the body
/// and pulling the carry point moves it, a release lets go and raises the event, and a body that
/// is not dynamic is refused. The mouse path is not driven; <see cref="Grabber2DScript.Grab"/>,
/// <see cref="Grabber2DScript.Carry"/> and <see cref="Grabber2DScript.Release"/> are called directly.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class Grabber2DScriptTests
{
    [Fact]
    public void Grab_Carry_Release_MovesTheBodyAndLetsGo()
    {
        using var game = new Game();
        using var simulation = new Box2DSimulation();
        simulation.Gravity = Vector2.Zero;                    // so the only motion is the grabber's

        Grabber2DScript? grabber = null;
        B2BodyId body = default;
        var frames = 0;
        var grabbed = false;
        var startX = 0f;
        var endX = 0f;
        var heldAfterRelease = true;
        var released = 0;

        game.Run(SceneSetup.None, start: scene =>
        {
            grabber = new Grabber2DScript { Simulation = simulation };
            grabber.Released += _ => released++;

            var camera = new Entity("camera") { new CameraComponent(), grabber };
            camera.Scene = scene;

            body = Box(simulation, Vector3.Zero);
        }, update: (_, _) =>
        {
            frames++;

            if (frames == 2)
            {
                grabbed = grabber!.Grab(body, new Vector2(0.2f, 0));
                startX = b2Body_GetPosition(body).X;
                return;
            }

            if (frames is > 2 and <= 40)
            {
                grabber!.Carry(new Vector2(3, 0));
                simulation.Update(TimeSpan.FromSeconds(1 / 60.0));
                return;
            }

            if (frames == 41)
            {
                endX = b2Body_GetPosition(body).X;
                grabber!.Release();
                heldAfterRelease = grabber.Held is not null;
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Assert.True(grabbed, "Grab returned false for a dynamic body.");
        Assert.True(endX > startX + 0.5f, $"Expected the body to be pulled toward +x; it went from {startX} to {endX}.");
        Assert.False(heldAfterRelease);
        Assert.Equal(1, released);
    }

    [Fact]
    public void Grab_RefusesAStaticBody()
    {
        using var game = new Game();
        using var simulation = new Box2DSimulation();

        Grabber2DScript? grabber = null;
        B2BodyId wall = default;
        var grabbed = true;
        var frames = 0;

        game.Run(SceneSetup.None, start: scene =>
        {
            grabber = new Grabber2DScript { Simulation = simulation };

            var camera = new Entity("camera") { new CameraComponent(), grabber };
            camera.Scene = scene;

            wall = simulation.CreateStaticBody(Vector3.Zero);
        }, update: (_, _) =>
        {
            if (++frames < 2) return;

            grabbed = grabber!.Grab(wall, Vector2.Zero);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.False(grabbed);
        Assert.Null(grabber!.Held);
    }
}