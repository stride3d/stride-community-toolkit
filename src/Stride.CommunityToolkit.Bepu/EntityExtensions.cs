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
/// primitive shape, plus the collider derivation itself, <see cref="ColliderFor(Primitive3D)"/> and
/// <see cref="ColliderFor(Primitive2D, float)"/>, for callers who build their own body.
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

        Fit(component, options, () => ColliderFor(shape, shape.Depth));

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

        Fit(component, options, () => ColliderFor(shape));

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
            // A capsule's Length is the straight part between the two semicircles; the shape's Height is the whole thing. A
            // height under two radii would make it negative, which Bepu rejects, so it is clamped the way the mesh clamps it
            Primitive2D.CapsuleShape capsule => new CapsuleCollider { Radius = capsule.Radius, Length = MathF.Max(capsule.Height, capsule.Radius * 2.01f) - 2 * capsule.Radius },
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