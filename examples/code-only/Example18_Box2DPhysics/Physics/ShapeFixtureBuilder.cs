using Box2D.NET;
using Example.Common;
using Example18_Box2DPhysics.Helpers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Physics;

/// <summary>
/// Builds Box2D shapes (fixtures) from toolkit <see cref="Shape2DModel"/> definitions.
/// </summary>
public static class ShapeFixtureBuilder
{
    /// <summary>
    /// Creates and attaches a Box2D fixture that matches the provided <paramref name="shapeModel"/> type and dimensions.
    /// </summary>
    /// <param name="shapeModel">The toolkit shape model describing size and primitive type.</param>
    /// <param name="bodyId">The Box2D body to which the generated shape will be attached.</param>
    /// <param name="shapeDef">
    /// Optional shape definition (density, friction, restitution, sensor flag). If <c>null</c>,
    /// <see cref="CreateDefaultShapeDef"/> is used.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="shapeModel"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown if the shape type is not supported.</exception>
    /// <example>
    /// <code>
    /// var bodyId = world.CreateStaticBody(position);
    /// ShapeFixtureBuilder.AttachShape(shapeModel, bodyId);
    /// </code>
    /// </example>
    public static void AttachShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        var finalShapeDef = shapeDef ?? CreateDefaultShapeDef();

        switch (shapeModel.Type)
        {
            case Primitive2DModelType.Square2D:
            case Primitive2DModelType.Rectangle2D:
                CreateBox(shapeModel, bodyId, finalShapeDef);
                break;
            case Primitive2DModelType.Circle2D:
                CreateCircle(shapeModel, bodyId, finalShapeDef);
                break;
            case Primitive2DModelType.Triangle2D:
                CreateTriangle(shapeModel, bodyId, finalShapeDef);
                break;
            case Primitive2DModelType.Capsule:
                CreateCapsule(shapeModel, bodyId, finalShapeDef);
                break;
            default:
                throw new ArgumentException($"Unsupported shape type: {shapeModel.Type}");
        }
    }

    /// <summary>
    /// Creates a default <see cref="B2ShapeDef"/> populated with project-wide physics configuration values
    /// from <see cref="GameConfig"/>.
    /// </summary>
    /// <returns>A shape definition with default density, friction, and restitution.</returns>
    /// <example>
    /// <code>
    /// var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
    /// ShapeFixtureBuilder.AttachShape(shapeModel, bodyId, shapeDef);
    /// </code>
    /// </example>
    public static B2ShapeDef CreateDefaultShapeDef()
    {
        var shapeDef = b2DefaultShapeDef();

        shapeDef.density = GameConfig.DefaultDensity;
        shapeDef.material.friction = GameConfig.DefaultFriction;
        shapeDef.material.restitution = GameConfig.DefaultRestitution;

        return shapeDef;
    }

    /// <summary>
    /// Creates a custom <see cref="B2ShapeDef"/> with explicitly specified physics material properties.
    /// </summary>
    /// <param name="density">Mass density (kg/m^2). Higher values increase mass.</param>
    /// <param name="friction">Coefficient of friction (typical range 0–1).</param>
    /// <param name="restitution">Bounciness (0 = inelastic, 1 = perfectly elastic).</param>
    /// <param name="isSensor">If true, the shape detects contacts but produces no collision response.</param>
    /// <returns>A shape definition initialized with the provided parameters.</returns>
    /// <example>
    /// <code>
    /// var customDef = ShapeFixtureBuilder.CreateCustomShapeDef(2.0f, 0.6f, 0.1f, isSensor: true);
    /// ShapeFixtureBuilder.AttachShape(sensorShapeModel, bodyId, customDef);
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

    private static void CreateBox(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);

        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
    }

    private static void CreateCircle(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);

        b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
    }

    private static void CreateTriangle(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var meshData = TriangleProceduralModel.New(shapeModel.Size);
        var points = meshData.Vertices
            .Take(3)
            .Select(v => new B2Vec2(v.Position.X, v.Position.Y)).ToArray();

        if (points.Length < 3) throw new InvalidOperationException("Triangle must have at least 3 vertices");

        var hull = b2ComputeHull(points, 3);
        var triangle = b2MakePolygon(ref hull, 0.0f);

        b2CreatePolygonShape(bodyId, ref shapeDef, ref triangle);
    }

    private static void CreateCapsule(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var halfHeight = shapeModel.Size.Y / 2;
        var radius = shapeModel.Size.X / 2;
        var capsuleHeight = halfHeight - radius;

        var capsule = new B2Capsule(new B2Vec2(0, -capsuleHeight), new B2Vec2(0, capsuleHeight), radius);

        b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
    }
}