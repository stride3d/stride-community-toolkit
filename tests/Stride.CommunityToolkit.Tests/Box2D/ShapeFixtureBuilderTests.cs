using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Tests.Engine;
using Stride.Core.Mathematics;
using Xunit;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// <see cref="ShapeFixtureBuilder"/> returns the shape it attached, for every kind of primitive, and
/// <see cref="PhysicsStepSettings"/> refuses the values that would freeze or reverse a world.
/// </summary>
/// <remarks>
/// Box2D.NET keeps its worlds in process-wide state that is not safe to create and destroy from
/// several test classes at once, so every test that owns a world runs in the serialised collection.
/// </remarks>
[Collection(GameExtensionsRunTests.Name)]
public class ShapeFixtureBuilderTests
{
    [Fact]
    public void AttachShape_ReturnsTheShapeItMade_ForEveryKind()
    {
        using var simulation = new Box2DSimulation();
        var body = simulation.CreateStaticBody(Vector2.Zero);

        Primitive2D[] shapes =
        [
            Primitive2D.Rectangle(1f, 2f),
            Primitive2D.Circle(0.5f),
            Primitive2D.Triangle(1f, 1f),
            Primitive2D.Capsule(radius: 0.25f, height: 1f),
            // Shorter than two radii: attached as the circle it draws as, and still returned
            Primitive2D.Capsule(radius: 0.5f, height: 0.5f),
        ];

        foreach (var shape in shapes)
        {
            var id = ShapeFixtureBuilder.AttachShape(shape, body);

            Assert.True(b2Shape_IsValid(id), $"{shape.GetType().Name} returned no shape");
            Assert.Equal(body, b2Shape_GetBody(id));
        }

        var polygon = ShapeFixtureBuilder.AttachPolygon([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], body);

        Assert.Equal(B2ShapeType.b2_polygonShape, b2Shape_GetType(polygon));
    }

    [Fact]
    public void AttachPolygon_RoundsTheHullByTheRadius()
    {
        using var simulation = new Box2DSimulation();
        var body = simulation.CreateStaticBody(Vector2.Zero);

        Vector2[] outline = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        var sharp = ShapeFixtureBuilder.AttachPolygon(outline, body);
        var rounded = ShapeFixtureBuilder.AttachPolygon(outline, body, radius: 0.15f);

        Assert.Equal(0f, b2Shape_GetPolygon(sharp).radius);
        Assert.Equal(0.15f, b2Shape_GetPolygon(rounded).radius, Tolerances.Float);
        Assert.Throws<ArgumentOutOfRangeException>(() => ShapeFixtureBuilder.AttachPolygon(outline, body, radius: -0.1f));
    }

    [Fact]
    public void AttachBox_CentresTheBoxOnTheOffset()
    {
        using var simulation = new Box2DSimulation();
        var body = simulation.CreateStaticBody(Vector2.Zero);

        var box = ShapeFixtureBuilder.AttachBox(new Vector2(2f, 1f), body, new Vector2(3f, -4f));
        var centre = b2Shape_GetPolygon(box).centroid;
        var bounds = b2Shape_GetAABB(box);

        Assert.Equal(3f, centre.X, Tolerances.Geometry);
        Assert.Equal(-4f, centre.Y, Tolerances.Geometry);

        // The bounds carry Box2D's margin, a couple of centimetres a side
        Assert.Equal(2f, bounds.upperBound.X - bounds.lowerBound.X, 0.05f);
        Assert.Equal(1f, bounds.upperBound.Y - bounds.lowerBound.Y, 0.05f);
    }

    [Fact]
    public void InCategory_SetsTheCategoryAndTheMask_AndLeavesTheGroup()
    {
        using var simulation = new Box2DSimulation();
        var body = simulation.CreateStaticBody(Vector2.Zero);

        var everything = b2Shape_GetFilter(ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(0.5f), body, ShapeFixtureBuilder.CreateDefaultShapeDef().InCategory(0x4)));
        var masked = b2Shape_GetFilter(ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(0.5f), body, ShapeFixtureBuilder.CreateDefaultShapeDef().InCollisionGroup(2).InCategory(0x8, 0x3)));

        Assert.Equal(0x4UL, everything.categoryBits);
        Assert.Equal(ulong.MaxValue, everything.maskBits);
        Assert.Equal(0x8UL, masked.categoryBits);
        Assert.Equal(0x3UL, masked.maskBits);
        Assert.Equal(2, masked.groupIndex);
    }

    [Fact]
    public void StepSettings_RefuseWhatWouldFreezeOrReverseTheWorld()
    {
        var settings = new PhysicsStepSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.TargetHz = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxStepsPerFrame = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.SubStepCount = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.TimeScale = -0.5f);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicsStepSettings { WorkerCount = -1 });

        // A paused world is a time scale of zero, which is allowed
        settings.TimeScale = 0f;

        Assert.Equal(60, settings.TargetHz);
    }
}