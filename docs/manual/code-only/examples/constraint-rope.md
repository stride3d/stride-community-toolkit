---
generated: true
slug: constraint-rope
---

# Rope - building a stable chain of constraints

Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime. Keeping a
heavy weight on a light chain from thrashing itself apart is the real problem: the force holding
the weight travels link by link to the anchor, and the solver gets only so many passes per frame.
Two ropes carry the same weight: the naive one ties each segment to its neighbour at the segment
ends, the stable one anchors at the centres and adds skip constraints that let impulses take
shortcuts along the chain. Z switches the stabilisation off while it hangs. Follows Bepu's own
RopeStabilityDemo rather than the obvious ball-socket construction, which is the one that misbehaves.

The `Program.cs` file shows how to:

- Building a rope as a runtime chain of bodies and constraints
- Linking segments with DistanceLimitConstraintComponent rather than ball sockets
- Why a rope needs a minimum distance well below its maximum
- Why an unstable rope looks fine while slack and only misbehaves under load
- How the mass ratio between weight and links drives instability
- Removing angular feedback with a zero lever arm
- Letting impulses take shortcuts with skip constraints
- Moving a constraint's anchor points and allowed distance together at runtime
- Why a constraint shorter than the distance it spans distorts a rope before anything moves
- Deriving an impulse from mass so tuning one does not silently change the other
- Setting collider mass explicitly instead of using the generated collider
- Size for a sphere primitive is its radius, not its diameter
- Making the topmost segment kinematic to hold the chain up
- Applying an impulse from the update loop rather than a velocity at build time
- Picking up and throwing bodies with GrabberScript
- Using helpers: SceneSetup3D, UseSkybox

![Rope - building a stable chain of constraints](media/constraint-rope.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E05_3D_Constraints_Rope).

[!code-csharp[](../../../../examples/code-only/E05_3D_Constraints_Rope/Program.cs?start=1&end=205)]