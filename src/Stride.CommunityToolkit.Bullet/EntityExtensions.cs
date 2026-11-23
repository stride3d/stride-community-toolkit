using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Extension methods on <see cref="Entity"/> that add Bullet 2D and 3D physics with the collider that matches a
/// primitive shape. <see cref="Colliders.For(Primitive3D)"/> and <see cref="Colliders.For(Primitive2D, float)"/> are the
/// collider derivation itself, for callers who build their own component.
/// </summary>
public static class EntityExtensions
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Bullet");

    /// <summary>
    /// Adds Bullet 2D physics to the entity, with the collider that matches <paramref name="shape"/>, extruded by
    /// the shape's depth where the shape is a hull.
    /// </summary>
    /// <param name="entity">The entity to add physics components to.</param>
    /// <param name="shape">The flat shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">The body's mode, component and mass; <see langword="null"/> is a dynamic body.</param>
    /// <returns>The entity, with the component added unless the mode is <see cref="BodyMode.None"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A component is supplied together with <see cref="BodyMode.None"/>.</exception>
    public static Entity AddBulletPhysics(this Entity entity, Primitive2D shape, BulletPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        var component = ComponentFor(options);

        if (component is null) return entity;

        // A component that already holds a shape is the caller's own collider and is kept as it is
        if (component.ColliderShapes.Count == 0)
        {
            component.ColliderShapes.Add(Colliders.For(shape, shape.Depth));
        }

        ApplyMass(component, options.Mass);
        entity.Add(component);

        return entity;
    }

    /// <summary>
    /// Adds Bullet 3D physics to the entity, with the collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="entity">The entity to add physics components to.</param>
    /// <param name="shape">The shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">The body's mode, component and mass; <see langword="null"/> is a dynamic body.</param>
    /// <returns>The entity, with the component added unless the mode is <see cref="BodyMode.None"/> or the shape has no Bullet collider.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A component is supplied together with <see cref="BodyMode.None"/>.</exception>
    public static Entity AddBulletPhysics(this Entity entity, Primitive3D shape, BulletPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        var component = ComponentFor(options);

        if (component is null) return entity;

        // A component that already holds a shape is the caller's own collider and is kept as it is
        if (component.ColliderShapes.Count == 0)
        {
            var colliderShape = Colliders.For(shape);

            if (colliderShape is null) return entity;

            component.ColliderShapes.Add(colliderShape);
        }

        ApplyMass(component, options.Mass);
        entity.Add(component);

        return entity;
    }

    /// <summary>
    /// The component the options ask for: the supplied one as it is, else one made from the mode;
    /// <see langword="null"/> for <see cref="BodyMode.None"/>. A fresh component per call, so the same options
    /// instance can be reused across entities.
    /// </summary>
    private static PhysicsComponent? ComponentFor(BulletPhysicsOptions options)
    {
        if (options.PhysicsComponent is { } supplied)
        {
            if (options.Body == BodyMode.None)
                throw new InvalidOperationException("The options supply a component and ask for no physics at once; drop one of the two.");

            return supplied;
        }

        return options.Body switch
        {
            BodyMode.None => null,
            BodyMode.Static => new StaticColliderComponent(),
            BodyMode.Kinematic => new RigidbodyComponent { IsKinematic = true },
            _ => new RigidbodyComponent(),
        };
    }

    // Bullet keeps mass on the rigid body; a static or character component has none to set
    private static void ApplyMass(PhysicsComponent component, float? mass)
    {
        if (mass is not { } value) return;

        if (component is RigidbodyComponent rigidbody)
            rigidbody.Mass = value;
        else
            Log.Debug($"Mass {value} ignored: a {component.GetType().Name} has none.");
    }
}