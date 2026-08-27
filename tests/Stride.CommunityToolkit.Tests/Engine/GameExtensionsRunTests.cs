using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Pins the contract of <see cref="GameExtensions.Run(Game, Func{Scene, Task}, Action{Scene, GameTime}, GameContext)"/>
/// and its synchronous twin: <c>update</c> does not begin until <c>start</c> has completed, and an
/// exception from either callback escapes <c>Run</c> to the caller.
/// </summary>
/// <remarks>
/// <para>
/// These are integration tests over a real <see cref="Game"/>, because the behaviour under test is a
/// property of the engine's script scheduler and game loop, not of toolkit code alone: <c>start</c>
/// runs as a microthread, and an exception in it is only surfaced because
/// <c>Scheduler.PropagateExceptions</c> rethrows from <c>ScriptSystem.Update</c> and
/// <c>GameBase</c> lets it out of <c>Run</c>. A mock scheduler would pass regardless of whether the
/// engine still does that.
/// </para>
/// <para>
/// Each game runs in <see cref="GameContextHeadless"/>, so no window is created. No graphics
/// compositor is configured either, so nothing is drawn and no shader is ever compiled - which is
/// what lets these run without the asset compiler's <c>data/db</c> output. The graphics device is
/// still created, so a machine with no Direct3D 11 device at all cannot run them.
/// </para>
/// <para>
/// The collection disables parallelisation: the engine keeps process-wide state (the virtual file
/// system mount, the global logger), and Stride's own test suites run one game at a time for the
/// same reason.
/// </para>
/// </remarks>
[Collection(Name)]
public class GameExtensionsRunTests
{
    public const string Name = "Headless game";

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class Collection;

    /// <summary>
    /// Enough frames for every scenario below to reach its assertions, and a hard stop so a regression
    /// that stalls the loop fails the test rather than hanging the runner.
    /// </summary>
    private const int FrameBudget = 60;

    [Fact]
    public void AsyncStart_UpdateDoesNotBeginUntilStartCompletes()
    {
        using var game = new Game();

        var startCompleted = false;
        var updatesBeforeStartCompleted = 0;
        var updatesAfterStartCompleted = 0;

        game.Run(start: async _ =>
        {
            // Two real frames, not a thread-pool delay, so the test asserts scheduling order rather
            // than wall-clock timing
            await game.Script.NextFrame();
            await game.Script.NextFrame();

            startCompleted = true;
        }, update: (_, _) =>
        {
            if (startCompleted)
                updatesAfterStartCompleted++;
            else
                updatesBeforeStartCompleted++;

            if (updatesAfterStartCompleted >= 3 || game.UpdateTime.FrameCount > FrameBudget)
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(startCompleted, "start never completed");
        Assert.Equal(0, updatesBeforeStartCompleted);
        Assert.Equal(3, updatesAfterStartCompleted);
    }

    [Fact]
    public void SyncStart_RunsBeforeFirstUpdate()
    {
        using var game = new Game();

        var order = new List<string>();

        game.Run(start: _ => order.Add("start"), update: (_, _) =>
        {
            order.Add("update");

            if (order.Count >= 3 || game.UpdateTime.FrameCount > FrameBudget)
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(["start", "update", "update"], order);
    }

    [Fact]
    public void Callbacks_ReceiveTheRootScene()
    {
        using var game = new Game();

        Scene? startScene = null;
        Scene? updateScene = null;

        game.Run(start: scene => startScene = scene, update: (scene, _) =>
        {
            updateScene = scene;
            game.Exit();
        }, context: new GameContextHeadless());

        Assert.NotNull(startScene);
        Assert.Same(game.SceneSystem.SceneInstance.RootScene, startScene);
        Assert.Same(startScene, updateScene);
    }

    [Fact]
    public void Update_ReceivesTheNewRootSceneAfterASceneSwitch()
    {
        using var game = new Game();

        var replacement = new Scene();
        var scenesSeen = new List<Scene>();

        game.Run(update: (scene, _) =>
        {
            scenesSeen.Add(scene);

            // Frame 1: switch scenes the way a game does. Frame 2: check update followed the switch.
            if (scenesSeen.Count == 1)
                game.SceneSystem.SceneInstance.RootScene = replacement;
            else
                game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(2, scenesSeen.Count);
        Assert.NotSame(replacement, scenesSeen[0]);
        Assert.Same(replacement, scenesSeen[1]);
    }

    [Fact]
    public void AsyncStart_ExceptionAfterAwait_EscapesRun()
    {
        using var game = new Game();

        var updateCalled = false;

        var exception = Assert.Throws<InvalidOperationException>(() => game.Run(start: async _ =>
        {
            await game.Script.NextFrame();

            throw new InvalidOperationException("boom after await");
        }, update: (_, _) => updateCalled = true, context: new GameContextHeadless()));

        Assert.Equal("boom after await", exception.Message);
        Assert.False(updateCalled, "update ran even though start faulted");
    }

    [Fact]
    public void AsyncStart_ExceptionBeforeFirstAwait_EscapesRun()
    {
        using var game = new Game();

        var exception = Assert.Throws<InvalidOperationException>(() => game.Run(
            start: _ => throw new InvalidOperationException("boom before await"),
            update: null,
            context: new GameContextHeadless()));

        Assert.Equal("boom before await", exception.Message);
    }

    [Fact]
    public void SyncStart_Exception_EscapesRun()
    {
        using var game = new Game();

        var exception = Assert.Throws<InvalidOperationException>(() => game.Run(
            start: (Scene _) => throw new InvalidOperationException("boom from sync start"),
            context: new GameContextHeadless()));

        Assert.Equal("boom from sync start", exception.Message);
    }

    [Fact]
    public void Update_Exception_EscapesRun()
    {
        using var game = new Game();

        var exception = Assert.Throws<InvalidOperationException>(() => game.Run(
            update: (_, _) => throw new InvalidOperationException("boom from update"),
            context: new GameContextHeadless()));

        Assert.Equal("boom from update", exception.Message);
    }

    [Fact]
    public void AsyncStart_NullStart_Throws()
    {
        using var game = new Game();

        Assert.Throws<ArgumentNullException>(() => game.Run(start: (Func<Scene, Task>)null!));
    }
}
