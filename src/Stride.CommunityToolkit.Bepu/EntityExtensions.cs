using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides extension methods for the <see cref="Entity"/> class to simplify adding Bepu 2D and 3D physics components. These methods automatically configure appropriate collider shapes based on primitive types and allow customization through options.
/// </summary>
public static class EntityExtensions
{
    extension(Entity entity)
    {
        /// <summary>
        /// Adds Bepu 2D physics components to the entity with an appropriate collider shape based on the primitive type.
        /// </summary>
        /// <param name="type">The type of 2D primitive shape for the collider.</param>
        /// <param name="options">Optional physics configuration including the body component, size, custom polygon vertices, depth, and whether to include a collider. If null, default options are used.</param>
        /// <returns>The entity with the Bepu 2D physics components added.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an unsupported <see cref="Primitive2DModelType"/> is specified.</exception>
        public Entity AddBepu2DPhysics(Primitive2DModelType type, Bepu2DPhysicsOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(entity);

            options ??= new();

            if (!options.IncludeCollider)
            {
                entity.Add(options.Component);

                return entity;
            }

            var colliderShape = Get2DColliderShape(type, options.Size, options.Depth, options.Vertices);

            //if (colliderShape is null) return entity;

            var compoundCollider = options.Component.Collider as CompoundCollider;

            compoundCollider?.Colliders.Add(colliderShape);

            entity.Add(options.Component);

            return entity;
        }

        /// <summary>
        /// Adds Bepu 3D physics components to the entity with an appropriate collider shape based on the primitive type.
        /// </summary>
        /// <param name="type">The type of 3D primitive shape for the collider.</param>
        /// <param name="options">Optional physics configuration including the body component, size, and whether to include a collider. If null, default options are used.</param>
        /// <returns>The entity with the Bepu 3D physics components added.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the entity does not have a <see cref="ModelComponent"/> with a valid model, or when an unsupported <see cref="PrimitiveModelType"/> is specified.</exception>
        public Entity AddBepu3DPhysics(PrimitiveModelType type, Bepu3DPhysicsOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var model = entity.Get<ModelComponent>()?.Model;

            if (model is null)
                throw new InvalidOperationException("Entity must have a ModelComponent with a valid model to add Bepu physics.");

            options ??= new();

            if (!options.IncludeCollider)
            {
                // Should we add the CollidableComponent even if no collider is included?
                entity.Add(options.Component);

                return entity;
            }

            var colliderShape = Get3DColliderShape(type, options.Size);

            //if (colliderShape is null) return entity;

            var compoundCollider = options.Component.Collider as CompoundCollider;

            compoundCollider?.Colliders.Add(colliderShape);

            entity.Add(options.Component);

            return entity;
        }
    }

    /// <summary>
    /// Creates a 2D collider shape based on the specified primitive type.
    /// </summary>
    /// <param name="type">The type of 2D primitive shape.</param>
    /// <param name="size">Optional size for the collider. For most shapes, X represents width and Y represents height.</param>
    /// <param name="depth">The depth (Z-axis) of the 2D shape in 3D space.</param>
    /// <param name="vertices">Optional custom polygon vertices in the XY plane. Used only for <see cref="Primitive2DModelType.Polygon"/>.</param>
    /// <returns>A <see cref="ColliderBase"/> configured for the specified primitive type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <see cref="Primitive2DModelType"/> is specified.</exception>
    private static ColliderBase Get2DColliderShape(Primitive2DModelType type, Vector2? size = null, float depth = 0, Vector2[]? vertices = null)
    {
        return type switch
        {
            Primitive2DModelType.Triangle => TriangularPrismCollider.Create(size is null ? null : new(size.Value.X, size.Value.Y, depth)),
            Primitive2DModelType.Rectangle => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.Y, depth) },
            Primitive2DModelType.Square => size is null ? new BoxCollider() : new() { Size = new(size.Value.X, size.Value.X, depth) },
            Primitive2DModelType.Capsule => size is null ? new CapsuleCollider() : new() { Radius = size.Value.X, Length = size.Value.Y - 2 * size.Value.X },
            Primitive2DModelType.Circle => CreateCircleCollider(depth, size),
            Primitive2DModelType.Polygon => PolygonCollider.Create(vertices, size?.X, size.HasValue ? (int)size.Value.Y : null, depth),
            _ => throw new InvalidOperationException($"Unsupported Primitive2DModelType: {type}"),
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

    /// <summary>
    /// Creates a 3D collider shape based on the specified primitive type.
    /// </summary>
    /// <param name="type">The type of 3D primitive shape.</param>
    /// <param name="size">Optional size for the collider. Interpretation varies by shape type (e.g., radius, dimensions).</param>
    /// <returns>A <see cref="ColliderBase"/> configured for the specified primitive type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unsupported <see cref="PrimitiveModelType"/> is specified.</exception>
    private static ColliderBase Get3DColliderShape(PrimitiveModelType type, Vector3? size = null)
        => type switch
        {
            PrimitiveModelType.Capsule => size is null ? new CapsuleCollider() { Radius = 0.35f } : new() { Radius = size.Value.X, Length = size.Value.Y },
            PrimitiveModelType.Cone => ConeCollider.Create(size),
            PrimitiveModelType.Cube => size is null ? new BoxCollider() : new() { Size = (Vector3)size },
            PrimitiveModelType.Cylinder => size is null ? new CylinderCollider() : new()
            {
                Radius = size.Value.X,
                Length = size.Value.Z,
                //RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            PrimitiveModelType.Plane => size is null ? new BoxCollider() : new() { Size = new Vector3(size.Value.X, 0, size.Value.Z) },
            PrimitiveModelType.RectangularPrism => size is null ? new BoxCollider() : new() { Size = (Vector3)size },
            PrimitiveModelType.Sphere => size is null ? new SphereCollider() : new() { Radius = size.Value.X },
            PrimitiveModelType.Teapot => TeapotCollider.Create(size?.X),
            PrimitiveModelType.TriangularPrism => TriangularPrismCollider.Create(size),
            PrimitiveModelType.Torus => TorusCollider.Create(majorRadius: size?.X, minorRadius: size?.Y),
            _ => throw new InvalidOperationException(),
        };
}