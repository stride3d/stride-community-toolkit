using Box2D.NET;
using Example18_Box2DPhysics.Helpers; // GameConfig
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Example18_Box2DPhysics.Physics;

/// <summary>
/// Utilities that build static world geometry like ground and containment walls.
/// </summary>
public static class WorldGeometryBuilder
{
    public static B2BodyId AddGround(B2WorldId worldId, Vector2? position = null, Vector2? size = null)
    {
        var groundPosition = position ?? new Vector2(0.0f, -10.0f);
        var groundSize = size ?? new Vector2(50.0f, 10.0f);
        var def = b2DefaultBodyDef();

        def.position = new B2Vec2(groundPosition.X, groundPosition.Y);
        def.name = GameConfig.GroundName;
        def.type = B2BodyType.b2_staticBody;

        var groundId = b2CreateBody(worldId, ref def);
        var groundBox = b2MakeBox(groundSize.X, groundSize.Y);
        var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();

        shapeDef.material.friction = 0.6f;

        b2CreatePolygonShape(groundId, ref shapeDef, ref groundBox);

        return groundId;
    }

    public static List<B2BodyId> AddWalls(B2WorldId worldId, float width = 40f, float height = 40f, float wallThickness = 1f)
    {
        var walls = new List<B2BodyId>();
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;
        var configs = new[]
        {
            new { Position = new Vector2(-halfWidth, 0), Size = new Vector2(wallThickness, height) },
            new { Position = new Vector2(halfWidth, 0), Size = new Vector2(wallThickness, height) },
            new { Position = new Vector2(0, halfHeight), Size = new Vector2(width, wallThickness) },
            new { Position = new Vector2(0, -halfHeight), Size = new Vector2(width, wallThickness) }
        };

        foreach (var c in configs)
        {
            var def = b2DefaultBodyDef();
            def.position = new B2Vec2(c.Position.X, c.Position.Y);
            def.type = B2BodyType.b2_staticBody;
            def.name = "Wall";
            var bodyId = b2CreateBody(worldId, ref def);
            var box = b2MakeBox(c.Size.X, c.Size.Y);
            var shapeDef = ShapeFixtureBuilder.CreateDefaultShapeDef();
            b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
            walls.Add(bodyId);
        }

        return walls;
    }
}
