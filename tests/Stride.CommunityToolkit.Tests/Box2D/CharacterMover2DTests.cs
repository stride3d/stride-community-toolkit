using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;
using static Box2D.NET.B2Bodies;
using static Stride.CommunityToolkit.Tests.Box2D.Box2DFixtures;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins <see cref="CharacterMover2D"/> against a real world: it lands and floats at the pogo rest
/// length, walks at its top speed, is stopped by a wall with its velocity clipped, jumps and comes
/// back, walks through a soft mover-category shape, and moves its entity. Stepped directly at
/// 60 Hz; the simulation registration is covered by the last test.
/// </summary>
/// <remarks>Box2D worlds are process-wide state, so this runs in the serialised collection.</remarks>
[Collection(GameExtensionsRunTests.Name)]
public class CharacterMover2DTests
{
    private const float Step = 1f / 60f;

    [Fact]
    public void Lands_AndFloatsAtThePogoRestLength()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        var mover = new CharacterMover2D(new Vector2(0, 3));

        Run(mover, simulation, 180);

        Assert.True(mover.IsOnGround);

        // Ground top at 0.5, lower centre rests three radii (0.9) above it, centre is a half height (0.5) higher.
        Assert.Equal(1.9f, mover.Position.Y, 1);
        Assert.Equal(0f, mover.Velocity.X, 3);
    }

    [Fact]
    public void Walks_AtMaxSpeed()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        var mover = new CharacterMover2D(new Vector2(0, 2));

        Run(mover, simulation, 60);
        mover.Throttle = 1;
        Run(mover, simulation, 120);

        Assert.True(mover.IsOnGround);
        Assert.Equal(mover.MaxSpeed, mover.Velocity.X, 1);
        Assert.True(mover.Position.X > 5, $"walked only to {mover.Position.X}");
    }

    [Fact]
    public void Wall_StopsIt_AndClipsTheVelocity()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        StaticBox(simulation, new Vector2(4, 3), halfWidth: 0.5f, halfHeight: 3f, CharacterMover2D.StaticCategory);
        var mover = new CharacterMover2D(new Vector2(0, 2));

        Run(mover, simulation, 60);
        mover.Throttle = 1;
        Run(mover, simulation, 180);

        Assert.True(mover.Position.X + mover.Radius <= 3.5f + 0.02f, $"went into the wall: {mover.Position.X}");
        Assert.True(mover.Position.X > 3f, $"stopped short of the wall: {mover.Position.X}");
        Assert.Equal(0f, mover.Velocity.X, 2);
        Assert.NotEmpty(mover.Planes.ToArray());
    }

    [Fact]
    public void Jump_LeavesTheGround_ThenLandsAgain()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        var mover = new CharacterMover2D(new Vector2(0, 2));

        Run(mover, simulation, 90);
        var restingHeight = mover.Position.Y;

        mover.Jump();
        Run(mover, simulation, 10);

        Assert.False(mover.IsOnGround);
        Assert.True(mover.Position.Y > restingHeight + 0.5f, $"did not rise: {mover.Position.Y}");

        Run(mover, simulation, 180);

        Assert.True(mover.IsOnGround);
        Assert.Equal(restingHeight, mover.Position.Y, 1);
    }

    [Fact]
    public void Jump_InTheAir_IsIgnored()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        var mover = new CharacterMover2D(new Vector2(0, 6));

        mover.Jump();
        Run(mover, simulation, 1);

        Assert.True(mover.Velocity.Y < 0, "an airborne jump request should not add upward speed");
    }

    [Fact]
    public void SoftMoverCategoryShape_CanBeWalkedThrough()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);

        // In the mover category the mover overlaps it but never sweeps against it, and a tiny push
        // limit makes it soft: the samples' "friendly" character.
        var body = StaticBox(simulation, new Vector2(4, 2), halfWidth: 0.5f, halfHeight: 1f, CharacterMover2D.MoverCategory);
        Span<B2ShapeId> shapes = stackalloc B2ShapeId[1];
        b2Body_GetShapes(body, shapes, 1);
        CharacterMover2D.SetResponse(shapes[0], maxPush: 0.025f, clipVelocity: false);

        var mover = new CharacterMover2D(new Vector2(0, 2));

        Run(mover, simulation, 60);
        mover.Throttle = 1;
        Run(mover, simulation, 300);

        Assert.True(mover.Position.X > 5f, $"did not get through the soft shape: {mover.Position.X}");
    }

    [Fact]
    public void RegisteredWithASimulation_StepsItself_AndMovesItsEntity()
    {
        using var simulation = new Box2DSimulation();
        Ground(simulation);
        var entity = new Entity { Transform = { Position = new Vector3(0, 0, 0.25f) } };
        var mover = new CharacterMover2D(new Vector2(0, 3)) { Entity = entity };

        simulation.RegisterSimulationUpdate(mover);

        for (var i = 0; i < 180; i++)
            simulation.Update(TimeSpan.FromSeconds(Step));

        Assert.True(mover.IsOnGround);
        Assert.Equal(mover.Position.Y, entity.Transform.Position.Y, 4);
        Assert.Equal(0.25f, entity.Transform.Position.Z);         // z is left alone
    }

    private static void Run(CharacterMover2D mover, Box2DSimulation simulation, int steps)
    {
        for (var i = 0; i < steps; i++)
            mover.Step(simulation.WorldId, Step);
    }

    // A wide static floor whose top is at y = 0.5, in the static category.
    private static void Ground(Box2DSimulation simulation)
        => StaticBox(simulation, Vector2.Zero, halfWidth: 20f, halfHeight: 0.5f, CharacterMover2D.StaticCategory);
}