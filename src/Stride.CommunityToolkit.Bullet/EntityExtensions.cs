using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Extension methods on <see cref="Entity"/> that add Bullet 2D and 3D physics with the collider that matches a
/// primitive shape, plus the collider derivation itself, <see cref="ColliderFor(Primitive3D)"/> and
/// <see cref="ColliderFor(Primitive2D, float)"/>, for callers who build their own component.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Adds Bullet 2D physics to the entity, with the collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="entity">The entity to add physics components to.</param>
    /// <param name="shape">The flat shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">Optional physics configuration: the physics component, the collider depth and the mass. If null, default options are used.</param>
    /// <returns>The entity with the Bullet 2D physics components added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    public static Entity AddBulletPhysics(this Entity entity, Primitive2D shape, Bullet2DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        // Bullet's 2D circle is a sphere; the flat model is turned to face the camera the same way
        if (shape is Primitive2D.CircleShape)
        {
            entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.UnitX, MathUtil.DegreesToRadians(90));
        }

        if (options.PhysicsComponent is null) return entity;

        // A component that already holds a shape is the caller's own collider and is kept as it is
        if (options.PhysicsComponent.ColliderShapes.Count == 0)
        {
            options.PhysicsComponent.ColliderShapes.Add(ColliderFor(shape, options.Depth));
        }

        ApplyMass(options.PhysicsComponent, options.Mass);

        entity.Add(options.PhysicsComponent);

        return entity;
    }

    /// <summary>
    /// Adds Bullet 3D physics to the entity, with the collider that matches <paramref name="shape"/>.
    /// </summary>
    /// <param name="entity">The entity to add physics components to.</param>
    /// <param name="shape">The shape the entity's model was built from; the collider is derived from it.</param>
    /// <param name="options">Optional physics configuration: the physics component and the mass. If null, default options are used.</param>
    /// <returns>The entity with the Bullet 3D physics components added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> or <paramref name="shape"/> is null.</exception>
    /// <remarks>
    /// A torus or a teapot has no Bullet collider shape and is added without a collider.
    /// </remarks>
    public static Entity AddBulletPhysics(this Entity entity, Primitive3D shape, Bullet3DPhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        if (options.PhysicsComponent is null) return entity;

        // A component that already holds a shape is the caller's own collider and is kept as it is
        if (options.PhysicsComponent.ColliderShapes.Count == 0)
        {
            var colliderShape = ColliderFor(shape);

            if (colliderShape is null) return entity;

            options.PhysicsComponent.ColliderShapes.Add(colliderShape);
        }

        ApplyMass(options.PhysicsComponent, options.Mass);

        entity.Add(options.PhysicsComponent);

        return entity;
    }

    // Bullet keeps mass on the rigid body; a static or character component has none to set
    private static void ApplyMass(PhysicsComponent component, float? mass)
    {
        if (mass is { } value && component is RigidbodyComponent rigidbody)
            rigidbody.Mass = value;
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
    private static ColliderShapeAssetDesc HullOf(Stride.Graphics.GeometricMeshData<Stride.Graphics.VertexPositionNormalTexture> meshData)
        => HullOf(meshData.Vertices.Select(w => w.Position).ToList(), meshData.Indices.Select(w => (uint)w).ToList());

    private static ColliderShapeAssetDesc HullOf(List<Vector3> points, List<uint> indices)
    {
        var collider = new ConvexHullColliderShapeDesc
        {
            Scaling = new(0.9f),
            ConvexHulls = [],
            ConvexHullsIndices = []
        };

        collider.ConvexHulls.Add([points]);
        collider.ConvexHullsIndices.Add([indices]);

        return Describe(collider);
    }

    // A convex hull per glyph of solid lettering, in one shape
    private static ColliderShapeAssetDesc HullsOf(Primitive3D.TextShape text)
    {
        var glyphs = TextMesh.CreateGlyphs(text.Font, text.Content, text.Options);

        if (glyphs.Count == 0)
            throw new ArgumentException("The text has no glyph with a shape, so there is nothing to collide with.", nameof(text));

        var collider = new ConvexHullColliderShapeDesc
        {
            Scaling = new(0.9f),
            ConvexHulls = [],
            ConvexHullsIndices = []
        };

        var hulls = new List<List<Vector3>>(glyphs.Count);
        var hullIndices = new List<List<uint>>(glyphs.Count);

        foreach (var glyph in glyphs)
        {
            hulls.Add(glyph.Mesh.Vertices.Select(v => v.Position + glyph.Position).ToList());
            hullIndices.Add(glyph.Mesh.Indices.Select(i => (uint)i).ToList());
        }

        collider.ConvexHulls.Add(hulls);
        collider.ConvexHullsIndices.Add(hullIndices);

        return Describe(collider);
    }

    private static ColliderShapeAssetDesc Describe(ConvexHullColliderShapeDesc collider)
    {

        List<IAssetColliderShapeDesc> descriptions = [collider];

        return new ColliderShapeAssetDesc
        {
            Shape = new PhysicsColliderShape(descriptions)
        };
    }
}