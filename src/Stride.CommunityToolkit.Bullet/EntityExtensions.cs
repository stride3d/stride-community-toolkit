using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Extension methods on <see cref="Entity"/> that add Bullet 2D and 3D physics with the collider that matches a
/// primitive shape, plus the collider derivation itself, <see cref="ColliderFor(Primitive3D)"/> and
/// <see cref="ColliderFor(Primitive2D, float)"/>, for callers who build their own component.
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

        // Bullet's 2D circle is a sphere; the flat model is turned to face the camera the same way
        if (shape is Primitive2D.CircleShape)
        {
            entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));
        }

        var component = ComponentFor(options);

        if (component is null) return entity;

        // A component that already holds a shape is the caller's own collider and is kept as it is
        if (component.ColliderShapes.Count == 0)
        {
            component.ColliderShapes.Add(ColliderFor(shape, shape.Depth));
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
            var colliderShape = ColliderFor(shape);

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

    /// <summary>
    /// The Bullet collider shape that matches a flat <paramref name="shape"/>, extruded <paramref name="depth"/> along Z.
    /// </summary>
    /// <param name="shape">The 2D shape.</param>
    /// <param name="depth">The thickness along Z of the triangle's and polygon's hull; the box, sphere and capsule shapes are Bullet's own 2D shapes and ignore it.</param>
    /// <returns>A collider shape the same size as the shape's model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static IInlineColliderShapeDesc ColliderFor(Primitive2D shape, float depth)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive2D.RectangleShape rectangle => new BoxColliderShapeDesc { Size = new Vector3(rectangle.Size.X, rectangle.Size.Y, 0), Is2D = true },
            Primitive2D.CircleShape circle => new SphereColliderShapeDesc { Radius = circle.Radius, Is2D = true },
            // A capsule's Length is the straight part between the two semicircles; the shape's Height is the whole thing
            Primitive2D.CapsuleShape capsule => new CapsuleColliderShapeDesc { Radius = capsule.Radius, Length = capsule.Height - 2 * capsule.Radius, Is2D = true },
            Primitive2D.TriangleShape triangle => HullOf(TriangularPrismProceduralModel.New(new Vector3(triangle.Size.X, triangle.Size.Y, depth))),
            Primitive2D.RegularPolygonShape polygon => HullOf(new PolygonProceduralModel { Radius = polygon.Radius, Sides = polygon.Sides }, depth),
            Primitive2D.PolygonShape polygon => HullOf(new PolygonProceduralModel { Vertices = polygon.Vertices }, depth),
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }

    /// <summary>
    /// The Bullet collider shape that matches <paramref name="shape"/>, or <see langword="null"/> for a torus or a
    /// teapot, which have none.
    /// </summary>
    /// <param name="shape">The 3D shape.</param>
    /// <returns>A collider shape the same size as the shape's model, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="shape"/> is null.</exception>
    public static IInlineColliderShapeDesc? ColliderFor(Primitive3D shape)
    {
        ArgumentNullException.ThrowIfNull(shape);

        return shape switch
        {
            Primitive3D.BoxShape box => new BoxColliderShapeDesc { Size = box.Size },
            Primitive3D.SphereShape sphere => new SphereColliderShapeDesc { Radius = sphere.Radius },
            Primitive3D.CapsuleShape capsule => new CapsuleColliderShapeDesc { Radius = capsule.Radius, Length = capsule.Length },
            Primitive3D.CylinderShape cylinder => new CylinderColliderShapeDesc { Radius = cylinder.Radius, Height = cylinder.Height },
            Primitive3D.ConeShape cone => new ConeColliderShapeDesc { Radius = cone.Radius, Height = cone.Height },
            // A plane has no thickness; a flat box the size of the plane is what a static ground needs
            Primitive3D.PlaneShape plane => new BoxColliderShapeDesc { Size = new Vector3(plane.Width, 0, plane.Length) },
            Primitive3D.TriangularPrismShape prism => HullOf(TriangularPrismProceduralModel.New(prism.Size)),
            // One hull per glyph, placed where the glyph sits; a hull fills an O, as Bepu's does
            Primitive3D.TextShape text => HullsOf(text),
            Primitive3D.TorusShape => null,
            Primitive3D.TeapotShape => null,
            _ => throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape))
        };
    }

    // A convex hull from a flat polygon model, extruded along Z: the polygon at +depth/2 and at -depth/2
    private static ColliderShapeAssetDesc HullOf(PolygonProceduralModel polygon, float depth)
    {
        var outline = polygon.Vertices.Length > 0 ? polygon.Vertices : PolygonProceduralModel.GenerateRegularPolygonVertices(polygon.Radius, polygon.Sides);
        var halfDepth = depth / 2f;
        var points = new List<Vector3>(outline.Length * 2);

        foreach (var vertex in outline)
        {
            points.Add(new Vector3(vertex.X, vertex.Y, halfDepth));
            points.Add(new Vector3(vertex.X, vertex.Y, -halfDepth));
        }

        // Bullet builds the hull from the points; the indices are only needed for the asset's shape
        var indices = new List<uint>(points.Count);

        for (var i = 0; i < points.Count; i++) indices.Add((uint)i);

        return HullOf(points, indices);
    }

    // A convex hull from a generated mesh
    private static ColliderShapeAssetDesc HullOf(GeometricMeshData<VertexPositionNormalTexture> meshData)
        => HullOf(meshData.Vertices.Select(w => w.Position).ToList(), meshData.Indices.Select(w => (uint)w).ToList());

    private static ColliderShapeAssetDesc HullOf(List<Vector3> points, List<uint> indices) => Describe([points], [indices]);

    // A convex hull per glyph of solid lettering, in one shape
    private static ColliderShapeAssetDesc HullsOf(Primitive3D.TextShape text)
    {
        var glyphs = TextMesh.CreateGlyphs(text.Font, text.Content, text.Options);

        if (glyphs.Count == 0)
            throw new ArgumentException("The text has no glyph with a shape, so there is nothing to collide with.", nameof(text));

        var hulls = new List<List<Vector3>>(glyphs.Count);
        var hullIndices = new List<List<uint>>(glyphs.Count);

        foreach (var glyph in glyphs)
        {
            hulls.Add(glyph.Mesh.Vertices.Select(v => v.Position + glyph.Position).ToList());
            hullIndices.Add(glyph.Mesh.Indices.Select(i => (uint)i).ToList());
        }

        return Describe(hulls, hullIndices);
    }

    // The hulls as one convex hull shape, scaled down a little so neighbouring bodies do not start in contact
    private static ColliderShapeAssetDesc Describe(List<List<Vector3>> hulls, List<List<uint>> indices)
    {
        var collider = new ConvexHullColliderShapeDesc
        {
            Scaling = new(0.9f),
            ConvexHulls = [hulls],
            ConvexHullsIndices = [indices]
        };

        return new ColliderShapeAssetDesc { Shape = new PhysicsColliderShape([collider]) };
    }
}