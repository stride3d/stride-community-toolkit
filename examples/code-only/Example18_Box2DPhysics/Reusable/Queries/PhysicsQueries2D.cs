using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics.Reusable.Queries;

/// <summary>
/// Stateless helper methods for common Box2D query patterns. Intended for extraction to a reusable
/// toolkit library. These wrappers avoid any Stride engine types and operate directly on world IDs
/// and primitive math structs only.
/// </summary>
public static class PhysicsQueries2D
{
    /// <summary>
    /// Performs a closest-hit raycast.
    /// </summary>
    public static (bool hit, B2BodyId bodyId, B2ShapeId shapeId, Vector2 point, Vector2 normal, float fraction) RaycastClosest(
        B2WorldId worldId, Vector2 origin, Vector2 direction, float maxDistance)
    {
        var start = new Box2D.NET.B2Vec2(origin.X, origin.Y);
        var translation = new Box2D.NET.B2Vec2(direction.X * maxDistance, direction.Y * maxDistance);
        var result = b2World_CastRayClosest(worldId, start, translation, b2DefaultQueryFilter());
        if (!result.hit)
            return (false, default, default, default, default, 0f);
        var p = new Vector2(result.point.X, result.point.Y);
        var n = new Vector2(result.normal.X, result.normal.Y);
        var bodyId = b2Shape_GetBody(result.shapeId);
        return (true, bodyId, result.shapeId, p, n, result.fraction);
    }

    /// <summary>
    /// Overlaps a point by creating a small AABB around it.
    /// </summary>
    public static B2BodyId? OverlapPoint(B2WorldId worldId, Vector2 point, float querySize = 0.1f)
    {
        var lower = new Box2D.NET.B2Vec2(point.X - querySize, point.Y - querySize);
        var upper = new Box2D.NET.B2Vec2(point.X + querySize, point.Y + querySize);
        var box = new Box2D.NET.B2AABB { lowerBound = lower, upperBound = upper };
        B2BodyId? hit = null;
        b2World_OverlapAABB(worldId, box, b2DefaultQueryFilter(), (shapeId, userData) =>
        {
            var bodyId = b2Shape_GetBody(shapeId);
            if (b2Shape_TestPoint(shapeId, new Box2D.NET.B2Vec2(point.X, point.Y)))
            {
                hit = bodyId;
                return false;
            }
            return true;
        }, null);
        return hit;
    }

    // ---------------------------------------------
    // Additional generic queries (extracted logic)
    // ---------------------------------------------

    /// <summary>
    /// Represents a raw raycast hit without any engine-specific references.
    /// </summary>
    public readonly record struct QueryRaycastHit(
        B2BodyId BodyId,
        B2ShapeId ShapeId,
        Vector2 Point,
        Vector2 Normal,
        float Fraction);

    /// <summary>
    /// Performs a raycast returning all hits along the segment from origin in direction up to maxDistance.
    /// </summary>
    public static List<QueryRaycastHit> RaycastAll(B2WorldId worldId, Vector2 origin, Vector2 direction, float maxDistance)
    {
        var hits = new List<QueryRaycastHit>();
        var start = new Box2D.NET.B2Vec2(origin.X, origin.Y);
        var translation = new Box2D.NET.B2Vec2(direction.X * maxDistance, direction.Y * maxDistance);

        b2World_CastRay(worldId, start, translation, b2DefaultQueryFilter(), (shapeId, point, normal, fraction, userData) =>
        {
            var bodyId = b2Shape_GetBody(shapeId);
            hits.Add(new QueryRaycastHit(
                bodyId,
                shapeId,
                new Vector2(point.X, point.Y),
                new Vector2(normal.X, normal.Y),
                fraction));
            return 1.0f; // continue collecting
        }, null);

        return hits;
    }

    /// <summary>
    /// Overlaps an AABB region and returns all unique bodies inside.
    /// </summary>
    public static List<B2BodyId> OverlapAABB(B2WorldId worldId, Vector2 lowerBound, Vector2 upperBound)
    {
        var bodies = new List<B2BodyId>();
        var box = new Box2D.NET.B2AABB
        {
            lowerBound = new Box2D.NET.B2Vec2(lowerBound.X, lowerBound.Y),
            upperBound = new Box2D.NET.B2Vec2(upperBound.X, upperBound.Y)
        };

        b2World_OverlapAABB(worldId, box, b2DefaultQueryFilter(), (shapeId, userData) =>
        {
            var bodyId = b2Shape_GetBody(shapeId);
            if (!bodies.Contains(bodyId))
            {
                bodies.Add(bodyId);
            }
            return true;
        }, null);

        return bodies;
    }

    /// <summary>
    /// Overlaps a circle region and returns all unique bodies inside.
    /// </summary>
    public static List<B2BodyId> OverlapCircle(B2WorldId worldId, Vector2 center, float radius)
    {
        var bodies = new List<B2BodyId>();
        var circle = new B2Circle(new Box2D.NET.B2Vec2(center.X, center.Y), radius);
        var proxy = b2MakeProxy(circle.center, 1, circle.radius);

        b2World_OverlapShape(worldId, ref proxy, b2DefaultQueryFilter(), (shapeId, userData) =>
        {
            var bodyId = b2Shape_GetBody(shapeId);
            if (!bodies.Contains(bodyId))
            {
                bodies.Add(bodyId);
            }
            return true;
        }, null);

        return bodies;
    }
}
