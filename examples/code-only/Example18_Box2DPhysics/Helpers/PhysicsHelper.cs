using Box2D.NET;
using Example.Common;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Helpers;

public static class PhysicsHelper
{
    public static void CreateShapePhysics(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        var nonNullableShapeDef = shapeDef ?? CreateDefaultShapeDef();

        switch (shapeModel.Type)
        {
            case Primitive2DModelType.Square2D:
            case Primitive2DModelType.Rectangle2D:
                var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
                b2CreatePolygonShape(bodyId, ref nonNullableShapeDef, ref box);
                break;

            case Primitive2DModelType.Circle2D:
                var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);
                b2CreateCircleShape(bodyId, ref nonNullableShapeDef, ref circle);
                break;

            case Primitive2DModelType.Triangle2D:
                CreateTriangleShape(shapeModel, bodyId, nonNullableShapeDef);
                break;

            case Primitive2DModelType.Capsule:
                var capsule = new B2Capsule(
                    new(0, -shapeModel.Size.X / 2),
                    new(0, (shapeModel.Size.Y / 2) - shapeModel.Size.X / 2),
                    shapeModel.Size.X / 2);
                b2CreateCapsuleShape(bodyId, ref nonNullableShapeDef, ref capsule);
                break;
        }

        static void CreateTriangleShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
        {
            var meshData = TriangleProceduralModel.New(shapeModel.Size);
            var points2 = meshData.Vertices.Select(v => new B2Vec2(v.Position.X, v.Position.Y)).ToArray();

            //// Define the three vertices of your triangle
            //// For an equilateral triangle centered at origin with size as the width
            //float halfWidth = shapeModel.Size.X / 2;
            //float halfHeight = shapeModel.Size.Y / 2;

            //B2Vec2[] points = new B2Vec2[3] {
            //    new B2Vec2(0, halfHeight),            // Top vertex
            //    new B2Vec2(-halfWidth, -halfHeight),  // Bottom left
            //    new B2Vec2(halfWidth, -halfHeight)    // Bottom right
            //};

            // Create a hull from these points
            B2Hull hull = b2ComputeHull(points2, 3);

            // Create a polygon shape from the hull
            B2Polygon triangle = b2MakePolygon(ref hull, 0.0f);

            // Create the shape on the body
            b2CreatePolygonShape(bodyId, ref shapeDef, ref triangle);
        }
    }

    public static B2ShapeDef CreateDefaultShapeDef()
    {
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = 2.0f;
        shapeDef.material.friction = 0.3f;

        return shapeDef;
    }

    public static void AddGround(B2WorldId worldId)
    {
        // Define the ground body.
        var groundBodyDef = b2DefaultBodyDef();
        groundBodyDef.position = new B2Vec2(0.0f, -10.0f);
        groundBodyDef.name = "Ground";

        var groundId = b2CreateBody(worldId, ref groundBodyDef);

        B2Polygon groundBox = b2MakeBox(50.0f, 10);

        // Add the box shape to the ground body.
        B2ShapeDef groundShapeDef = b2DefaultShapeDef();
        b2CreatePolygonShape(groundId, ref groundShapeDef, ref groundBox);
    }
}