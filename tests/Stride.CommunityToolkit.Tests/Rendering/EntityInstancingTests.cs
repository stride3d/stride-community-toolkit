using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// The two traps of ARCHITECTURE items 8 and 9, against a real headless game: an instance that leaves
/// its scene is unregistered, and instancing is either set up for you or warned about.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class EntityInstancingTests
{
    [Fact]
    public void AnInstanceThatLeavesTheSceneIsUnregistered()
    {
        using var game = new Game();

        var instancing = new EntityInstancing();
        var countBefore = -1;
        var countAfter = -1;
        var countAfterLateRegistration = -1;

        game.Run(SceneSetup.None, start: scene =>
        {
            // Registered once in the scene: the instancing hooks the scene's removals straight away
            var inScene = new Entity("in scene");
            inScene.Scene = scene;
            instancing.AddInstance(inScene);

            // Registered before it joins the scene: the hook is taken on the next update
            var late = new Entity("late");
            instancing.AddInstance(late);
            late.Scene = scene;
            instancing.Update();

            var stays = new Entity("stays");
            stays.Scene = scene;
            instancing.AddInstance(stays);

            countBefore = instancing.RegisteredInstanceCount;

            inScene.Scene = null;
            countAfter = instancing.RegisteredInstanceCount;

            late.Scene = null;
            countAfterLateRegistration = instancing.RegisteredInstanceCount;

            game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(3, countBefore);
        Assert.Equal(2, countAfter);
        Assert.Equal(1, countAfterLateRegistration);
    }

    [Fact]
    public void UseInstancingPutsTheRenderFeatureInTheCompositor()
    {
        using var game = new Game();

        var present = false;

        game.Run(Bare().UseInstancing(), start: _ =>
        {
            present = HasInstancingFeature(game);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(present);
    }

    [Fact]
    public void BufferUploadAddsTheRenderFeatureItself()
    {
        using var game = new Game();

        var before = true;
        var after = false;

        game.Run(Bare(), start: _ =>
        {
            before = HasInstancingFeature(game);

            using var instancing = new BufferedEntityInstancing();

            game.AddInstancingBufferUpload(instancing);
            after = HasInstancingFeature(game);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.False(before);
        Assert.True(after);
    }

    [Fact]
    public void AMasterWithoutTheRenderFeatureIsWarnedAbout()
    {
        using var game = new Game();

        var warnings = new List<string>();
        var frames = 0;

        void OnLogged(ILogMessage message)
        {
            if (message.Type == LogMessageType.Warning && message.Text.Contains("InstancingRenderFeature")) warnings.Add(message.Text);
        }

        GlobalLogger.GlobalMessageLogged += OnLogged;

        try
        {
            game.Run(Bare(), start: scene =>
            {
                var master = new Entity("master") { new ModelComponent(), new InstancingComponent { Type = new EntityInstancing() } };
                master.Scene = scene;
            }, update: (_, _) =>
            {
                if (++frames >= 2) game.Exit();
            }, context: new GameContextHeadless());
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= OnLogged;
        }

        var warning = Assert.Single(warnings);

        Assert.Contains("master", warning);
    }

    // The compositor and nothing that needs a shader: the test host has none
    private static SceneSetup3D Bare() => new() { Camera = null, Lighting = null, Skybox = null, Ground = null };

    private static bool HasInstancingFeature(Game game)
        => game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().Any(mesh => mesh.RenderFeatures.OfType<InstancingRenderFeature>().Any());
}