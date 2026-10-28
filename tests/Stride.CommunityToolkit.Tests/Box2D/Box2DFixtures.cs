using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// The two bodies the Box2D tests keep building: a unit dynamic box, and a static box of any size in a
/// collision category.
/// </summary>
internal static class Box2DFixtures
{
    /// <summary>A unit box, dynamic, so it has mass and a joint has something to hold.</summary>
    internal static B2BodyId Box(Box2DSimulation simulation, Vector3 position)
    {
        var body = simulation.CreateDynamicBody(position);

        b2CreatePolygonShape(body, b2DefaultShapeDef(), b2MakeBox(0.5f, 0.5f));

        return body;
    }

    /// <summary>A static box at <paramref name="position"/>, with the half extents and category bits given.</summary>
    internal static B2BodyId StaticBox(Box2DSimulation simulation, Vector2 position, float halfWidth, float halfHeight, ulong categoryBits)
    {
        var body = simulation.CreateStaticBody(new Vector3(position, 0));
        var def = b2DefaultShapeDef();
        def.filter.categoryBits = categoryBits;
        var box = b2MakeBox(halfWidth, halfHeight);

        b2CreatePolygonShape(body, in def, in box);

        return body;
    }
}