using Box2D.NET;
using Stride.Core.Mathematics;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Stateless helper methods for common Box2D query patterns. These wrappers avoid any Stride engine
/// types and operate directly on world ids and primitive math structs only.
/// </summary>
public static class PhysicsQueries2D
{
    // One delegate each, shared by every call, with the result list or holder passed as Box2D's
    // context object: a lambda closing over a local would allocate a closure and a delegate per query
    private static readonly b2OverlapResultFcn CollectBody = static (shapeId, context) =>
    {
        var bodies = (List<B2BodyId>)context;
        var bodyId = b2Shape_GetBody(shapeId);

        if (!bodies.Contains(bodyId)) bodies.Add(bodyId);

        return true;
    };

    private static readonly b2CastResultFcn CollectHit = static (shapeId, point, normal, fraction, context) =>
    {
        ((List<QueryRaycastHit>)context).Add(new QueryRaycastHit(b2Shape_GetBody(shapeId), shapeId, point.ToVector2(), normal.ToVector2(), fraction));

        return 1f; // every hit is wanted, so the ray keeps its full length
    };

    private static readonly b2CastResultFcn KeepClosest = static (shapeId, point, normal, fraction, context) =>
    {
        ((ClosestHit)context).Hit = new ShapeCastHit(shapeId, b2Shape_GetBody(shapeId), point.ToVector2(), normal.ToVector2(), fraction);

        // Returning the fraction clips the sweep to this hit, so only a nearer one can replace it
        return fraction;
    };

    // One holder per thread, reused, so a shape cast allocates nothing: CharacterMover2D casts every step
    [ThreadStatic]
    private static ClosestHit? t_closest;

    private sealed class ClosestHit
    {
        internal ShapeCastHit? Hit;
    }

    /// <summary>
    /// Performs a closest-hit raycast against every shape in <paramref name="worldId"/> along a segment starting at <paramref name="origin"/> in <paramref name="direction"/> up to <paramref name="maxDistance"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="origin">World-space ray origin.</param>
    /// <param name="direction">Normalized direction vector.</param>
    /// <param name="maxDistance">Maximum ray length.</param>
    /// <returns>The closest hit, or <see langword="null"/> when the ray strikes nothing.</returns>
    public static QueryRaycastHit? RaycastClosest(B2WorldId worldId, Vector2 origin, Vector2 direction, float maxDistance)
    {
        var result = b2World_CastRayClosest(worldId, origin.ToB2Vec2(), (direction * maxDistance).ToB2Vec2(), b2DefaultQueryFilter());

        return result.hit ? new QueryRaycastHit(b2Shape_GetBody(result.shapeId), result.shapeId, result.point.ToVector2(), result.normal.ToVector2(), result.fraction) : null;
    }

    /// <summary>
    /// Tests whether any shape overlaps the given <paramref name="point"/> by constructing a tiny AABB around it.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="point">The point to test.</param>
    /// <param name="querySize">Half-extent of the temporary AABB used for the broad-phase query.</param>
    /// <returns>The first body id whose shape actually contains the point; null if none.</returns>
    public static B2BodyId? OverlapPoint(B2WorldId worldId, Vector2 point, float querySize = 0.1f)
    {
        var box = new B2AABB
        {
            lowerBound = new B2Vec2(point.X - querySize, point.Y - querySize),
            upperBound = new B2Vec2(point.X + querySize, point.Y + querySize)
        };
        var target = point.ToB2Vec2();

        B2BodyId? hit = null;

        // A pick, not a per-frame query, so the closure over the point and the result is fine here
        b2World_OverlapAABB(worldId, box, b2DefaultQueryFilter(), (shapeId, _) =>
        {
            if (!b2Shape_TestPoint(shapeId, target)) return true;

            hit = b2Shape_GetBody(shapeId);

            return false;
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

        b2World_CastRay(worldId, origin.ToB2Vec2(), (direction * maxDistance).ToB2Vec2(), b2DefaultQueryFilter(), CollectHit, hits);

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
        var box = new B2AABB { lowerBound = lowerBound.ToB2Vec2(), upperBound = upperBound.ToB2Vec2() };

        b2World_OverlapAABB(worldId, box, b2DefaultQueryFilter(), CollectBody, bodies);

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
        => OverlapCircle(worldId, center, radius, b2DefaultQueryFilter());

    /// <summary>
    /// Collects all unique bodies whose shapes overlap a circle centered at <paramref name="center"/> with <paramref name="radius"/>,
    /// restricted to shapes whose category bits pass <paramref name="filter"/>.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="center">World-space circle center.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="filter">Category and mask bits the overlapped shapes must satisfy.</param>
    /// <returns>List of body ids (no duplicates).</returns>
    public static List<B2BodyId> OverlapCircle(B2WorldId worldId, Vector2 center, float radius, B2QueryFilter filter)
    {
        var bodies = new List<B2BodyId>();
        var proxy = b2MakeProxy(center.ToB2Vec2(), 1, radius);

        b2World_OverlapShape(worldId, ref proxy, filter, CollectBody, bodies);

        return bodies;
    }

    /// <summary>
    /// Sweeps a circle (or a point, when <paramref name="radius"/> is zero) from <paramref name="center"/>
    /// along <paramref name="translation"/> and returns the closest thing it strikes.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="center">World-space centre of the circle at the start of the sweep.</param>
    /// <param name="radius">Circle radius; zero casts a point.</param>
    /// <param name="translation">The sweep, as a world-space displacement.</param>
    /// <param name="filter">Category and mask bits the struck shapes must satisfy.</param>
    /// <returns>The closest hit, or null when the sweep is clear.</returns>
    public static ShapeCastHit? CastCircleClosest(B2WorldId worldId, Vector2 center, float radius, Vector2 translation, B2QueryFilter filter)
    {
        var proxy = b2MakeProxy(center.ToB2Vec2(), 1, radius);

        return CastShapeClosest(worldId, ref proxy, translation, filter);
    }

    /// <summary>
    /// Sweeps a line segment from <paramref name="a"/> to <paramref name="b"/> along
    /// <paramref name="translation"/> and returns the closest thing it strikes.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="a">One end of the segment at the start of the sweep, in world space.</param>
    /// <param name="b">The other end.</param>
    /// <param name="translation">The sweep, as a world-space displacement.</param>
    /// <param name="filter">Category and mask bits the struck shapes must satisfy.</param>
    /// <returns>The closest hit, or null when the sweep is clear.</returns>
    public static ShapeCastHit? CastSegmentClosest(B2WorldId worldId, Vector2 a, Vector2 b, Vector2 translation, B2QueryFilter filter)
    {
        var proxy = b2MakeProxy(a.ToB2Vec2(), b.ToB2Vec2(), 2, 0f);

        return CastShapeClosest(worldId, ref proxy, translation, filter);
    }

    /// <summary>
    /// Sweeps any convex proxy (see <c>b2MakeProxy</c>) along <paramref name="translation"/> and returns
    /// the closest thing it strikes. The circle and segment overloads build the proxy for you.
    /// </summary>
    /// <param name="worldId">Target Box2D world.</param>
    /// <param name="proxy">The shape to sweep, already in world space.</param>
    /// <param name="translation">The sweep, as a world-space displacement.</param>
    /// <param name="filter">Category and mask bits the struck shapes must satisfy.</param>
    /// <returns>The closest hit, or null when the sweep is clear.</returns>
    public static ShapeCastHit? CastShapeClosest(B2WorldId worldId, ref B2ShapeProxy proxy, Vector2 translation, B2QueryFilter filter)
    {
        var closest = t_closest ??= new ClosestHit();

        closest.Hit = null;

        b2World_CastShape(worldId, ref proxy, translation.ToB2Vec2(), filter, KeepClosest, closest);

        return closest.Hit;
    }
}