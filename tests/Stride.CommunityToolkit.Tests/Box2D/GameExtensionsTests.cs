using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// <c>game.AddGrabber2D(simulation)</c> puts the grabber on the camera entity with its simulation set, in a headless game.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class GameExtensionsTests
{
    [Fact]
    public void AddGrabber2D_PutsTheScriptOnTheCameraEntityWithTheSimulation()
    {
        using var game = new Game();
        using var simulation = new Box2DSimulation();

        Grabber2DScript? grabber = null;
        Entity? camera = null;

        game.Run(SceneSetup.None, start: scene =>
        {
            camera = new Entity(CameraDefaults.MainCameraName) { new CameraComponent() };
            camera.Scene = scene;

            grabber = game.AddGrabber2D(simulation);
        }, update: (_, _) => game.Exit(), context: new GameContextHeadless());

        Assert.NotNull(grabber);
        Assert.Same(simulation, grabber.Simulation);
        Assert.Contains(grabber, camera!.Components);
    }
}