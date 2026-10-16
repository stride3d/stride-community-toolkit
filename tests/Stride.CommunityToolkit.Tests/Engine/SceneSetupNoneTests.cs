using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Pins what <see cref="SceneSetup.None"/> is for: a scene built by hand that still gets the physics
/// engine chosen on it. It used to be one shared instance whose <c>Prepare</c> did nothing, so
/// <c>SceneSetup.None.UseBepu()</c> changed the shared instance and registered no engine at all.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class SceneSetupNoneTests
{
    [Fact]
    public void None_IsANewInstanceEachTime()
    {
        Assert.NotSame(SceneSetup.None, SceneSetup.None);
    }

    [Fact]
    public void None_StillRegistersThePhysicsEngine()
    {
        using var game = new Game();

        PhysicsSetupBase? physics = null;

        game.Run(SceneSetup.None.UseBepu(), start: _ => physics = PhysicsSetupBase.Of(game), update: (_, _) => game.Exit(), context: new GameContextHeadless());

        Assert.IsType<BepuPhysicsSetup>(physics);
    }

    [Fact]
    public void None_RegistersNoEngineUnlessAsked()
    {
        using var game = new Game();

        PhysicsSetupBase? physics = null;

        game.Run(SceneSetup.None, start: _ => physics = PhysicsSetupBase.Of(game), update: (_, _) => game.Exit(), context: new GameContextHeadless());

        Assert.Same(PhysicsSetupBase.None, physics);
    }
}