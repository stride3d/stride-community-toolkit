using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Stride.CommunityToolkit.Tests.Box2D;

/// <summary>
/// The two bodies the Box2D tests keep building, a unit dynamic box and a static box of any size in a
/// collision category, and the 60 Hz step they run the world at.
/// </summary>
internal static class Box2DFixtures
{
    /// <summary>One frame at 60 Hz.</summary>
    internal static readonly TimeSpan Frame = TimeSpan.FromSeconds(1 / 60.0);

    /// <summary>Runs the simulation for <paramref name="frames"/> frames of <see cref="Frame"/>.</summary>
    internal static void StepFor(Box2DSimulation simulation, int frames)
    {
        for (var i = 0; i < frames; i++) simulation.Update(Frame);
    }

    /// <summary>
    /// A unit box, dynamic, so it has mass and a joint has something to hold; in a collision group when
    /// <paramref name="group"/> is positive.
    /// </summary>
    internal static B2BodyId Box(Box2DSimulation simulation, Vector3 position, int group = 0)
    {
        var body = simulation.CreateDynamicBody(position.XY());
        var def = InGroup(b2DefaultShapeDef(), group);
        var box = b2MakeBox(0.5f, 0.5f);

        b2CreatePolygonShape(body, in def, in box);

        return body;
    }

    /// <summary>
    /// A static box at <paramref name="position"/>, with the half extents and category bits given; in a
    /// collision group when <paramref name="group"/> is positive.
    /// </summary>
    internal static B2BodyId StaticBox(Box2DSimulation simulation, Vector2 position, float halfWidth, float halfHeight, ulong categoryBits, int group = 0)
    {
        var body = simulation.CreateStaticBody(position);
        var def = InGroup(b2DefaultShapeDef(), group);
        def.filter.categoryBits = categoryBits;
        var box = b2MakeBox(halfWidth, halfHeight);

        b2CreatePolygonShape(body, in def, in box);

        return body;
    }

    private static B2ShapeDef InGroup(B2ShapeDef def, int group) => group > 0 ? def.InCollisionGroup(group) : def;
}