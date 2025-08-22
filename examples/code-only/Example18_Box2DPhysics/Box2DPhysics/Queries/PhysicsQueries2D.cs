using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics.Box2DPhysics.Queries;

/// <summary>
/// Stateless helper methods for common Box2D query patterns. Intended for extraction to a reusable
/// toolkit library. These wrappers avoid any Stride engine types and operate directly on world IDs
/// and primitive math structs only.
/// </summary>
public static partial class PhysicsQueries2D
{
    /// <summary>
    /// Performs a closest-hit raycast against every shape in <paramref name="worldId"/> along a segment starting at <paramref name="origin"/> in <paramref name="direction"/> up to <paramref name="maxDistance"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="origin">World-space ray origin.</param>
    /// <param name="direction">Normalized direction vector.</param>
    /// <param name="maxDistance">Maximum ray length.</param>
    /// <returns>
    /// Tuple: (hit flag, body id, shape id, point, normal, fraction). When hit is false, the remaining values are unspecified.
    /// </returns>
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
    /// Tests whether any shape overlaps the given <paramref name="point"/> by constructing a tiny AABB around it.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="point">The point to test.</param>
    /// <param name="querySize">Half-extent of the temporary AABB used for broad-phase query.</param>
    /// <returns>The first body id whose shape actually contains the point; null if none.</returns>
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

    /// <summary>
    /// Performs a raycast returning every hit encountered along the segment starting at <paramref name="origin"/> in <paramref name="direction"/> up to <paramref name="maxDistance"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="origin">World-space ray origin.</param>
    /// <param name="direction">Normalized direction vector.</param>
    /// <param name="maxDistance">Maximum ray length.</param>
    /// <returns>List of raw hit data (unsorted). Empty when nothing is hit.</returns>
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
    /// Collects all unique bodies whose shapes overlap the axis-aligned box defined by <paramref name="lowerBound"/> and <paramref name="upperBound"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="lowerBound">Lower corner of the AABB.</param>
    /// <param name="upperBound">Upper corner of the AABB.</param>
    /// <returns>List of body ids (no duplicates).</returns>
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
    /// Collects all unique bodies whose shapes overlap a circle centered at <paramref name="center"/> with <paramref name="radius"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="center">World-space circle center.</param>
    /// <param name="radius">Circle radius.</param>
    /// <returns>List of body ids (no duplicates).</returns>
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