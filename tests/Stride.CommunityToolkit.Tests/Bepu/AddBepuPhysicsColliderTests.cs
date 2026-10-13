using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Bepu;

/// <summary>
/// Which collider a body ends up with when the physics helpers are handed a component: the shape
/// fitted to the primitive goes into an empty compound collider, and a compound that already holds
/// the caller's own shape is kept as it is. No simulation is needed; the component is read as built.
/// </summary>
public class AddBepuPhysicsColliderTests
{
    [Fact]
    public void EmptyCompoundGetsTheShapeFittedToThePrimitive()
    {
        var entity = EntityWithModel();

        entity.AddBepuPhysics(Primitive3D.Box(new Vector3(8f, 3f, 0.4f)), new Bepu3DPhysicsOptions
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

        entity.AddBepuPhysics(Primitive3D.Cube(2f), new Bepu3DPhysicsOptions());

        Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(entity)));
        Assert.IsType<BodyComponent>(entity.Get<CollidableComponent>());
    }

    [Fact]
    public void CompoundThatHoldsAShapeIsKeptAsItIs()
    {
        var entity = EntityWithModel();
        var own = new BoxCollider { Mass = 2f };

        entity.AddBepuPhysics(Primitive3D.Cube(1.2f), new Bepu3DPhysicsOptions
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

        entity.AddBepuPhysics(Primitive3D.Sphere(0.5f), new Bepu3DPhysicsOptions { Mass = 4f });

        Assert.Equal(4f, Assert.Single(CollidersOf(entity)).Mass);
    }

    [Fact]
    public void TwoDimensionalHelperFollowsTheSameRule()
    {
        var fitted = new Entity();
        var kept = new Entity();
        var own = new BoxCollider { Mass = 3f };

        fitted.AddBepuPhysics(Primitive2D.Rectangle(4f, 0.5f), new Bepu2DPhysicsOptions
        {
            Component = new Body2DComponent { Collider = new CompoundCollider() },
        });

        kept.AddBepuPhysics(Primitive2D.Rectangle(4f, 0.5f), new Bepu2DPhysicsOptions
        {
            Component = new Body2DComponent { Collider = new CompoundCollider { Colliders = { own } } },
        });

        Assert.IsType<BoxCollider>(Assert.Single(CollidersOf(fitted)));
        Assert.Same(own, Assert.Single(CollidersOf(kept)));
    }

    /// <summary>The 3D helper asks for a model to be present; it reads nothing out of it.</summary>
    private static Entity EntityWithModel() => new() { new ModelComponent(new Model()) };

    private static IList<ColliderBase> CollidersOf(Entity entity)
        => Assert.IsType<CompoundCollider>(entity.Get<CollidableComponent>().Collider).Colliders;
}