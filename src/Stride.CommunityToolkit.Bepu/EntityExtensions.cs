using Stride.BepuPhysics.Definitions.Colliders;
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

        if (type == PrimitiveModelType.TriangularPrism)
        {
            // This is needed when using ConvexHullCollider
            //var meshData = TriangularPrismProceduralModel.New(options.Size is null ? new(1, 1, options.Depth) : new(options.Size.Value.X, options.Size.Value.Y, options.Depth));

            //var points = meshData.Vertices.Select(w => w.Position).ToList();
            //var uintIndices = meshData.Indices.Select(w => (uint)w).ToList();
            //var collider = new ConvexHullColliderShapeDesc()
            //{
            //    Model = model, // seems doing nothing
            //    ConvexHulls = [],
            //    ConvexHullsIndices = []
            //};

            //collider.ConvexHulls.Add([points]);
            //collider.ConvexHullsIndices.Add([uintIndices]);

            //List<IAssetColliderShapeDesc> descriptions = [];

            //descriptions.Add(collider);

            //var collider2 = new ConvexHullCollider() { Hull = new PhysicsColliderShape(descriptions) };

            //var compoundCollier = options.Component.Collider as CompoundCollider;

            //compoundCollier.Colliders.Add(collider2);

            // Or you can use just his
            options.Component.Collider = new MeshCollider() { Model = model, Closed = true };

            entity.Add(options.Component);
        }
        else
        {
            var colliderShape = Get3DColliderShape(type, options.Size);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier?.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    /// <summary>
    /// Adds Bepu 2D physics components to the entity.
    /// </summary>
    public static Entity AddBepu2DPhysics(this Entity entity, Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        options ??= new();

        if (type == Primitive2DModelType.Circle)
        {
            var model = entity.Get<ModelComponent>()?.Model;

            if (model is null)
            {
                throw new InvalidOperationException("Entity must have a ModelComponent with a valid model to add Bepu physics.");
            }

            entity.Remove<ModelComponent>();

            var childEntity = new Entity("Child") { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

            childEntity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));

            entity.AddChild(childEntity);
        }

        if (type == Primitive2DModelType.Triangle)
        {
            var model = entity.Get<ModelComponent>()?.Model;

            if (model is null)
            {
                throw new InvalidOperationException("Entity must have a ModelComponent with a valid model to add Bepu physics.");
            }

            // This is needed when using ConvexHullCollider
            //var meshData = TriangularPrismProceduralModel.New(options.Size is null ? new(1, 1, options.Depth) : new(options.Size.Value.X, options.Size.Value.Y, options.Depth));

            //var points = meshData.Vertices.Select(w => w.Position).ToList();
            //var uintIndices = meshData.Indices.Select(w => (uint)w).ToList();
            //var collider = new ConvexHullColliderShapeDesc()
            //{
            //    Model = model, // seems doing nothing
            //    ConvexHulls = [],
            //    ConvexHullsIndices = []
            //};

            //collider.ConvexHulls.Add([points]);
            //collider.ConvexHullsIndices.Add([uintIndices]);

            //List<IAssetColliderShapeDesc> descriptions = [];

            //descriptions.Add(collider);

            //var collider2 = new ConvexHullCollider() { Hull = new PhysicsColliderShape(descriptions) };

            //var compoundCollier = options.Component.Collider as CompoundCollider;

            //compoundCollier.Colliders.Add(collider2);

            // Or you can use just his
            options.Component.Collider = new MeshCollider() { Model = model, Closed = true };

            entity.Add(options.Component);
        }
        else
        {
            var colliderShape = Get2DColliderShape(type, options.Size, options.Depth);

            if (colliderShape is null) return entity;

            var compoundCollier = options.Component.Collider as CompoundCollider;

            compoundCollier?.Colliders.Add(colliderShape);

            entity.Add(options.Component);
        }

        return entity;
    }

    private static ColliderBase? Get2DColliderShape(Primitive2DModelType type, Vector2? size = null, float depth = 0)
    {
        return type switch
        {
            Primitive2DModelType.Rectangle => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Circle => size is null ? new CylinderCollider() : new()
            {
                Radius = size.Value.X,
                Length = depth,
                RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            //Primitive2DModelType.Triangle => triangleCollider ?? new TriangleCollider(),
            _ => throw new InvalidOperationException(),
        };
    }

    private static ColliderBase? Get3DColliderShape(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Plane => size is null ? new BoxCollider() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Y) },
            PrimitiveModelType.Sphere => size is null ? new SphereCollider() : new() { Radius = size.Value.X },
            PrimitiveModelType.Cube => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
            PrimitiveModelType.RectangularPrism => size is null ? new BoxCollider() : new() { Size = size ?? Vector3.One },
            PrimitiveModelType.Cylinder => size is null ? new CylinderCollider() : new()
            {
                Radius = size.Value.X,
                Length = size.Value.Z,
                //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            PrimitiveModelType.Capsule => size is null ? new CapsuleCollider() { Radius = 0.35f } : new() { Radius = size.Value.X, Length = size.Value.Y },
            _ => throw new InvalidOperationException(),
        };
}