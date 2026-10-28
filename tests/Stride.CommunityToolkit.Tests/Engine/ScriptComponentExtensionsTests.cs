using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering.Compositing;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// The camera lookups on <see cref="ScriptComponentExtensions"/> read the graphics compositor's slots, so they
/// need a script running in a game whose compositor has slots: a headless game with a compositor that has
/// camera slots and no render path, which draws nothing. The camera processor binds each slot to the camera
/// component that names it, a frame or two in, which is what the probe waits for.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class ScriptComponentExtensionsTests
{
    private const int FrameBudget = 60;

    [Fact]
    public void GetCamera_ReadsTheCompositorSlotsByNameAndNullOtherwise()
    {
        var main = new CameraComponent();
        var second = new CameraComponent();

        var result = RunProbe(["Main", "Second"], [main, second],
            script => (script.GetCamera(), script.GetCamera("Second"), script.GetCamera("Missing"), script.GetFirstCamera()),
            answered => answered.ByName is not null);

        Assert.Same(main, result.ByDefault);
        Assert.Same(second, result.ByName);
        Assert.Null(result.Missing);
        Assert.Same(main, result.First);
    }

    [Fact]
    public void GetFirstCamera_IsNullWhenTheCompositorHasNoSlot()
    {
        var frames = 0;

        var result = RunProbe([], [], script => (script.GetCamera(), null, null, script.GetFirstCamera()), _ => ++frames > 3);

        Assert.Null(result.ByDefault);
        Assert.Null(result.First);
    }

    /// <summary>
    /// Runs a headless game whose compositor has one slot per name, each bound to the camera at the same index,
    /// and a script that answers the probe every update until <paramref name="done"/> says the answer is in.
    /// </summary>
    private static Answer RunProbe(string[] slotNames, CameraComponent[] cameras, Func<ScriptComponent, Answer> probe, Func<Answer, bool> done)
    {
        using var game = new Game();

        var compositor = new GraphicsCompositor();
        var script = new Probe { Ask = probe };

        game.Run(SceneSetup.None, start: scene =>
        {
            game.SceneSystem.GraphicsCompositor = compositor;

            for (var i = 0; i < slotNames.Length; i++)
            {
                var slot = new SceneCameraSlot { Name = slotNames[i] };

                compositor.Cameras.Add(slot);
                cameras[i].Slot = slot.ToSlotId();
                scene.Entities.Add(new Entity(slotNames[i]) { cameras[i] });
            }

            scene.Entities.Add(new Entity("Probe") { script });
        }, update: (_, _) =>
        {
            if ((script.Answered && done(script.Result)) || game.UpdateTime.FrameCount > FrameBudget) game.Exit();
        }, context: new GameContextHeadless());

        Assert.True(script.Answered, "the probe script never ran");

        return script.Result;
    }

    private sealed class Probe : SyncScript
    {
        public Func<ScriptComponent, Answer> Ask { get; set; } = _ => default;

        public bool Answered { get; private set; }

        public Answer Result { get; private set; }

        public override void Update()
        {
            Result = Ask(this);
            Answered = true;
        }
    }

    private record struct Answer(CameraComponent? ByDefault, CameraComponent? ByName, CameraComponent? Missing, CameraComponent? First)
    {
        public static implicit operator Answer((CameraComponent?, CameraComponent?, CameraComponent?, CameraComponent?) tuple)
            => new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }
}