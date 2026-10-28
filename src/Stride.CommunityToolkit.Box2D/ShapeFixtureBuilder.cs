using Box2D.NET;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Builds Box2D shapes (fixtures) matching the toolkit's 2D primitive shapes, so a body's collision
/// shape lines up with the rendered <see cref="Primitive2D"/> mesh of the same size.
/// </summary>
public static class ShapeFixtureBuilder
{
    /// <summary>
    /// Creates and attaches a Box2D fixture matching the given primitive shape and its dimensions.
    /// </summary>
    /// <param name="shape">The 2D shape, from the <see cref="Primitive2D"/> factories.</param>
    /// <param name="bodyId">The Box2D body to which the generated shape will be attached.</param>
    /// <param name="shapeDef">
    /// Optional shape definition (density, friction, restitution, sensor flag). If <c>null</c>,
    /// <see cref="CreateDefaultShapeDef"/> is used.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="shape"/> is not one of the known shapes.</exception>
    /// <example>
    /// <code>
    /// var bodyId = simulation.CreateStaticBody(entity, position);
    /// ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(0.5f), bodyId);
    /// </code>
    /// </example>
    public static void AttachShape(Primitive2D shape, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        ArgumentNullException.ThrowIfNull(shape);

        var def = shapeDef ?? CreateDefaultShapeDef();

        switch (shape)
        {
            case Primitive2D.RectangleShape rectangle:
                AttachBox(rectangle.Size, bodyId, def);
                break;
            case Primitive2D.CircleShape circle:
                AttachCircle(circle.Radius, bodyId, def);
                break;
            case Primitive2D.TriangleShape triangle:
                AttachTriangle(triangle.Size, bodyId, def);
                break;
            case Primitive2D.CapsuleShape capsule:
                AttachCapsule(capsule.Radius, capsule.Height, bodyId, def);
                break;
            case Primitive2D.RegularPolygonShape polygon:
                AttachPolygon(PolygonProceduralModel.GenerateRegularPolygonVertices(polygon.Radius, polygon.Sides), bodyId, def);
                break;
            case Primitive2D.PolygonShape polygon:
                AttachPolygon(polygon.Vertices, bodyId, def);
                break;
            default:
                throw new ArgumentException($"Unknown shape {shape.GetType().Name}.", nameof(shape));
        }
    }

    /// <summary>
    /// Creates and attaches a convex polygon fixture from custom vertices.
    /// </summary>
    /// <param name="vertices">The polygon corners in local space; their convex hull is used. Box2D supports at most 8 hull vertices.</param>
    /// <param name="bodyId">The Box2D body to which the polygon will be attached.</param>
    /// <param name="shapeDef">
    /// Optional shape definition (density, friction, restitution, sensor flag). If <c>null</c>,
    /// <see cref="CreateDefaultShapeDef"/> is used.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when fewer than 3 vertices are provided.</exception>
    public static void AttachPolygon(Vector2[] vertices, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Length < 3) throw new ArgumentException("A polygon needs at least 3 vertices.", nameof(vertices));

        var def = shapeDef ?? CreateDefaultShapeDef();
        var points = new B2Vec2[vertices.Length];

        for (var i = 0; i < vertices.Length; i++) points[i] = vertices[i].ToB2Vec2();

        var hull = b2ComputeHull(points, points.Length);
        var polygon = b2MakePolygon(in hull, 0f);

        b2CreatePolygonShape(bodyId, in def, in polygon);
    }

    /// <summary>
    /// Attaches a chain of segments to a body - terrain, a track, the walls of a room. Unlike a row
    /// of separate boxes or segments, a chain has no internal corners for a rolling or sliding body
    /// to catch on: Box2D smooths the joins between consecutive segments.
    /// </summary>
    /// <param name="points">
    /// The chain vertices in the body's local space, in order; every point is part of the chain.
    /// A chain is one-sided: it collides on the right of its direction of travel, so a floor to be
    /// stood on from above must be listed <em>right to left</em>, and a closed room must wind
    /// counter-clockwise.
    /// </param>
    /// <param name="bodyId">The body to attach to, usually static.</param>
    /// <param name="isLoop">Close the chain from the last point back to the first.</param>
    /// <param name="friction">Surface friction of every segment. Box2D's default when omitted.</param>
    /// <remarks>
    /// Box2D wants an open chain to carry one extra point at each end - ghost vertices that shape
    /// the smoothing but are not collided. They are added here by extending the first and last
    /// segments, so the points given are exactly the chain that exists.
    /// </remarks>
    /// <exception cref="ArgumentException">Fewer than two points, or fewer than four for a loop.</exception>
    public static B2ChainId AttachChain(Vector2[] points, B2BodyId bodyId, bool isLoop = false, float? friction = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (points.Length < (isLoop ? 4 : 2))
            throw new ArgumentException(isLoop ? "A looped chain needs at least four points." : "A chain needs at least two points.", nameof(points));

        var chainDef = b2DefaultChainDef();

        chainDef.isLoop = isLoop;

        if (isLoop)
        {
            chainDef.points = new B2Vec2[points.Length];

            for (var i = 0; i < points.Length; i++) chainDef.points[i] = points[i].ToB2Vec2();
        }
        else
        {
            var first = points[0] - (points[1] - points[0]);
            var last = points[^1] + (points[^1] - points[^2]);

            chainDef.points = new B2Vec2[points.Length + 2];
            chainDef.points[0] = first.ToB2Vec2();
            chainDef.points[^1] = last.ToB2Vec2();

            for (var i = 0; i < points.Length; i++) chainDef.points[i + 1] = points[i].ToB2Vec2();
        }

        chainDef.count = chainDef.points.Length;

        if (friction is { } value)
        {
            var material = b2DefaultSurfaceMaterial();

            material.friction = value;
            chainDef.materials = [material];
            chainDef.materialCount = 1;
        }

        return b2CreateChain(bodyId, in chainDef);
    }

    /// <summary>
    /// Creates a <see cref="B2ShapeDef"/> with Box2D's own defaults (density 1, friction 0.6,
    /// restitution 0). Use <see cref="CreateCustomShapeDef"/> to specify material properties.
    /// </summary>
    /// <returns>A shape definition with Box2D default material properties.</returns>
    public static B2ShapeDef CreateDefaultShapeDef() => b2DefaultShapeDef();

    /// <summary>
    /// Creates a custom <see cref="B2ShapeDef"/> with explicitly specified physics material properties.
    /// </summary>
    /// <param name="density">Mass density (kg/m^2). Higher values increase mass.</param>
    /// <param name="friction">Coefficient of friction (typical range 0-1).</param>
    /// <param name="restitution">Bounciness (0 = inelastic, 1 = perfectly elastic).</param>
    /// <param name="isSensor">If true, the shape detects contacts but produces no collision response.</param>
    /// <returns>A shape definition initialized with the provided parameters.</returns>
    /// <example>
    /// <code>
    /// var customDef = ShapeFixtureBuilder.CreateCustomShapeDef(2.0f, 0.6f, 0.1f, isSensor: true);
    /// ShapeFixtureBuilder.AttachShape(Primitive2D.Square(size), bodyId, customDef);
    /// </code>
    /// </example>
    public static B2ShapeDef CreateCustomShapeDef(float density, float friction, float restitution, bool isSensor = false)
    {
        var shapeDef = b2DefaultShapeDef();

        shapeDef.density = density;
        shapeDef.material.friction = friction;
        shapeDef.material.restitution = restitution;
        shapeDef.isSensor = isSensor;

        return shapeDef;
    }

    /// <summary>
    /// The shape definition with its shape in a collision group, so <see cref="Box2DSimulation.CollisionMatrix"/>
    /// decides what the shape collides with. The group is Box2D's <c>groupIndex</c> and must be positive: zero is
    /// no group, and a negative group is Box2D's own "never collide with each other".
    /// </summary>
    /// <param name="shapeDef">The definition to put in the group; the copy returned is the one to attach shapes with.</param>
    /// <param name="group">The group number, 1 or more.</param>
    /// <returns>The definition in the group, with Box2D's custom filtering enabled so the matrix is consulted.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="group"/> is not positive.</exception>
    public static B2ShapeDef InCollisionGroup(this B2ShapeDef shapeDef, int group)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(group);

        shapeDef.filter.groupIndex = group;
        shapeDef.enableCustomFiltering = true;

        return shapeDef;
    }

    private static void AttachBox(Vector2 size, B2BodyId bodyId, in B2ShapeDef shapeDef)
    {
        var box = b2MakeBox(size.X / 2, size.Y / 2);

        b2CreatePolygonShape(bodyId, in shapeDef, in box);
    }

    private static void AttachCircle(float radius, B2BodyId bodyId, in B2ShapeDef shapeDef)
    {
        var circle = new B2Circle(b2Vec2_zero, radius);

        b2CreateCircleShape(bodyId, in shapeDef, in circle);
    }

    // The procedural mesh's first three vertices are the corners, so the fixture matches the rendered triangle exactly
    private static void AttachTriangle(Vector2 size, B2BodyId bodyId, in B2ShapeDef shapeDef)
    {
        var vertices = TriangleProceduralModel.New(size).Vertices;
        Span<B2Vec2> points = stackalloc B2Vec2[3];

        for (var i = 0; i < points.Length; i++) points[i] = new B2Vec2(vertices[i].Position.X, vertices[i].Position.Y);

        var hull = b2ComputeHull(points, points.Length);
        var triangle = b2MakePolygon(in hull, 0f);

        b2CreatePolygonShape(bodyId, in shapeDef, in triangle);
    }

    // The shape's Height is the whole capsule, caps included; Box2D wants the half length of the straight part
    private static void AttachCapsule(float radius, float height, B2BodyId bodyId, in B2ShapeDef shapeDef)
    {
        var halfLength = height / 2 - radius;
        var capsule = new B2Capsule(new B2Vec2(0, -halfLength), new B2Vec2(0, halfLength), radius);

        b2CreateCapsuleShape(bodyId, in shapeDef, in capsule);
    }
}