using Box2D.NET;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Helper class for creating and managing Box2D physics shapes and bodies.
/// Provides methods for creating physics shapes from 2D models and managing the physics world.
/// </summary>
public static class PhysicsHelper
{
    /// <summary>
    /// Creates a Box2D physics shape from a 2D model and attaches it to a body
    /// </summary>
    /// <param name="shapeModel">The 2D shape model containing type and size information</param>
    /// <param name="bodyId">The Box2D body ID to attach the shape to</param>
    /// <param name="shapeDef">Optional shape definition, uses default if null</param>
    public static void CreateShapePhysics(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        var shapeDefinition = shapeDef ?? CreateDefaultShapeDef();

        switch (shapeModel.Type)
        {
            case Primitive2DModelType.Square2D:
            case Primitive2DModelType.Rectangle2D:
                CreateBoxShape(shapeModel, bodyId, shapeDefinition);
                break;

            case Primitive2DModelType.Circle2D:
                CreateCircleShape(shapeModel, bodyId, shapeDefinition);
                break;

            case Primitive2DModelType.Triangle2D:
                CreateTriangleShape(shapeModel, bodyId, shapeDefinition);
                break;

            case Primitive2DModelType.Capsule:
                CreateCapsuleShape(shapeModel, bodyId, shapeDefinition);
                break;

            default:
                throw new ArgumentException($"Unsupported shape type: {shapeModel.Type}");
        }
    }

    /// <summary>
    /// Creates a default shape definition with standard physics properties
    /// </summary>
    /// <returns>A configured B2ShapeDef with default values</returns>
    public static B2ShapeDef CreateDefaultShapeDef()
    {
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = GameConfig.DefaultDensity;
        shapeDef.material.friction = GameConfig.DefaultFriction;
        shapeDef.material.restitution = GameConfig.DefaultRestitution;
        return shapeDef;
    }

    /// <summary>
    /// Creates a customized shape definition with specific physics properties
    /// </summary>
    /// <param name="density">Material density</param>
    /// <param name="friction">Surface friction</param>
    /// <param name="restitution">Bounciness (0 = no bounce, 1 = perfect bounce)</param>
    /// <param name="isSensor">Whether the shape is a sensor (triggers only)</param>
    /// <returns>A configured B2ShapeDef</returns>
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
    /// Adds a ground plane to the physics world for objects to collide with
    /// </summary>
    /// <param name="worldId">The Box2D world ID</param>
    /// <param name="position">Ground position (default: bottom of screen)</param>
    /// <param name="size">Ground size (default: wide and thick)</param>
    public static B2BodyId AddGround(B2WorldId worldId, Vector2? position = null, Vector2? size = null)
    {
        var groundPosition = position ?? new Vector2(0.0f, -10.0f);
        var groundSize = size ?? new Vector2(50.0f, 10.0f);

        // Create ground body definition
        var groundBodyDef = b2DefaultBodyDef();
        groundBodyDef.position = new B2Vec2(groundPosition.X, groundPosition.Y);
        groundBodyDef.name = GameConfig.GroundName;
        groundBodyDef.type = B2BodyType.b2_staticBody;

        // Create the ground body
        var groundId = b2CreateBody(worldId, ref groundBodyDef);

        // Create ground shape
        var groundBox = b2MakeBox(groundSize.X, groundSize.Y);
        var groundShapeDef = CreateDefaultShapeDef();
        groundShapeDef.material.friction = 0.6f; // Higher friction for ground

        b2CreatePolygonShape(groundId, ref groundShapeDef, ref groundBox);

        return groundId;
    }

    /// <summary>
    /// Creates walls around the physics world to contain objects
    /// </summary>
    /// <param name="worldId">The Box2D world ID</param>
    /// <param name="width">Width of the containment area</param>
    /// <param name="height">Height of the containment area</param>
    /// <param name="wallThickness">Thickness of the walls</param>
    public static List<B2BodyId> AddWalls(B2WorldId worldId, float width = 40f, float height = 40f, float wallThickness = 1f)
    {
        var walls = new List<B2BodyId>();
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;

        // Wall positions: left, right, top, bottom
        var wallConfigurations = new[]
        {
            new { Position = new Vector2(-halfWidth, 0), Size = new Vector2(wallThickness, height) },
            new { Position = new Vector2(halfWidth, 0), Size = new Vector2(wallThickness, height) },
            new { Position = new Vector2(0, halfHeight), Size = new Vector2(width, wallThickness) },
            new { Position = new Vector2(0, -halfHeight), Size = new Vector2(width, wallThickness) }
        };

        foreach (var config in wallConfigurations)
        {
            var bodyDef = b2DefaultBodyDef();
            bodyDef.position = new B2Vec2(config.Position.X, config.Position.Y);
            bodyDef.type = B2BodyType.b2_staticBody;
            bodyDef.name = "Wall";

            var bodyId = b2CreateBody(worldId, ref bodyDef);
            var box = b2MakeBox(config.Size.X, config.Size.Y);
            var shapeDef = CreateDefaultShapeDef();

            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            walls.Add(bodyId);
        }

        return walls;
    }

    private static void CreateBoxShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
    }

    private static void CreateCircleShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);
        b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
    }

    private static void CreateTriangleShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        // Create triangle using mesh data from the procedural model
        var meshData = TriangleProceduralModel.New(shapeModel.Size);
        var points = meshData.Vertices
            .Take(3) // Ensure we only take 3 vertices for triangle
            .Select(v => new B2Vec2(v.Position.X, v.Position.Y))
            .ToArray();

        if (points.Length < 3)
        {
            throw new InvalidOperationException("Triangle must have at least 3 vertices");
        }

        // Create hull and polygon from the triangle vertices
        var hull = b2ComputeHull(points, 3);
        var triangle = b2MakePolygon(ref hull, 0.0f);

        b2CreatePolygonShape(bodyId, ref shapeDef, ref triangle);
    }

    private static void CreateCapsuleShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
    {
        var halfHeight = shapeModel.Size.Y / 2;
        var radius = shapeModel.Size.X / 2;
        var capsuleHeight = halfHeight - radius;

        var capsule = new B2Capsule(
            new B2Vec2(0, -capsuleHeight),
            new B2Vec2(0, capsuleHeight),
            radius);

        b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
    }

    /// <summary>
    /// Applies an impulse to a body at its center
    /// </summary>
    /// <param name="bodyId">The body to apply impulse to</param>
    /// <param name="impulse">The impulse vector</param>
    public static void ApplyImpulse(B2BodyId bodyId, Vector2 impulse)
    {
        var b2Impulse = new B2Vec2(impulse.X, impulse.Y);
        b2Body_ApplyLinearImpulseToCenter(bodyId, b2Impulse, true);
    }

    /// <summary>
    /// Applies an impulse to a body at a specific point
    /// </summary>
    /// <param name="bodyId">The body to apply impulse to</param>
    /// <param name="impulse">The impulse vector</param>
    /// <param name="point">The world point where impulse is applied</param>
    public static void ApplyImpulseAtPoint(B2BodyId bodyId, Vector2 impulse, Vector2 point)
    {
        var b2Impulse = new B2Vec2(impulse.X, impulse.Y);
        var b2Point = new B2Vec2(point.X, point.Y);
        b2Body_ApplyLinearImpulse(bodyId, b2Impulse, b2Point, true);
    }

    /// <summary>
    /// Sets the velocity of a body
    /// </summary>
    /// <param name="bodyId">The body to modify</param>
    /// <param name="velocity">The new velocity</param>
    public static void SetVelocity(B2BodyId bodyId, Vector2 velocity)
    {
        var b2Velocity = new B2Vec2(velocity.X, velocity.Y);
        b2Body_SetLinearVelocity(bodyId, b2Velocity);
    }

    /// <summary>
    /// Gets the current velocity of a body
    /// </summary>
    /// <param name="bodyId">The body to query</param>
    /// <returns>The current velocity</returns>
    public static Vector2 GetVelocity(B2BodyId bodyId)
    {
        var velocity = b2Body_GetLinearVelocity(bodyId);
        return new Vector2(velocity.X, velocity.Y);
    }
}