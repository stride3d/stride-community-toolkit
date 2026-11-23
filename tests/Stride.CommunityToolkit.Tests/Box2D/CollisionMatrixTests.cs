using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Types;
using static Stride.CommunityToolkit.Tests.Box2D.Box2DFixtures;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// The collision matrix reaches Box2D: a pair of groups told not to collide passes through each other, while a pair
/// never configured stacks as before, in one simulation stepped without a game.
/// </summary>
[Collection(GameExtensionsRunTests.Name)]
public class CollisionMatrixTests
{
    [Fact]
    public void GroupsToldNotToCollide_PassThroughEachOther()
    {
        using var simulation = new Box2DSimulation();

        simulation.CollisionMatrix.SetCollision(1, 2, false);

        // The floor keeps Box2D's default category and joins group 1
        StaticBox(simulation, Vector2.Zero, halfWidth: 10f, halfHeight: 0.5f, categoryBits: 1, group: 1);

        var falling = Box(simulation, new Vector3(-2, 3, 0), group: 2);
        var resting = Box(simulation, new Vector3(2, 3, 0), group: 1);

        StepFor(simulation, 180);

        Assert.True(b2Body_GetPosition(falling).Y < -1f, "the group 2 box should have fallen through the group 1 floor");
        Assert.InRange(b2Body_GetPosition(resting).Y, 0.9f, 1.1f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InCollisionGroup_RefusesZeroAndNegativeGroups(int group)
        => Assert.Throws<ArgumentOutOfRangeException>(() => b2DefaultShapeDef().InCollisionGroup(group));
}