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

    /// <summary>
    /// What a locked axis's inverse inertia is scaled by: small enough that the axis does not turn, not zero, which
    /// would leave a rank-deficient tensor that corrupts contact generation for a convex hull.
    /// </summary>
    private const float LockedInertiaScale = 1e-4f;

    /// <summary>
    /// Stops the body turning about the chosen axes of its own frame for all practical purposes: a crate that may
    /// slide but not tumble, a figure that must stay upright, a 2D-in-3D body that turns about Z alone.
    /// </summary>
    /// <param name="body">The body, already in a simulation.</param>
    /// <param name="lockX">Whether to lock rotation about the body's X axis.</param>
    /// <param name="lockY">Whether to lock rotation about the body's Y axis.</param>
    /// <param name="lockZ">Whether to lock rotation about the body's Z axis.</param>
    /// <remarks>
    /// <para>
    /// The inverse inertia about each locked axis is scaled down by ten thousand, the way <see cref="Body2DComponent"/>
    /// keeps its bodies in the plane, and the coupling between the axes is cleared so the free ones stay free. It is
    /// not zeroed: a tensor with a zero row is rank-deficient, and a convex hull with such a tensor corrupts Bepu's
    /// contact generation.
    /// </para>
    /// <para>
    /// The inertia belongs to the attached body, so call this once the body is in a simulation, from the frame after
    /// its entity joined the scene, and again after a change of <see cref="BodyComponent.Kinematic"/>, which gives the
    /// body its full shape inertia back. The axes are the body's own: a body that is turned when it is locked keeps that
    /// turn.
    /// </para>
    /// <para>
    /// The lock is on torque, which is what collisions and impulses apply. An angular velocity written to
    /// <see cref="BodyComponent.AngularVelocity"/> is kept whatever the inertia; a body that must stop turning as well
    /// as stay still has that velocity zeroed by the caller.
    /// </para>
    /// </remarks>
    public static void LockRotation(this BodyComponent body, bool lockX, bool lockY, bool lockZ)
    {
        ArgumentNullException.ThrowIfNull(body);

        var inertia = body.BodyInertia;
        ref var inverse = ref inertia.InverseInertiaTensor;

        if (lockX) inverse.XX *= LockedInertiaScale;
        if (lockY) inverse.YY *= LockedInertiaScale;
        if (lockZ) inverse.ZZ *= LockedInertiaScale;

        inverse.YX = 0f;
        inverse.ZX = 0f;
        inverse.ZY = 0f;

        body.BodyInertia = inertia;
    }
}