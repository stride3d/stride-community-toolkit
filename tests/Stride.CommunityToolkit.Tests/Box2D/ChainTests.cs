using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Stride.CommunityToolkit.Tests.Box2D.Box2DFixtures;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins <see cref="ShapeFixtureBuilder.AttachChain"/>: the segments come out in order, a floor
/// listed left to right holds a body up, and too few points are refused.
/// </summary>
/// <remarks>
/// Box2D.NET keeps its worlds in process-wide state that is not safe to create and destroy from
/// several test classes at once, so every test that owns a world runs in the serialised collection.
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class ChainTests
{
    [Fact]
    public void AttachChain_CreatesOneSegmentPerEdge()
    {
        using var simulation = new Box2DSimulation();
        var ground = simulation.CreateStaticBody(Vector3.Zero);

        var chain = ShapeFixtureBuilder.AttachChain([new(-5, 0), new(0, 0), new(5, 1), new(10, 1)], ground);

        Assert.Equal(3, b2Chain_GetSegmentCount(chain));
    }

    [Fact]
    public void AttachChain_ListedRightToLeft_HoldsABodyUp()
    {
        using var simulation = new Box2DSimulation();
        var ground = simulation.CreateStaticBody(Vector3.Zero);

        // A chain collides on the right of its direction of travel: a floor runs right to left.
        ShapeFixtureBuilder.AttachChain([new(20, 0), new(-20, 0)], ground);

        var box = Box(simulation, new Vector3(0, 1, 0));

        for (var i = 0; i < 120; i++)
            simulation.Update(TimeSpan.FromSeconds(1 / 60.0));

        // Resting on the chain: half a unit up, not fallen through.
        Assert.InRange(b2Body_GetPosition(box).Y, 0.4f, 0.6f);
    }

    [Fact]
    public void AttachChain_RefusesTooFewPoints()
    {
        using var simulation = new Box2DSimulation();
        var ground = simulation.CreateStaticBody(Vector3.Zero);

        Assert.Throws<ArgumentException>(() => ShapeFixtureBuilder.AttachChain([new(0, 0)], ground));
        Assert.Throws<ArgumentException>(() => ShapeFixtureBuilder.AttachChain([new(0, 0), new(1, 0), new(1, 1)], ground, isLoop: true));
    }
}