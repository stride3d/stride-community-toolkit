using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// What <c>AddBepuPhysics</c> attaches: the component the body mode asks for, the fitted collider in an empty
/// compound, the caller's own compound kept, the mass and interpolation rules, and a wide 2D capsule clamped the way
/// its mesh is.
/// </summary>
public class AddBepuPhysicsColliderTests
{
    [Fact]
    public void WideCapsuleGetsAColliderClampedLikeItsMesh()
    {
        // A height under two radii: the mesh keeps a straight part of a hundredth of a radius, and so does the collider
        var collider = Assert.IsType<CapsuleCollider>(CommunityToolkit.Bepu.EntityExtensions.ColliderFor(Primitive2D.Capsule(radius: 0.5f, height: 0.8f), depth: 1f));

        Assert.Equal(0.5f, collider.Radius);
        Assert.Equal(0.005f, collider.Length, 0.0001f);
    }

    [Fact]
    public void EmptyCompoundGetsTheShapeFittedToThePrimitive()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Box(new Vector3(8f, 3f, 0.4f)), new BepuPhysicsOptions
        {
            Component = new StaticComponent { Collider = new CompoundCollider() },
        });

        var box = Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(entity)));

        Assert.Equal(new Vector3(8f, 3f, 0.4f), box.Size);
    }

    [Fact]
    public void NoComponentGivenStillGetsTheFittedShape()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Cube(2f), new BepuPhysicsOptions());

        Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(entity)));
        Assert.IsType<BodyComponent>(entity.Get<CollidableComponent>());
    }

    [Fact]
    public void CompoundThatHoldsAShapeIsKeptAsItIs()
    {
        var entity = EntityWithModel();
        var own = new BoxCollider { Mass = 2f };

        entity.AddBepuPhysics(Primitive3D.Cube(1.2f), new BepuPhysicsOptions
        {
            Component = new BodyComponent { Collider = new CompoundCollider { Colliders = { own } } },
        });

        // One collider, the caller's: a second one would add its mass to the body as well
        Assert.Same(own, Assert.Single(CollidersOf(entity)));
    }

    [Fact]
    public void MassGoesOnTheFittedShape()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Sphere(0.5f), new BepuPhysicsOptions { Mass = 4f });

        Assert.Equal(4f, Assert.Single(CollidersOf(entity)).Mass);
    }

    [Fact]
    public void TwoDimensionalHelperFollowsTheSameRule()
    {
        var fitted = new Entity();
        var kept = new Entity();
        var own = new BoxCollider { Mass = 3f };

        fitted.AddBepuPhysics(Primitive2D.Rectangle(4f, 0.5f), new BepuPhysicsOptions
        {
            Component = new Body2DComponent { Collider = new CompoundCollider() },
        });

        kept.AddBepuPhysics(Primitive2D.Rectangle(4f, 0.5f), new BepuPhysicsOptions
        {
            Component = new Body2DComponent { Collider = new CompoundCollider { Colliders = { own } } },
        });

        Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(fitted)));
        Assert.Same(own, Assert.Single(CollidersOf(kept)));
    }

    [Fact]
    public void TheDynamicModeIsABodyInterpolatedByDefault()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Cube(), new BepuPhysicsOptions());

        var body = Assert.IsType<BodyComponent>(entity.Get<CollidableComponent>());

        Assert.False(body.Kinematic);
        Assert.Equal(InterpolationMode.Interpolated, body.InterpolationMode);
    }

    [Fact]
    public void InterpolationCanBeTurnedOff()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Cube(), new BepuPhysicsOptions { Interpolation = false });

        Assert.Equal(InterpolationMode.None, Assert.IsType<BodyComponent>(entity.Get<CollidableComponent>()).InterpolationMode);
    }

    [Fact]
    public void TheKinematicModeIsABodyThatIsKinematic()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Cube(), new BepuPhysicsOptions { Body = BodyMode.Kinematic });

        var body = Assert.IsType<BodyComponent>(entity.Get<CollidableComponent>());

        Assert.True(body.Kinematic);
        Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(entity)));
    }

    [Fact]
    public void TheStaticModeIsAStaticWithTheFittedShapeAndNoMass()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Plane(10f, 10f), new BepuPhysicsOptions { Body = BodyMode.Static, Mass = 5f });

        Assert.IsType<StaticComponent>(entity.Get<CollidableComponent>());

        // A static has no mass to set; the value is logged and the collider keeps Bepu's default
        Assert.Equal(1f, Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(entity))).Mass);
    }

    [Fact]
    public void TheNoneModeAttachesNothing()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Cube(), new BepuPhysicsOptions { Body = BodyMode.None });

        Assert.Null(entity.Get<CollidableComponent>());
    }

    [Fact]
    public void ASuppliedComponentWinsOverTheMode()
    {
        var entity = EntityWithModel();
        var supplied = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = InterpolationMode.None };

        entity.AddBepuPhysics(Primitive3D.Cube(), new BepuPhysicsOptions { Body = BodyMode.Static, Component = supplied });

        Assert.Same(supplied, entity.Get<CollidableComponent>());
        Assert.Equal(InterpolationMode.None, supplied.InterpolationMode);
    }

    [Fact]
    public void ASuppliedComponentWithTheNoneModeIsRefused()
    {
        var entity = EntityWithModel();
        var options = new BepuPhysicsOptions { Body = BodyMode.None, Component = new BodyComponent { Collider = new CompoundCollider() } };

        Assert.Throws<InvalidOperationException>(() => entity.AddBepuPhysics(Primitive3D.Cube(), options));
    }

    [Fact]
    public void TwoDimensionalModesAndDepthFollowTheShape()
    {
        var kinematic = new Entity();
        var isStatic = new Entity();

        kinematic.AddBepuPhysics(Primitive2D.Square(1f, depth: 4f), new BepuPhysicsOptions { Body = BodyMode.Kinematic });
        isStatic.AddBepuPhysics(Primitive2D.Rectangle(4f, 0.5f), new BepuPhysicsOptions { Body = BodyMode.Static });

        var body = Assert.IsType<Body2DComponent>(kinematic.Get<CollidableComponent>());

        Assert.True(body.Kinematic);
        Assert.Equal(4f, Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(kinematic))).Size.Z);
        Assert.IsType<StaticComponent>(isStatic.Get<CollidableComponent>());
    }

    [Fact]
    public void TheSameOptionsServeSeveralEntities()
    {
        var options = new BepuPhysicsOptions { Body = BodyMode.Kinematic };
        var first = EntityWithModel();
        var second = EntityWithModel();

        first.AddBepuPhysics(Primitive3D.Cube(), options);
        second.AddBepuPhysics(Primitive3D.Cube(), options);

        // A component per entity: the options make a fresh one each time
        Assert.NotSame(first.Get<CollidableComponent>(), second.Get<CollidableComponent>());
    }

    private static Entity EntityWithModel() => new() { new ModelComponent(new Model()) };

    private static IList<ColliderBase> CollidersOf(Entity entity)
        => Assert.IsType<CompoundCollider>(entity.Get<CollidableComponent>().Collider).Colliders;
}