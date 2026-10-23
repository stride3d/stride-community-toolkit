using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Engine;
using Stride.Games;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// The body mode read through the setup path: <c>CreatePrimitive</c> and the grounds with Bepu chosen, in a
/// headless game, with the core's own options and with the package's.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class BodyModeThroughSetupTests
{
    [Fact]
    public void CreatePrimitiveAttachesWhatTheModeAsksFor()
    {
        using var game = new Game();

        CollidableComponent? dynamic = null, isStatic = null, kinematic = null, none = null, flatStatic = null, flatKinematic = null;

        game.Run(SceneSetup.None.UseBepu(), start: _ =>
        {
            dynamic = game.CreatePrimitive(Primitive3D.Cube()).Get<CollidableComponent>();
            isStatic = game.CreatePrimitive(Primitive3D.Cube(), new PrimitiveEntityOptions { Body = BodyMode.Static }).Get<CollidableComponent>();
            kinematic = game.CreatePrimitive(Primitive3D.Cube(), new BepuPhysicsOptions { Body = BodyMode.Kinematic }).Get<CollidableComponent>();
            none = game.CreatePrimitive(Primitive3D.Cube(), new PrimitiveEntityOptions { Body = BodyMode.None }).Get<CollidableComponent>();
            flatStatic = game.CreatePrimitive(Primitive2D.Square(), new PrimitiveEntityOptions { Body = BodyMode.Static }).Get<CollidableComponent>();
            flatKinematic = game.CreatePrimitive(Primitive2D.Circle(), new BepuPhysicsOptions { Body = BodyMode.Kinematic }).Get<CollidableComponent>();

            game.Exit();
        }, context: new GameContextHeadless());

        Assert.False(Assert.IsType<BodyComponent>(dynamic).Kinematic);
        Assert.IsType<StaticComponent>(isStatic);
        Assert.True(Assert.IsType<BodyComponent>(kinematic).Kinematic);
        Assert.Null(none);
        Assert.IsType<StaticComponent>(flatStatic);
        Assert.True(Assert.IsType<Body2DComponent>(flatKinematic).Kinematic);
    }

    [Fact]
    public void GroundsAreStaticAndLeaveTheOptionsAsGiven()
    {
        using var game = new Game();

        var options = new BepuPhysicsOptions { Body = BodyMode.Kinematic, Mass = 2f, EntityName = null };
        CollidableComponent? ground = null, flatGround = null, visual = null;

        game.Run(SceneSetup.None.UseBepu(), start: _ =>
        {
            ground = game.Add3DGround(options: options).Get<CollidableComponent>();
            flatGround = game.Add2DGround(options: new PrimitiveEntityOptions()).Get<CollidableComponent>();
            visual = game.Add3DGround(options: new PrimitiveEntityOptions { Body = BodyMode.None }).Get<CollidableComponent>();

            game.Exit();
        }, context: new GameContextHeadless());

        Assert.IsType<StaticComponent>(ground);
        Assert.IsType<StaticComponent>(flatGround);
        Assert.Null(visual);

        // The caller's options are not changed by the call: neither the mode nor the defaults filled in
        Assert.Equal(BodyMode.Kinematic, options.Body);
        Assert.Null(options.EntityName);
        Assert.Null(options.Material);
    }

    [Fact]
    public void WithBodyCopiesTheOptionsWhateverTheirType()
    {
        var options = new BepuPhysicsOptions { Body = BodyMode.Kinematic, Mass = 2f, EntityName = "Crate" };

        var copy = Assert.IsType<BepuPhysicsOptions>(options.WithBody(BodyMode.Static));

        Assert.NotSame(options, copy);
        Assert.Equal(BodyMode.Static, copy.Body);
        Assert.Equal(2f, copy.Mass);
        Assert.Equal("Crate", copy.EntityName);
        Assert.Equal(BodyMode.Kinematic, options.Body);
    }

    [Fact]
    public void AShapeRefusesADepthThatIsNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Primitive2D.Circle(depth: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Primitive2D.Square(depth: -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Primitive2D.Rectangle(depth: float.NaN));
        Assert.Equal(2.5f, Primitive2D.Triangle(depth: 2.5f).Depth);
    }
}