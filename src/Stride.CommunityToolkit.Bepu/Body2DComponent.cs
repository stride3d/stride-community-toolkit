using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Engine;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Dynamic body constrained to a 2D plane (XY) while still using Bepu's 3D solver.
/// </summary>
/// <remarks>
/// This component locks angular motion to the Z axis by zeroing the X/Y inverse inertia terms once at attach time
/// and applies a small pre-solve velocity correction each frame to drive the body back onto the Z = 0 plane.
/// The correction avoids post-solve teleports which can inject energy and destabilize piles, especially with convex hulls.
/// </remarks>
[ComponentCategory("Physics - Bepu 2D")]
public class Body2DComponent : BodyComponent, ISimulationUpdate
{
    /// <summary>
    /// Creates a new <see cref="Body2DComponent"/>. Interpolation is enabled by default.
    /// </summary>
    public Body2DComponent()
    {
        InterpolationMode = BepuPhysics.Definitions.InterpolationMode.Interpolated;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Keeps the shape-derived inertia so roll (Z angular motion) works and zeros X/Y inverse inertia to restrict yaw/pitch.
    /// Also applies mild stability tweaks for convex hull colliders (damping and recovery velocity cap).
    /// </remarks>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        // Keep the shape-derived inertia so rotation (including around Z) works.
        base.AttachInner(pose, shapeInertia, shapeIndex);

        // Constrain rotation to Z by removing X/Y inverse inertia (hard lock) and clearing cross terms.
        var inertia = BodyInertia;
        var inverseInertia = inertia.InverseInertiaTensor;
        inverseInertia.XX = 0f;
        inverseInertia.YY = 0f;
        inverseInertia.YX = 0f;
        inverseInertia.ZX = 0f;
        inverseInertia.ZY = 0f; // leave ZZ for roll
        inertia.InverseInertiaTensor = inverseInertia;
        BodyInertia = inertia;

        // Hulls tend to create energetic corrections in dense piles; tame it slightly.
        if (HasConvexHull(Collider))
        {
            MaximumRecoveryVelocity = MathF.Min(MaximumRecoveryVelocity, 1.5f);
            SpringDampingRatio = MathF.Max(SpringDampingRatio, 1f);
            SpringFrequency = MathF.Min(SpringFrequency, 30f);
        }
    }

    /// <summary>
    /// Returns true if the collider hierarchy contains at least one <see cref="ConvexHullCollider"/>.
    /// </summary>
    private static bool HasConvexHull(ICollider? collider)
    {
        if (collider is null) return false;
        if (collider is CompoundCollider compound && compound.Colliders is { } list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is ConvexHullCollider) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Called before the physics tick to perform pre-solve corrections.
    /// </summary>
    /// <param name="sim">Active simulation.</param>
    /// <param name="simTimeStep">Fixed time step size in seconds.</param>
    /// <remarks>
    /// Applies a proportional velocity correction on the Z axis to drive the body back to the plane (Z = 0):
    /// <c>vZ = -Position.Z</c>. This avoids injecting energy while keeping the body constrained to 2D.
    /// </remarks>
    public virtual void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        // Keep this body active
        Awake = true;

        var current = LinearVelocity;
        var zError = Position.Z;
        current.Z = -zError; // proportional correction
        LinearVelocity = current;
    }

    /// <summary>
    /// Called after the physics tick. Intentionally left empty for this component.
    /// </summary>
    public virtual void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep) { }
}