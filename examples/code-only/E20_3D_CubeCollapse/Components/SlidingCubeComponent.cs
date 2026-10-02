using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.Core.Mathematics;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace CubeCollapse.Components;

/// <summary>
/// A cube that can only travel straight down its own column.
/// </summary>
/// <remarks>
/// <para>
/// The cube still collides in three dimensions like any other body, but it cannot rotate and it
/// cannot leave the X/Z lane it spawned on. Removing cubes from underneath it therefore makes it
/// drop cleanly into the gap instead of tipping, sliding sideways and shoving neighbouring stacks
/// over.
/// </para>
/// <para>
/// This also keeps every cube in the column the game's grid records for it, so a falling cube
/// can only come to rest in the slot the grid has already given it.
/// </para>
/// </remarks>
[ComponentCategory("Cube Collapse")]
public class SlidingCubeComponent : BodyComponent, ISimulationUpdate
{
    /// <summary>
    /// Ceiling on the lane-restoring speed, in world units per second. Mainly guards extreme cases,
    /// such as a cube shoved a long way off its lane, from snapping back violently.
    /// </summary>
    private const float MaximumCorrectionSpeed = 4f;

    private float _laneX;
    private float _laneZ;

    // Tracks the kinematic state the rotation lock was applied for, so it can be restored when the
    // cube switches back to dynamic and Bepu reinstates the full shape inertia
    private bool _lockedWhileKinematic;

    /// <summary>
    /// Gets or sets whether the cube is held to its column. Set to <see langword="false"/> to let it
    /// move and rotate like an ordinary rigid body, which is useful for comparing the two while
    /// testing.
    /// </summary>
    public bool ConstrainToColumn { get; set; } = true;

    /// <summary>
    /// Initializes a new <see cref="SlidingCubeComponent"/> with interpolation enabled, so a cube
    /// dropping into a gap stays smooth when the display refreshes faster than the fixed physics
    /// step. This is presentation only and does not affect the simulation.
    /// </summary>
    // The property name shadows the enum type inside this class, so the enum needs qualifying.
    public SlidingCubeComponent() => InterpolationMode = Stride.BepuPhysics.Definitions.InterpolationMode.Interpolated;

    /// <inheritdoc />
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        // Bepu hands the body its shape inertia during the base call, and BodyReference only exists
        // afterwards, so this is the first point at which there is anything to lock. Assigning
        // BodyInertia before the component is added to an entity silently does nothing.
        base.AttachInner(pose, shapeInertia, shapeIndex);

        _laneX = pose.Position.X;
        _laneZ = pose.Position.Z;

        ApplyRotationLock();
    }

    /// <summary>
    /// Updates the simulation state of the cube.
    /// </summary>
    /// <param name="simulation">The simulation stepping this cube.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>
    /// Runs before the solver, so these corrections are resolved together with every contact rather
    /// than overwriting the solver's result afterwards. Sleeping cubes are left alone.
    /// </remarks>
    public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        if (!ConstrainToColumn) return;

        // Deliberately ahead of the sleep check. Turning off Kinematic hands the cube its full shape
        // inertia back, which quietly undoes the rotation lock
        if (_lockedWhileKinematic != Kinematic)
        {
            ApplyRotationLock();
        }

        if (!Awake) return;

        var position = Position;
        var velocity = LinearVelocity;

        // Zero while on lane, a bounded restoring velocity once knocked off it
        velocity.X = Math.Clamp(_laneX - position.X, -MaximumCorrectionSpeed, MaximumCorrectionSpeed);
        velocity.Z = Math.Clamp(_laneZ - position.Z, -MaximumCorrectionSpeed, MaximumCorrectionSpeed);

        LinearVelocity = velocity;

        // The inertia lock makes rotation impossible rather than merely difficult, but velocity can
        // still arrive from a direct assignment or from before the cube attached
        AngularVelocity = Vector3.Zero;
    }

    /// <summary>
    /// Method called after the simulation has run on the cube.
    /// </summary>
    /// <param name="simulation">The simulation that stepped this cube.</param>
    /// <param name="simTimeStep">The fixed time step, in seconds.</param>
    /// <remarks>
    /// Does nothing. The whole correction has already happened before the solve, in
    /// <see cref="SimulationUpdate"/>.
    /// </remarks>
    public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep) { }

    /// <summary>
    /// Removes the cube's ability to rotate on every axis.
    /// </summary>
    /// <remarks>
    /// Call this straight after flipping <see cref="BodyComponent.Kinematic"/> to skip the single
    /// step of freedom that would otherwise pass before <see cref="SimulationUpdate"/> notices.
    /// </remarks>
    public void ApplyRotationLock()
    {
        if (!ConstrainToColumn) return;

        // Zeroing the WHOLE tensor means infinite resistance to rotation on every axis. This is the
        // idiom Bepu's own character controller uses and it is safe; it is zeroing only *some* terms
        // that leaves the rank-deficient tensor the solver diverges on, which is why
        // Body2DComponent has to scale its out-of-plane terms rather than zero them.
        var inertia = BodyInertia;
        inertia.InverseInertiaTensor = default;
        BodyInertia = inertia;

        _lockedWhileKinematic = Kinematic;
    }
}