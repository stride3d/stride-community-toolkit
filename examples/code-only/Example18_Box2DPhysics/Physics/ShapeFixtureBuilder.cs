using Box2D.NET;
using Example18_Box2DPhysics.Helpers; // GameConfig
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Physics;

/// <summary>
/// Builds Box2D shapes (fixtures) from toolkit <see cref="Shape2DModel"/> definitions.
/// Extraction of shape-specific logic from the monolithic PhysicsHelper.
/// </summary>
public static class ShapeFixtureBuilder
{
    public static void AttachShape(Example.Common.Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        var def = shapeDef ?? CreateDefaultShapeDef();
        switch (shapeModel.Type)
        {
            case Primitive2DModelType.Square2D:
            case Primitive2DModelType.Rectangle2D:
                CreateBox(shapeModel, bodyId, def);
                break;
            case Primitive2DModelType.Circle2D:
                CreateCircle(shapeModel, bodyId, def);
                break;
            case Primitive2DModelType.Triangle2D:
                CreateTriangle(shapeModel, bodyId, def);
                break;
            case Primitive2DModelType.Capsule:
                CreateCapsule(shapeModel, bodyId, def);
                break;
            default:
                throw new ArgumentException($"Unsupported shape type: {shapeModel.Type}");
        }
    }

    public static B2ShapeDef CreateDefaultShapeDef()
    {
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = GameConfig.DefaultDensity;
        shapeDef.material.friction = GameConfig.DefaultFriction;
        shapeDef.material.restitution = GameConfig.DefaultRestitution;
        return shapeDef;
    }

    public static B2ShapeDef CreateCustomShapeDef(float density, float friction, float restitution, bool isSensor = false)
    {
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = density;
        shapeDef.material.friction = friction;
        shapeDef.material.restitution = restitution;
        shapeDef.isSensor = isSensor;
        return shapeDef;
    }

    private static void CreateBox(Example.Common.Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
    }

    private static void CreateCircle(Example.Common.Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);
        b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
    }

    private static void CreateTriangle(Example.Common.Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var meshData = TriangleProceduralModel.New(shapeModel.Size);
        var points = meshData.Vertices.Take(3).Select(v => new B2Vec2(v.Position.X, v.Position.Y)).ToArray();
        if (points.Length < 3) throw new InvalidOperationException("Triangle must have at least 3 vertices");
        var hull = b2ComputeHull(points, 3);
        var triangle = b2MakePolygon(ref hull, 0.0f);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref triangle);
    }

    private static void CreateCapsule(Example.Common.Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var halfHeight = shapeModel.Size.Y / 2;
        var radius = shapeModel.Size.X / 2;
        var capsuleHeight = halfHeight - radius;
        var capsule = new B2Capsule(new B2Vec2(0, -capsuleHeight), new B2Vec2(0, capsuleHeight), radius);
        b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
    }
}