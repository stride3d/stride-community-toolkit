using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Extension methods on <see cref="Entity"/> that add Bepu 2D and 3D physics with the collider that matches a
/// primitive shape, plus the collider derivation itself, <see cref="ColliderFor(Primitive3D)"/> and
/// <see cref="ColliderFor(Primitive2D, float)"/>, for callers who build their own body.
/// </summary>
public static class EntityExtensions
{
    extension(Entity entity)
    {
        /// <summary>
        /// Adds Bepu 2D physics to the entity, with the collider that matches <paramref name="shape"/>.
        /// </summary>
        /// <param name="shape">The flat shape the entity's model was built from; the collider is derived from it.</param>
        /// <param name="options">Optional physics configuration: the body component, the collider depth and the mass. If null, default options are used.</param>
        /// <returns>The entity with the Bepu 2D physics components added.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
        public Entity AddBepuPhysics(Primitive2D shape, Bepu2DPhysicsOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(shape);

            options ??= new();

            // A fresh component per call when none was supplied, so the same options instance can
            // be reused across entities
            // The toolkit's own Body2DComponent, not the engine's of the same name: a type in this
            // file's own namespace wins over one brought in by a using directive. When the engine's
            // version ships and this copy is deleted, this line resolves to it instead, unchanged
            var component = options.Component ?? new Body2DComponent { Collider = new CompoundCollider() };

            // The fitted shape goes into an empty compound only. A compound that already holds a
            // shape is the caller's own collider and is kept as it is: adding to it would give the
            // body two colliders, and the mass of both
            if (component.Collider is CompoundCollider { Colliders.Count: 0 } compoundCollider)
            {
                var collider = ColliderFor(shape, options.Depth);

                if (options.Mass is { } mass) collider.Mass = mass;

                compoundCollider.Colliders.Add(collider);
            }

            entity.Add(component);

            return entity;
        }

        /// <summary>
        /// Adds Bepu 3D physics to the entity, with the collider that matches <paramref name="shape"/>.
        /// </summary>
        /// <param name="shape">The shape the entity's model was built from; the collider is derived from it.</param>
        /// <param name="options">Optional physics configuration: the body component and whether to include a collider. If null, default options are used.</param>
        /// <returns>The entity with the Bepu 3D physics components added.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
        public Entity AddBepuPhysics(Primitive3D shape, Bepu3DPhysicsOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(shape);

            options ??= new();

            // A fresh component per call when none was supplied, so the same options instance can
            // be reused across entities
            var component = options.Component ?? new BodyComponent { Collider = new CompoundCollider() };

            // The fitted shape goes into an empty compound only. A compound that already holds a
            // shape is the caller's own collider and is kept as it is: adding to it would give the
            // body two colliders, and the mass of both
            if (component.Collider is CompoundCollider { Colliders.Count: 0 } compoundCollider)
            {
                var collider = ColliderFor(shape);

                if (options.Mass is { } mass) collider.Mass = mass;

                compoundCollider.Colliders.Add(collider);
            }

            entity.Add(component);

            return entity;
        }
    }

    /// <summary>
    /// The Bepu collider that matches a flat <paramref name="shape"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    /// <param name="shape">The 2D shape.</param>
    /// <param name="depth">The thickness of the collider along Z.</param>
    /// <returns>A collider the same size as the shape's model, to put into a <see cref="CompoundCollider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static ColliderBase ColliderFor(Primitive2D shape, float depth)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive2D.RectangleShape rectangle => new BoxCollider { Size = new Vector3(rectangle.Size.X, rectangle.Size.Y, depth) },
            // The RotationLocal needs to be initialized before the bounding shape is calculated.
            Primitive2D.CircleShape circle => new CylinderCollider
            {
                Radius = circle.Radius,
                Length = depth,
                RotationLocal = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90))
            },
            // A capsule's Length is the straight part between the two semicircles; the shape's Height is the whole thing
            Primitive2D.CapsuleShape capsule => new CapsuleCollider { Radius = capsule.Radius, Length = capsule.Height - 2 * capsule.Radius },
            Primitive2D.TriangleShape triangle => TriangularPrismCollider.Create(new Vector3(triangle.Size.X, triangle.Size.Y, depth)),
            Primitive2D.RegularPolygonShape polygon => PolygonCollider.Create(polygon, depth),
            Primitive2D.PolygonShape polygon => PolygonCollider.Create(polygon, depth),
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }

    /// <summary>
    /// The Bepu collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="shape">The 3D shape.</param>
    /// <returns>A collider the same size as the shape's model, to put into a <see cref="CompoundCollider"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static ColliderBase ColliderFor(Primitive3D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive3D.BoxShape box => new BoxCollider { Size = box.Size },
            Primitive3D.SphereShape sphere => new SphereCollider { Radius = sphere.Radius },
            Primitive3D.CapsuleShape capsule => new CapsuleCollider { Radius = capsule.Radius, Length = capsule.Length },
            Primitive3D.CylinderShape cylinder => new CylinderCollider { Radius = cylinder.Radius, Length = cylinder.Height },
            Primitive3D.ConeShape cone => ConeCollider.Create(cone),
            Primitive3D.TorusShape torus => TorusCollider.Create(torus),
            // A plane has no thickness; a flat box the size of the plane is what a static ground needs
            Primitive3D.PlaneShape plane => new BoxCollider { Size = new Vector3(plane.Width, 0, plane.Length) },
            Primitive3D.TeapotShape teapot => TeapotCollider.Create(teapot),
            Primitive3D.TriangularPrismShape prism => TriangularPrismCollider.Create(prism),
            Primitive3D.TextShape text => TextCollider.Create(text),
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }
}