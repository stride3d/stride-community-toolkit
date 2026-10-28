using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

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

        var floor = simulation.CreateStaticBody(Vector3.Zero);
        var floorDef = b2DefaultShapeDef().InCollisionGroup(1);
        var floorBox = b2MakeBox(10f, 0.5f);

        b2CreatePolygonShape(floor, in floorDef, in floorBox);

        var falling = Box(simulation, new Vector3(-2, 3, 0), group: 2);
        var resting = Box(simulation, new Vector3(2, 3, 0), group: 1);

        for (var i = 0; i < 180; i++) simulation.Update(TimeSpan.FromSeconds(1 / 60.0));

        Assert.True(b2Body_GetPosition(falling).Y < -1f, "the group 2 box should have fallen through the group 1 floor");
        Assert.InRange(b2Body_GetPosition(resting).Y, 0.9f, 1.1f);
    }

    [Fact]
    public void InCollisionGroup_RefusesZeroAndNegativeGroups()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => b2DefaultShapeDef().InCollisionGroup(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => b2DefaultShapeDef().InCollisionGroup(-1));
    }

    /// <summary>A unit dynamic box in a collision group.</summary>
    private static B2BodyId Box(Box2DSimulation simulation, Vector3 position, int group)
    {
        var body = simulation.CreateDynamicBody(position);
        var def = b2DefaultShapeDef().InCollisionGroup(group);
        var box = b2MakeBox(0.5f, 0.5f);

        b2CreatePolygonShape(body, in def, in box);

        return body;
    }
}