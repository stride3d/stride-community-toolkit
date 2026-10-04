using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// A dynamic body confined to the XY plane.
/// </summary>
/// <remarks>
/// <para>
/// The body moves and collides in three dimensions like any other, but its position on Z and its
/// rotation about X and Y are managed for it. Writing to them has no lasting effect: out-of-plane
/// position and velocity are corrected before every solve, so a value assigned from game code is
/// gone by the next step.
/// </para>
/// <para>
/// Rotation about X and Y is locked, not reset. A body that is already tilted about one of those
/// axes when it attaches keeps that tilt, held at that angle for as long as it lives. Give bodies an
/// identity rotation, or one about Z only, unless a permanent tilt is what you want.
/// </para>
/// <para>
/// Energy is not conserved the way a purpose-built 2D solver would conserve it. Contact resolution
/// runs in three dimensions and can push a body along Z; that motion is discarded rather than
/// redirected into the plane, so a little kinetic energy is lost whenever it happens. It is small
/// enough not to show in ordinary scenes, but it is a real difference from a native 2D engine.
/// </para>
/// <para>
/// Bodies are still free to sleep, which is what keeps large 2D scenes cheap. Nothing here wakes a
/// resting body.
/// </para>
/// <para>
/// A type of the same name is proposed for <c>Stride.BepuPhysics</c>. Code importing both
/// <c>Stride.BepuPhysics</c> and <c>Stride.CommunityToolkit.Bepu</c> must qualify which one it means.
/// </para>
/// </remarks>
[ComponentCategory("Physics - Bepu 2D")]
public class Body2DComponent : BodyComponent, ISimulationUpdate
{
    /// <summary>
    /// Ceiling on the plane-restoring speed, in world units per second.
    /// </summary>
    /// <remarks>
    /// This mainly guards extreme cases (for example, a body spawned far from the plane) by preventing
    /// an overly aggressive snap-back velocity.
    /// </remarks>
    private const float MaximumCorrectionSpeed = 1f;

    /// <summary>Default drift allowance, one millimetre for a scene built at one unit per metre.</summary>
    private const float DefaultZTolerance = 0.001f;

    // What the out-of-plane inverse-inertia terms are scaled by, rather than being set to zero.
    //
    // Zeroing them is the obvious way to say "infinitely resistant to rotation about this axis", but
    // zeroing *some* terms and not others is a degenerate case the solver handles badly. In a dense
    // pile of hull-shaped bodies the solve diverges, bodies are flung far enough to make the
    // broad-phase bounds meaningless, the pair count runs away, and the narrow phase allocates until
    // the process dies - tens of gigabytes within seconds.
    //
    // Measured over three runs each at 20,000 triangular prisms: a full tensor never fails, a tensor
    // with every term zeroed never fails (the idiom Bepu's character demo uses), and one with X and Y
    // zeroed while Z stays responsive fails every time. Scaling keeps every term non-zero while
    // leaving the body four orders of magnitude harder to rotate out of plane than within it. Any
    // residue it leaves is cleared by the angular velocity correction in SimulationUpdate on the same
    // step, so it cannot accumulate.
    //
    // Do not simplify this back to zero.
    private const float OutOfPlaneInertiaScale = 1e-4f;

    // Tracks the kinematic state the rotation lock was applied for, so it can be restored when the
    // body switches back to dynamic and Bepu reinstates the full shape inertia
    private bool _lockedWhileKinematic;

    private float _zTolerance = DefaultZTolerance;

    /// <summary>
    /// Gets or sets how far the body may drift off the Z = 0 plane before it is pulled back, in world
    /// units. Defaults to 0.001, one millimetre for a scene built at one unit per metre.
    /// </summary>
    /// <remarks>
    /// Out-of-plane velocity is always cleared; this value only controls when positional correction
    /// starts.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not finite, or is not greater than zero.
    /// </exception>
    [DataMemberRange(0.0001, 4)]
    [Display("Z tolerance", category: CategoryActivity)]
    public float ZTolerance
    {
        get => _zTolerance;
        set
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Z tolerance must be a finite value greater than zero.");
            }

            _zTolerance = value;
        }
    }

    /// <summary>
    /// Initializes a new <see cref="Body2DComponent"/> with interpolation enabled, so rendering stays
    /// smooth when the display refreshes faster than the fixed physics step.
    /// </summary>
    public Body2DComponent() => InterpolationMode = InterpolationMode.Interpolated;

    /// <inheritdoc />
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(pose, shapeInertia, shapeIndex);

        // Bepu hands the body its shape inertia during the base call, so this is the first point at
        // which there is something to lock
        ApplyRotationLock();
    }

    /// <summary>
    /// Updates the simulation state of the 2D body.
    /// </summary>
    /// <param name="sim">The simulation stepping this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>
    /// Runs before the solver, so the corrections applied here are resolved together with every
    /// contact rather than overwriting the solver's result afterwards. Out-of-plane velocity is
    /// cleared, and once the body is further from the plane than <see cref="ZTolerance"/> a bounded
    /// velocity pulls it back. Sleeping bodies are left alone.
    /// </remarks>
    public virtual void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        // Deliberately ahead of the sleep check. Turning off Kinematic hands the body its full shape
        // inertia back, and if that happened while it slept it would be free to tumble during the
        // first solve after waking - the lock freezes rotation rather than correcting it, so any tilt
        // picked up in that one step would stay for good
        RestoreRotationLockIfKinematicChanged();

        if (!Awake) return;

        // Out-of-plane velocity is never wanted. Removing it even inside the tolerance band is what
        // stops slow drift accumulating until it crosses the threshold
        var zError = Position.Z;
        var targetVelocityZ = MathF.Abs(zError) > ZTolerance
            ? Math.Clamp(-zError, -MaximumCorrectionSpeed, MaximumCorrectionSpeed)
            : 0f;

        // The one place this copy has to differ from the engine's. There, the body's velocity is
        // reached through BodyComponent.BodyReference and written through a single ref, so the writes
        // are unconditional and cost one pair of array lookups between them. BodyReference is
        // internal, so outside the engine assembly the public properties are all there is: each one
        // is a full getter or setter, which makes it worth testing before writing
        var velocity = LinearVelocity;

        if (velocity.Z != targetVelocityZ)
        {
            velocity.Z = targetVelocityZ;
            LinearVelocity = velocity;
        }

        // The inertia lock makes X/Y rotation negligible rather than impossible, so this clears the
        // residue each step. It also catches velocity surviving from before the body attached, or
        // from a direct assignment
        var angularVelocity = AngularVelocity;

        if (angularVelocity.X != 0f || angularVelocity.Y != 0f)
        {
            angularVelocity.X = 0f;
            angularVelocity.Y = 0f;
            AngularVelocity = angularVelocity;
        }
    }

    /// <summary>
    /// Method called after the simulation has run on the 2D body.
    /// </summary>
    /// <param name="sim">The simulation that stepped this body.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>
    /// Does nothing. The whole correction has already happened before the solve, in
    /// <see cref="SimulationUpdate"/>.
    /// </remarks>
    public virtual void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep) { }

    /// <summary>
    /// Removes the body's ability to rotate about X and Y for all practical purposes, leaving Z free.
    /// </summary>
    private void ApplyRotationLock()
    {
        var inertia = BodyInertia;
        ref var inverseInertia = ref inertia.InverseInertiaTensor;

        // Scaled rather than zeroed - see OutOfPlaneInertiaScale, this is not an oversight
        inverseInertia.XX *= OutOfPlaneInertiaScale;
        inverseInertia.YY *= OutOfPlaneInertiaScale;
        inverseInertia.YX = 0f;
        inverseInertia.ZX = 0f;
        inverseInertia.ZY = 0f; // ZZ is left alone, so the body can still roll in the plane

        BodyInertia = inertia;

        _lockedWhileKinematic = Kinematic;
    }

    /// <summary>
    /// Reapplies the rotation lock after a switch between kinematic and dynamic.
    /// </summary>
    /// <remarks>
    /// Turning <see cref="BodyComponent.Kinematic"/> off restores the body's full shape inertia, which
    /// silently undoes the lock applied at attach time and would let the body tumble out of the plane.
    /// </remarks>
    private void RestoreRotationLockIfKinematicChanged()
    {
        if (_lockedWhileKinematic == Kinematic) return;

        ApplyRotationLock();
    }
}