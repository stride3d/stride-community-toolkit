using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders.Compiler;
using Stride.Streaming;
using System.Reflection;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Pins what <see cref="GameSettingsExtensions.UseGameSettings"/> does for a game that has no
/// <c>GameSettings</c> asset: registers the settings where the engine's subsystems look for them,
/// applies rendering settings to the device manager before <c>Run</c>, and applies the compilation
/// mode once the effect system exists.
/// </summary>
/// <remarks>
/// <para>
/// The compilation mode lives in a private field of <see cref="EffectSystem"/>
/// (<c>effectCompilerParameters</c>), which the engine only writes through
/// <see cref="EffectSystem.SetCompilationMode"/>. There is no public read-back, so the mode test
/// reads the field by reflection; if the engine renames it the test fails with a clear message
/// rather than silently passing.
/// </para>
/// <para>
/// Same harness as <see cref="GameExtensionsRunTests"/>: headless, no compositor, so nothing is
/// drawn and no shader is compiled - the test asserts the mode that <em>would</em> be used, not a
/// compilation.
/// </para>
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class GameSettingsExtensionsTests
{
    [Fact]
    public void UseGameSettings_RegistersTheServiceOnceTheGameStarts()
    {
        using var game = new Game();

        var returned = game.UseGameSettings();

        // Not yet: registering before Run would collide with the engine's own registration in a
        // project that has the asset. The service appears at WindowCreated, before any subsystem reads it.
        Assert.Null(game.Services.GetService<IGameSettingsService>());
        Assert.Empty(returned.Configurations);
        Assert.Equal(default, returned.CompilationMode);

        IGameSettingsService? service = null;

        game.Run(SceneSetup.None, update: (_, _) =>
        {
            service = game.Services.GetService<IGameSettingsService>();
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.NotNull(service);
        Assert.Same(returned, service.Settings);
    }

    [Fact]
    public void UseGameSettings_CompilationMode_ReachesTheEffectSystem()
    {
        using var game = new Game();

        // AppStore is the one mode whose parameters differ from the engine's default (Debug = false,
        // level 2), so it is the one that proves the value travelled.
        game.UseGameSettings(settings => settings.CompilationMode = CompilationMode.AppStore);

        EffectCompilerParameters? parameters = null;

        game.Run(SceneSetup.None, update: (_, _) =>
        {
            parameters = ReadCompilerParameters(game);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.NotNull(parameters);
        Assert.False(parameters.Value.Debug);
        Assert.Equal(2, parameters.Value.OptimizationLevel);
    }

    [Fact]
    public void UseGameSettings_AppliesRenderingSettingsToTheDeviceManagerBeforeRun()
    {
        using var game = new Game();

        game.UseGameSettings(settings =>
        {
            var rendering = settings.GetOrCreateConfiguration<RenderingSettings>();
            rendering.DefaultBackBufferWidth = 640;
            rendering.DefaultBackBufferHeight = 360;
            rendering.DefaultGraphicsProfile = GraphicsProfile.Level_10_0;
        });

        var deviceManager = Assert.IsType<GraphicsDeviceManager>(game.GraphicsDeviceManager);

        Assert.Equal(640, deviceManager.PreferredBackBufferWidth);
        Assert.Equal(360, deviceManager.PreferredBackBufferHeight);
        Assert.Equal([GraphicsProfile.Level_10_0], deviceManager.PreferredGraphicsProfile);
        Assert.Equal(GraphicsProfile.Level_10_0, deviceManager.ShaderProfile);
    }

    [Fact]
    public void UseGameSettings_CalledTwice_AdjustsAndReturnsTheSameSettings()
    {
        using var game = new Game();

        var first = game.UseGameSettings(settings => settings.GetOrCreateConfiguration<StreamingSettings>());

        // The second call sees the first call's configuration, adds its own, and its rendering settings
        // still reach the device manager - so two independent helpers can each contribute a piece.
        var second = game.UseGameSettings(settings =>
        {
            Assert.Single(settings.Configurations.OfType<StreamingSettings>());

            settings.GetOrCreateConfiguration<RenderingSettings>().DefaultBackBufferWidth = 800;
        });

        Assert.Same(first, second);
        Assert.Equal(2, second.Configurations.Count);
        Assert.Equal(800, Assert.IsType<GraphicsDeviceManager>(game.GraphicsDeviceManager).PreferredBackBufferWidth);
    }

    [Fact]
    public void UseGameSettings_CompilationModeSetOnASecondCall_ReachesTheEffectSystem()
    {
        using var game = new Game();

        game.UseGameSettings();
        game.UseGameSettings(settings => settings.CompilationMode = CompilationMode.AppStore);

        EffectCompilerParameters? parameters = null;

        game.Run(SceneSetup.None, update: (_, _) =>
        {
            parameters = ReadCompilerParameters(game);
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.NotNull(parameters);
        Assert.False(parameters.Value.Debug);
        Assert.Equal(2, parameters.Value.OptimizationLevel);
    }

    [Fact]
    public void UseGameSettings_AfterRun_Throws()
    {
        using var game = new Game();

        InvalidOperationException? thrown = null;

        game.Run(SceneSetup.None, update: (_, _) =>
        {
            thrown = Assert.Throws<InvalidOperationException>(() => game.UseGameSettings());
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.NotNull(thrown);
    }

    private static EffectCompilerParameters ReadCompilerParameters(Game game)
    {
        var field = typeof(EffectSystem).GetField("effectCompilerParameters", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("EffectSystem no longer has a private 'effectCompilerParameters' field; update the test to whatever now holds the compilation mode.");

        return (EffectCompilerParameters)field.GetValue(game.EffectSystem)!;
    }
}