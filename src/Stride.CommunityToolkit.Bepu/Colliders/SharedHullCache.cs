using System.Collections.Concurrent;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;

namespace Stride.CommunityToolkit.Bepu.Colliders;

/// <summary>
/// Keeps one <see cref="DecomposedHulls"/> per distinct shape and size, so many bodies of the same
/// shape share a single Bepu hull.
/// </summary>
/// <remarks>
/// <para>
/// Stride caches the built Bepu <c>ConvexHull</c> against the <see cref="DecomposedHulls"/> instance it
/// came from. A fresh instance per body therefore means a fresh hull per body: a thousand identical
/// prisms build, store and eventually free a thousand identical hulls.
/// </para>
/// <para>
/// That is expensive, and worse than expensive. The hulls hold unmanaged buffers taken from a static
/// pool that Stride returns them to from a finalizer, so the garbage collector can hand memory back
/// while the simulation is allocating from the same pool on its worker threads. Sharing one instance
/// keeps the entry alive for the process, which removes the churn and the finalizer entirely.
/// </para>
/// <para>
/// Entries are never evicted. One hull per distinct shape and size is a small, bounded cost, and
/// releasing them is what causes the problem in the first place.
/// </para>
/// </remarks>
public static class SharedHullCache
{
    private static readonly ConcurrentDictionary<(string Shape, float A, float B, float C), DecomposedHulls> _hulls = new();

    /// <summary>
    /// Returns the shared hull data for a shape and size, building it on first use.
    /// </summary>
    /// <param name="shape">Identifier for the shape family, distinguishing hulls of equal dimensions.</param>
    /// <param name="a">First dimension of the shape.</param>
    /// <param name="b">Second dimension, or zero when unused.</param>
    /// <param name="c">Third dimension, or zero when unused.</param>
    /// <param name="factory">Builds the hull data when the shape and size have not been seen before.</param>
    /// <returns>Hull data shared by every caller using the same shape and dimensions.</returns>
    public static DecomposedHulls GetOrAdd(string shape, float a, float b, float c, Func<DecomposedHulls> factory)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(factory);

        return _hulls.GetOrAdd((shape, a, b, c), static (_, build) => build(), factory);
    }

    /// <summary>
    /// Returns a new <see cref="ConvexHullCollider"/> backed by shared hull data.
    /// </summary>
    /// <param name="shape">Identifier for the shape family, distinguishing hulls of equal dimensions.</param>
    /// <param name="a">First dimension of the shape.</param>
    /// <param name="b">Second dimension, or zero when unused.</param>
    /// <param name="c">Third dimension, or zero when unused.</param>
    /// <param name="factory">Builds the hull data when the shape and size have not been seen before.</param>
    /// <returns>A collider that shares its hull with every other collider of the same shape and size.</returns>
    /// <remarks>
    /// The collider itself is new each time - a collidable owns its collider - but the hull behind it is
    /// shared, which is where the cost lives.
    /// </remarks>
    public static ConvexHullCollider CreateCollider(string shape, float a, float b, float c, Func<DecomposedHulls> factory)
        => new() { Hull = GetOrAdd(shape, a, b, c, factory) };
}
