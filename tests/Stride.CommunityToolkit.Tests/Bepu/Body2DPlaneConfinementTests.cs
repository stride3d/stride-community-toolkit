using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using System.Diagnostics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// The plane confinement of <see cref="Body2DComponent"/>, in a headless game with a stepping simulation: a
/// body that starts off the plane is pulled back toward it.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class Body2DPlaneConfinementTests
{
    private const float StartZ = 0.5f;

    // How long the correction gets, on the game's clock, and how long the test waits for it on the wall clock
    private static readonly TimeSpan Settle = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);

    [Fact]
    public void ABodyOffThePlane_IsPulledBackTowardIt()
    {
        using var game = new Game();

        Entity? square = null;
        float? firstZ = null;
        var lastZ = float.NaN;
        var clock = Stopwatch.StartNew();

        game.Run(SceneSetup.None, start: scene =>
        {
            square = new Entity("square") { new Body2DComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
            square.Transform.Position = new Vector3(0, 0, StartZ);
            square.Scene = scene;
        }, update: (_, _) =>
        {
            var body = square!.Get<Body2DComponent>();

            firstZ ??= body.Position.Z;
            lastZ = body.Position.Z;

            // Paced by game time, which is what the simulation steps on; a headless game runs its frames
            // much faster than that clock
            if (game.UpdateTime.Total >= Settle || clock.Elapsed > Deadline) game.Exit();
        }, context: new GameContextHeadless());

        Assert.Equal(StartZ, firstZ!.Value, Tolerances.Projection);
        Assert.True(MathF.Abs(lastZ) < StartZ / 2f, $"The body was not pulled toward the plane: it started at z {firstZ} and is at z {lastZ}.");
    }
}