using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Extension methods on <see cref="Entity"/> that add Bepu 2D and 3D physics with the collider that matches a
/// primitive shape. <see cref="Colliders.For(Primitive3D)"/> and <see cref="Colliders.For(Primitive2D, float)"/> are the
/// collider derivation itself, for callers who build their own body.
/// </summary>
public static class EntityExtensions
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Bepu");

    /// <summary>
    /// Adds Bepu 2D physics to the entity, with the collider that matches <paramref name="shape"/>, extruded
    /// by the shape's depth.
    /// </summary>
    /// <param name="entity">The entity the body is added to.</param>
    /// <param name="shape">The flat shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">The body's mode, component, mass and interpolation; <see langword="null"/> is a dynamic, interpolated body.</param>
    /// <returns>The entity, with the component added unless the mode is <see cref="BodyMode.None"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A component is supplied together with <see cref="BodyMode.None"/>.</exception>
    public static Entity AddBepuPhysics(this Entity entity, Primitive2D shape, BepuPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        // The toolkit's own Body2DComponent, not the engine's of the same name: a type in this
        // file's own namespace wins over one brought in by a using directive. When the engine's
        // version ships and this copy is deleted, this line resolves to it instead, unchanged
        var component = ComponentFor(options, static () => new Body2DComponent { Collider = new CompoundCollider() });

        if (component is null) return entity;

        Fit(component, options, () => Colliders.For(shape, shape.Depth));

        entity.Add(component);

        return entity;
    }

    /// <summary>
    /// Adds Bepu 3D physics to the entity, with the collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="entity">The entity the body is added to.</param>
    /// <param name="shape">The shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">The body's mode, component, mass and interpolation; <see langword="null"/> is a dynamic, interpolated body.</param>
    /// <returns>The entity, with the component added unless the mode is <see cref="BodyMode.None"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A component is supplied together with <see cref="BodyMode.None"/>.</exception>
    public static Entity AddBepuPhysics(this Entity entity, Primitive3D shape, BepuPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        var component = ComponentFor(options, static () => new BodyComponent { Collider = new CompoundCollider() });

        if (component is null) return entity;

        Fit(component, options, () => Colliders.For(shape));

        entity.Add(component);

        return entity;
    }

    /// <summary>
    /// The component the options ask for: the supplied one as it is, else one made from the mode with an empty
    /// compound, interpolated unless the options say otherwise; <see langword="null"/> for <see cref="BodyMode.None"/>.
    /// </summary>
    /// <remarks>
    /// A fresh component per call when none was supplied, so the same options instance can be reused across entities.
    /// </remarks>
    private static CollidableComponent? ComponentFor(BepuPhysicsOptions options, Func<BodyComponent> body)
    {
        if (options.Component is { } supplied)
        {
            if (options.Body == BodyMode.None)
                throw new InvalidOperationException("The options supply a component and ask for no physics at once; drop one of the two.");

            return supplied;
        }

        if (options.Body == BodyMode.None) return null;

        if (options.Body == BodyMode.Static) return new StaticComponent { Collider = new CompoundCollider() };

        var component = body();

        component.Kinematic = options.Body == BodyMode.Kinematic;
        component.InterpolationMode = options.Interpolation ? InterpolationMode.Interpolated : InterpolationMode.None;

        return component;
    }

    /// <summary>
    /// Puts the fitted shape into an empty compound, with the options' mass. A compound that already holds a shape
    /// is the caller's own collider and is kept as it is: adding to it would give the body two colliders, and the
    /// mass of both.
    /// </summary>
    private static void Fit(CollidableComponent component, BepuPhysicsOptions options, Func<ColliderBase> fitted)
    {
        if (component.Collider is not CompoundCollider { Colliders.Count: 0 } compound) return;

        var collider = fitted();

        if (options.Mass is { } mass)
        {
            if (component is StaticComponent)
            {
                Log.Debug($"Mass {mass} ignored: a static collider has none.");
            }
            else
            {
                // Bepu adds a hull's mass once per hull, so a mass given for the whole shape is shared out
                // among them: lettering is one hull per glyph
                collider.Mass = collider is ConvexHullCollider hulls ? mass / TextCollider.HullCount(hulls) : mass;
            }
        }

        compound.Colliders.Add(collider);
    }
}