using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.Core.Mathematics;

namespace E05_3D_Constraints_Rope;

/// <summary>
/// A built rope: its segments, the weight on the end, and the skip constraints so they can be
/// switched off at runtime.
/// </summary>
public sealed record Rope(
    IReadOnlyList<BodyComponent> Links,
    BodyComponent Weight,
    IReadOnlyList<DistanceLimitConstraintComponent> LinkConstraints,
    DistanceLimitConstraintComponent WeightConstraint,
    IReadOnlyList<DistanceLimitConstraintComponent> SkipConstraints,
    RopeSettings Settings)
{
    /// <summary>
    /// Moves every link constraint between anchoring at the segment centres and at their ends, and
    /// switches the skip constraints with it, so a rope can be flipped between the stable build and
    /// the naive one while it hangs.
    /// </summary>
    /// <remarks>
    /// The allowed distance has to move with the anchors. Pulling them in from the ends shortens the
    /// gap they measure by the lever arm at each end, so it is subtracted twice; leave the distance
    /// alone and the rope changes length instead of changing behaviour.
    /// </remarks>
    public void SetStabilised(bool stabilised)
    {
        var leverArm = stabilised ? 0 : Settings.LeverArm;
        var step = Settings.LinkRadius * 2 + Settings.LinkSpacing;

        foreach (var constraint in LinkConstraints)
        {
            constraint.LocalOffsetA = new Vector3(0, -leverArm, 0);
            constraint.LocalOffsetB = new Vector3(0, leverArm, 0);
            constraint.MaximumDistance = step - leverArm * 2;
            constraint.MinimumDistance = constraint.MaximumDistance * 0.1f;
        }

        // The weight hangs from the last segment on a constraint of its own, and it has to move with
        // the rest. Leaving it anchored at the segment end while every other joint is anchored at a
        // centre leaves one asymmetric joint at the bottom, which shows up as the last link sitting
        // visibly out of line while the rope swings.
        WeightConstraint.LocalOffsetA = new Vector3(0, -leverArm, 0);
        WeightConstraint.MaximumDistance = Settings.LinkSpacing + Settings.LinkRadius - leverArm;
        WeightConstraint.MinimumDistance = WeightConstraint.MaximumDistance * 0.1f;

        foreach (var constraint in SkipConstraints) constraint.Enabled = stabilised;
    }

    /// <summary>
    /// Distance from the fixed anchor down to the weight. A rope holding its shape keeps this
    /// roughly constant; one that is losing the fight visibly stretches.
    /// </summary>
    public float Length => Vector3.Distance(Links[0].Position, Weight.Position);
}