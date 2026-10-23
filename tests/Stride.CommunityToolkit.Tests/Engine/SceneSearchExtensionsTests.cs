using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// The scene-wide component search: children, child scenes and the running game's scene instance.
/// </summary>
public class SceneSearchExtensionsTests
{
    [Fact]
    public void GetComponentsInScene_WalksChildrenAndChildScenes()
    {
        var scene = new Scene();
        var parent = new Entity("Parent") { new CameraComponent() };
        var child = new Entity("Child") { new CameraComponent() };
        var childScene = new Scene();

        parent.AddChild(child);
        scene.Entities.Add(parent);
        scene.Entities.Add(new Entity("Plain"));
        childScene.Entities.Add(new Entity("InChildScene") { new CameraComponent() });
        scene.Children.Add(childScene);

        Assert.Equal(3, scene.GetComponentsInScene<CameraComponent>().Count());
        Assert.Equal(2, scene.GetComponentsInScene<CameraComponent>(includeChildScenes: false).Count());
        Assert.Same(parent.Get<CameraComponent>(), scene.GetComponentInScene<CameraComponent>());
    }

    [Fact]
    public void GetComponentInScene_IsNullWhenNothingMatches()
    {
        var scene = new Scene();

        scene.Entities.Add(new Entity("Plain"));

        Assert.Null(scene.GetComponentInScene<CameraComponent>());
        Assert.Empty(scene.GetComponentsInScene<LightComponent>());
    }

    [Fact]
    public void GetComponentsInScene_MatchesDerivedComponentTypes()
    {
        var scene = new Scene();

        scene.Entities.Add(new Entity("Scripted") { new Marker() });

        Assert.Single(scene.GetComponentsInScene<ScriptComponent>());
        Assert.Single(scene.GetComponentsInScene<Marker>());
    }

    private sealed class Marker : SyncScript
    {
        public override void Update() { }
    }
}

/// <summary>
/// The game-wide search against a headless game, and the game-thread dispatch it is usually paired with.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class SceneSearchGameTests
{
    [Fact]
    public void GetComponentsInScene_OnTheGame_SeesEveryLoadedEntity()
    {
        using var game = new Game();

        var found = -1;
        var first = false;

        game.Run(SceneSetup.None, start: scene =>
        {
            var parent = new Entity("Parent") { new CameraComponent() };
            var child = new Entity("Child") { new CameraComponent() };
            var childScene = new Scene();

            parent.AddChild(child);
            parent.Scene = scene;
            childScene.Entities.Add(new Entity("InChildScene") { new CameraComponent() });
            scene.Children.Add(childScene);

            found = game.GetComponentsInScene<CameraComponent>().Count();
            first = game.GetComponentInScene<CameraComponent>() is not null;

            game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(3, found);
        Assert.True(first);
    }

    [Fact]
    public async Task RunOnGameThread_RunsTheActionOnTheGameThread()
    {
        using var game = new Game();

        var gameThread = -1;
        var actionThread = -2;
        var result = -1;
        var frames = 0;
        Task<int>? asyncResult = null;

        game.Run(SceneSetup.None, start: _ =>
        {
            gameThread = Environment.CurrentManagedThreadId;

            // Scheduled from the thread pool, as a network callback or a finished download would be
            Task.Run(() =>
            {
                game.Script.RunOnGameThread(() => actionThread = Environment.CurrentManagedThreadId);
                asyncResult = game.Script.RunOnGameThreadAsync(() => Environment.CurrentManagedThreadId);
            }).Wait();
        }, update: (_, _) =>
        {
            if (asyncResult is { IsCompleted: true } && actionThread != -2)
            {
                result = asyncResult.Result;
                game.Exit();
            }
            else if (++frames > 600)
            {
                game.Exit();
            }
        }, context: new GameContextHeadless());

        Assert.NotEqual(-1, gameThread);
        Assert.Equal(gameThread, actionThread);
        Assert.Equal(gameThread, result);
        await Task.CompletedTask;
    }

    [Fact]
    public void NullScene_ThrowsAtTheCall_EvenWhenTheSequenceIsNeverEnumerated()
    {
        Scene scene = null!;
        IGame game = null!;

        Assert.Throws<ArgumentNullException>(() => scene.GetComponentsInScene<CameraComponent>());
        Assert.Throws<ArgumentNullException>(() => game.GetComponentsInScene<CameraComponent>());
        Assert.Throws<ArgumentNullException>(() => scene.GetCamera("Main"));
    }
}