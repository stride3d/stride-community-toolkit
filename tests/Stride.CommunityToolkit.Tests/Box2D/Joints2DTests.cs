using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;
using static Box2D.NET.B2DistanceJoints;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2RevoluteJoints;
using static Stride.CommunityToolkit.Tests.Box2D.Box2DFixtures;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// Pins <see cref="Joints2D"/> and the <see cref="Box2DSimulation.Joints"/> forwarders against a
/// real Box2D world - no game needed, the world is a plain object: the world pivot lands in each
/// body's frame, options reach the joint, the distance joint's default length is the anchor
/// distance, a destroyed joint is invalid, and an entity without a body is refused with a hint.
/// </summary>
/// <remarks>
/// Box2D.NET keeps its worlds in process-wide state that is not safe to create and destroy from
/// several test classes at once, so every test that owns a world runs in the serialised collection.
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class Joints2DTests
{
    [Fact]
    public void CreateRevolute_PutsThePivotInEachBodysFrame()
    {
        using var simulation = new Box2DSimulation();
        var a = Box(simulation, new Vector3(0, 0, 0));
        var b = Box(simulation, new Vector3(2, 0, 0));

        var joint = simulation.Joints.CreateRevolute(a, b, new Vector2(1, 0));

        Assert.True(Joints2D.IsValid(joint));
        Assert.Equal(a, b2Joint_GetBodyA(joint));
        Assert.Equal(b, b2Joint_GetBodyB(joint));

        var frameA = b2Joint_GetLocalFrameA(joint);
        var frameB = b2Joint_GetLocalFrameB(joint);

        Assert.Equal(1, frameA.p.X, 4);        // one unit to the right of A
        Assert.Equal(-1, frameB.p.X, 4);       // one unit to the left of B
    }

    [Fact]
    public void CreateRevolute_AppliesOptions()
    {
        using var simulation = new Box2DSimulation();
        var a = Box(simulation, new Vector3(0, 0, 0));
        var b = Box(simulation, new Vector3(2, 0, 0));

        var joint = simulation.Joints.CreateRevolute(a, b, new Vector2(1, 0), new RevoluteJointOptions
        {
            EnableMotor = true,
            MotorSpeed = 2.5f,
            MaxMotorTorque = 40,
            EnableLimit = true,
            LowerAngle = -0.5f,
            UpperAngle = 0.5f,
        });

        Assert.True(b2RevoluteJoint_IsMotorEnabled(joint));
        Assert.Equal(2.5f, b2RevoluteJoint_GetMotorSpeed(joint), 4);
        Assert.Equal(40, b2RevoluteJoint_GetMaxMotorTorque(joint), 4);
        Assert.True(b2RevoluteJoint_IsLimitEnabled(joint));
        Assert.Equal(-0.5f, b2RevoluteJoint_GetLowerLimit(joint), 4);
        Assert.Equal(0.5f, b2RevoluteJoint_GetUpperLimit(joint), 4);
    }

    [Fact]
    public void CreateDistance_DefaultsTheLengthToTheAnchorDistance()
    {
        using var simulation = new Box2DSimulation();
        var a = Box(simulation, new Vector3(0, 0, 0));
        var b = Box(simulation, new Vector3(5, 0, 0));

        var joint = simulation.Joints.CreateDistance(a, b, new Vector2(0.5f, 0), new Vector2(4.5f, 0));

        Assert.Equal(4, b2DistanceJoint_GetLength(joint), 4);
    }

    [Fact]
    public void Destroy_MakesTheJointInvalid_AndIsSafeTwice()
    {
        using var simulation = new Box2DSimulation();
        var a = Box(simulation, new Vector3(0, 0, 0));
        var b = Box(simulation, new Vector3(2, 0, 0));

        var joint = simulation.Joints.CreateWeld(a, b, new Vector2(1, 0));

        simulation.Joints.Destroy(joint);

        Assert.False(simulation.Joints.IsValid(joint));

        simulation.Joints.Destroy(joint);       // no throw
    }

    [Fact]
    public void EveryFactory_ProducesAValidJoint()
    {
        using var simulation = new Box2DSimulation();
        var a = Box(simulation, new Vector3(0, 0, 0));
        var b = Box(simulation, new Vector3(2, 0, 0));
        var pivot = new Vector2(1, 0);

        Assert.True(Joints2D.IsValid(simulation.Joints.CreatePrismatic(a, b, pivot, Vector2.UnitX)));
        Assert.True(Joints2D.IsValid(simulation.Joints.CreateWheel(a, b, pivot, Vector2.UnitY)));
        Assert.True(Joints2D.IsValid(simulation.Joints.CreateMotor(a, b)));
        Assert.True(Joints2D.IsValid(simulation.Joints.CreateMotor(a, b, pivot)));
        Assert.True(Joints2D.IsValid(simulation.Joints.CreateFilter(a, b)));
    }

    [Fact]
    public void EntityOverload_RefusesAnEntityWithoutABody()
    {
        using var simulation = new Box2DSimulation();
        var with = new Entity("with");
        var without = new Entity("without");
        simulation.CreateDynamicBody(with, Vector3.Zero);

        var ex = Assert.Throws<InvalidOperationException>(() => simulation.Joints.CreateWeld(with, without, Vector2.Zero));

        Assert.Contains("CreateDynamicBody", ex.Message);
    }
}