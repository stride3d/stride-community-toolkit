using Stride.BepuPhysics;
using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides extension methods for <see cref="BepuSimulation"/> that cast a <see cref="RaySegment"/>,
/// so a segment produced by <c>CameraComponentExtensions.ScreenToWorldRaySegment</c> can be handed
/// to Bepu directly, with no separate direction or maximum distance to work out.
/// </summary>
/// <remarks>
/// Bepu's own ray casts take an origin, a normalised direction and a maximum distance. A
/// <see cref="RaySegment"/> carries the same information as two points: the origin is
/// <see cref="RaySegment.Start"/>, the direction points from <see cref="RaySegment.Start"/> to
/// <see cref="RaySegment.End"/>, and the maximum distance is <see cref="RaySegment.Length"/>.
/// A segment whose two points coincide has no direction and never hits anything.
/// </remarks>
public static class SimulationExtensions
{
    /// <summary>
    /// Finds the closest intersection between the <paramref name="raySegment"/> and the shapes in the simulation.
    /// </summary>
    /// <param name="simulation">The <see cref="BepuSimulation"/> to cast in.</param>
    /// <param name="raySegment">The segment to cast, from <see cref="RaySegment.Start"/> to <see cref="RaySegment.End"/>.</param>
    /// <param name="hit">The closest intersection when this method returns <see langword="true"/>; undefined otherwise.</param>
    /// <param name="collisionMask">Which layers can be hit. Defaults to <see cref="CollisionMask.Everything"/>.</param>
    /// <returns><see langword="true"/> when the segment intersects a shape, <see langword="false"/> otherwise (including for a zero-length segment).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="simulation"/> is <see langword="null"/>.</exception>
    public static bool RayCast(this BepuSimulation simulation, in RaySegment raySegment, out HitInfo hit, CollisionMask collisionMask = CollisionMask.Everything)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        if (!TryGetRay(raySegment, out var origin, out var direction, out var maxDistance))
        {
            hit = default;

            return false;
        }

        return simulation.RayCast(origin, direction, maxDistance, out hit, collisionMask);
    }

    /// <summary>
    /// Collects every intersection between the <paramref name="raySegment"/> and the shapes in the simulation.
    /// </summary>
    /// <param name="simulation">The <see cref="BepuSimulation"/> to cast in.</param>
    /// <param name="raySegment">The segment to cast, from <see cref="RaySegment.Start"/> to <see cref="RaySegment.End"/>.</param>
    /// <param name="hits">The collection hits are appended to. It is not cleared first.</param>
    /// <param name="collisionMask">Which layers can be hit. Defaults to <see cref="CollisionMask.Everything"/>.</param>
    /// <remarks>Hits are not sorted; there are no guarantees about the order they are returned in.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="simulation"/> or <paramref name="hits"/> is <see langword="null"/>.</exception>
    public static void RayCastPenetrating(this BepuSimulation simulation, in RaySegment raySegment, ICollection<HitInfo> hits, CollisionMask collisionMask = CollisionMask.Everything)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(hits);

        if (!TryGetRay(raySegment, out var origin, out var direction, out var maxDistance))
        {
            return;
        }

        simulation.RayCastPenetrating(origin, direction, maxDistance, hits, collisionMask);
    }

    /// <summary>
    /// Collects every intersection between the <paramref name="raySegment"/> and the shapes in the simulation into a new list.
    /// </summary>
    /// <param name="simulation">The <see cref="BepuSimulation"/> to cast in.</param>
    /// <param name="raySegment">The segment to cast, from <see cref="RaySegment.Start"/> to <see cref="RaySegment.End"/>.</param>
    /// <param name="collisionMask">Which layers can be hit. Defaults to <see cref="CollisionMask.Everything"/>.</param>
    /// <returns>A new list of hits, empty when nothing was hit. Hits are not sorted.</returns>
    /// <remarks>
    /// Allocates a list per call. For a cast that runs every frame, prefer
    /// <see cref="RayCastPenetrating(BepuSimulation, in RaySegment, ICollection{HitInfo}, CollisionMask)"/> with a reused collection.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="simulation"/> is <see langword="null"/>.</exception>
    public static List<HitInfo> RayCastPenetrating(this BepuSimulation simulation, in RaySegment raySegment, CollisionMask collisionMask = CollisionMask.Everything)
    {
        var hits = new List<HitInfo>();

        simulation.RayCastPenetrating(raySegment, hits, collisionMask);

        return hits;
    }

    /// <summary>
    /// Converts a segment into the origin, normalised direction and maximum distance Bepu expects.
    /// </summary>
    /// <returns><see langword="false"/> when the segment has no length, and therefore no direction.</returns>
    private static bool TryGetRay(in RaySegment raySegment, out Vector3 origin, out Vector3 direction, out float maxDistance)
    {
        origin = raySegment.Start;
        direction = raySegment.End - raySegment.Start;
        maxDistance = direction.Length();

        if (maxDistance <= MathUtil.ZeroTolerance)
        {
            direction = default;

            return false;
        }

        direction /= maxDistance;

        return true;
    }
}