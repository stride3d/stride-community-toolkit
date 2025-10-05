using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides extension methods for the <see cref="Entity"/> class to simplify adding Bepu 2D and 3D physics components.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Adds Bepu 2D physics components to the entity.
    /// </summary>
    public static Entity AddBepu2DPhysics(this Entity entity, Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        options ??= new();

        if (!options.IncludeCollider)
        {
            entity.Add(options.Component);

            return entity;
        }

        //if (type == Primitive2DModelType.Circle)
        //{
        //    var model = entity.Get<ModelComponent>()?.Model;

        //    if (model is null)
        //    {
        //        throw new InvalidOperationException("Entity must have a ModelComponent with a valid model to add Bepu physics.");
        //    }

        //    entity.Remove<ModelComponent>();

        //    var childEntity = new Entity("Child") { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        //    childEntity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));

        //    entity.AddChild(childEntity);
        //}

        var colliderShape = Get2DColliderShape(type, options.Size, options.Depth);

        if (colliderShape is null) return entity;

        var compoundCollider = options.Component.Collider as CompoundCollider;

        compoundCollider?.Colliders.Add(colliderShape);

        entity.Add(options.Component);

        return entity;
    }

    /// <summary>
    /// Adds Bepu 3D physics components to the entity.
    /// </summary>
    public static Entity AddBepuPhysics(this Entity entity, PrimitiveModelType type, Bepu3DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var model = entity.Get<ModelComponent>()?.Model;

        if (model is null)
        {
            throw new InvalidOperationException("Entity must have a ModelComponent with a valid model to add Bepu physics.");
        }

        options ??= new();

        if (!options.IncludeCollider)
        {
            entity.Add(options.Component);

            return entity;
        }

        var colliderShape = Get3DColliderShape(type, options.Size);

        if (colliderShape is null) return entity;

        var compoundCollider = options.Component.Collider as CompoundCollider;

        compoundCollider?.Colliders.Add(colliderShape);

        entity.Add(options.Component);

        return entity;
    }

    private static ColliderBase? Get2DColliderShape(Primitive2DModelType type, Vector2? size = null, float depth = 0)
    {
        return type switch
        {
            Primitive2DModelType.Triangle => TriangularPrismCollider.Create(size is null ? null : new(size.Value.X, size.Value.Y, depth)),
            Primitive2DModelType.Rectangle => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.X, depth) },
            Primitive2DModelType.Capsule => size is null ? new CapsuleCollider() : new() { Radius = size.Value.X / 2, Length = size.Value.Y - size.Value.X },
            Primitive2DModelType.Circle => CreateCircleCollider(depth, size),
            _ => throw new InvalidOperationException(),
        };

        // The RotationLocal needs to be initialized before the bounding shape is calculated.
        static CylinderCollider CreateCircleCollider(float depth, Vector2? size) => size is null ? new CylinderCollider()
        {
            RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
        } : new()
        {
            Radius = size.Value.X,
            Length = depth,
            RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
        };
    }

    private static ColliderBase? Get3DColliderShape(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Capsule => size is null ? new CapsuleCollider() { Radius = 0.35f } : new() { Radius = size.Value.X, Length = size.Value.Y },
            PrimitiveModelType.Cone => ConeCollider.Create(size),
            PrimitiveModelType.Cube => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
            PrimitiveModelType.Cylinder => size is null ? new CylinderCollider() : new()
            {
                Radius = size.Value.X,
                Length = size.Value.Z,
                //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            PrimitiveModelType.Plane => size is null ? new BoxCollider() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Y) },
            PrimitiveModelType.RectangularPrism => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
            PrimitiveModelType.Sphere => size is null ? new SphereCollider() : new() { Radius = size.Value.X },
            PrimitiveModelType.Teapot => TeapotCollider.Create(size?.X),
            PrimitiveModelType.TriangularPrism => TriangularPrismCollider.Create(size),
            PrimitiveModelType.Torus => TorusCollider.Create(majorRadius: size?.X, minorRadius: size?.Y),
            _ => throw new InvalidOperationException(),
        };
}