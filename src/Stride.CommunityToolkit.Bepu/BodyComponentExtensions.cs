using Stride.BepuPhysics;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Extension methods for <see cref="BodyComponent"/>.
/// </summary>
public static class BodyComponentExtensions
{
    /// <summary>
    /// Puts the body at a position and rotation with no motion left and awake: a reset to a start pose, a
    /// respawn, a "put it back" key.
    /// </summary>
    /// <param name="body">The body to move.</param>
    /// <param name="position">Where the body's origin goes, in world units.</param>
    /// <param name="rotation">The body's orientation; <see langword="null"/>, the default, is <see cref="Quaternion.Identity"/>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="body"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <see cref="BodyComponent.Teleport"/> alone moves the body and nothing else: it keeps its velocity, its
    /// spin and its sleep, so a body that was tumbling keeps tumbling at the new place, and one that was asleep
    /// stays where it was put without checking what is under it. This zeroes both velocities and wakes the body
    /// from a fresh sleep, so that the sleep candidacy a resting body built up before the move does not come
    /// with it: a body that was still awake but about to sleep would otherwise be put to sleep at the new place
    /// within a step or two, before gravity had moved it, because waking an awake body changes nothing and the
    /// sleeper judges candidacy on the velocity before integration.
    /// </remarks>
    public static void TeleportAtRest(this BodyComponent body, Vector3 position, Quaternion? rotation = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        body.Teleport(position, rotation ?? Quaternion.Identity);
        body.LinearVelocity = Vector3.Zero;
        body.AngularVelocity = Vector3.Zero;

        // Sleeping first makes the wake a real one, which resets the body's sleep counters; waking a body that is
        // already awake does nothing, and its counters would carry over
        body.Awake = false;
        body.Awake = true;
    }
}