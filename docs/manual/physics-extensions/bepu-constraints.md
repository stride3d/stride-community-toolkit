# Bepu: Why Isn't My Constraint Doing Anything?

Constraints fail quietly. A joint that never moves, a motor that does nothing, and a correctly built
joint all produce the same output: no error, no warning, no log line.

Every section below starts with what you actually observe. All of it was found by building
`Example15_Constraint_Motors` and measuring the resulting angular velocities, not by reading the API.

> [!NOTE]
> A companion to [Bepu: Who Owns the Transform?](bepu-transform-ownership.md), which covers the
> body-side surprises. Several of those apply here too - in particular, a velocity assigned before
> the body reaches the simulation is silently lost.

## "My joint is completely rigid and nothing moves"

The most common cause is that the two joined parts occupy the same space.

> **A constraint does not stop the bodies it joins from colliding.**

Pinning two boxes together does not let them overlap. They still push each other apart, and a joint
built so its parts intersect spends every frame fighting a collision it cannot resolve. The result
looks like a frozen scene rather than a physics error.

This is easy to do by accident with `BallSocketConstraintComponent`, because the joint forces the two
anchor points to coincide. Pin an arm's top to an anchor cube's **centre** and the arm is *required*
to end up inside the cube:

```csharp
// Jams: the arm is pulled inside the anchor box.
LocalOffsetA = Vector3.Zero,                  // anchor centre
LocalOffsetB = new Vector3(0, armLength / 2, 0),
```

**Instead**, put the pivot in clear air, outside both colliders:

```csharp
// Works: the joint sits below the anchor box, so nothing overlaps.
LocalOffsetA = new Vector3(0, -0.45f, 0),
LocalOffsetB = new Vector3(0, armLength / 2, 0),
```

The same applies to a hinge: a mixer blade modelled as a bar through a vertical post will jam. Start
the post just above the blade instead of running it through.

## "My motor does nothing at all"

Check first whether the motor is the right *kind*. Several are named after the joint they are usually
paired with rather than after what they drive.

`BallSocketMotorConstraintComponent` drives **linear** velocity at the socket point, not rotation.
Combining it with a stiff `BallSocketConstraintComponent` holding that same point is a tug of war the
rigid joint always wins, and the body never moves - the driven axis reads exactly zero, every frame.

For rotation, use an angular motor:

| Want | Use |
|---|---|
| Spin one body continuously | `OneBodyAngularMotorConstraintComponent` |
| Drive rotation between two bodies | `AngularMotorConstraintComponent` |
| Drive rotation about a single axis | `AngularAxisMotorConstraintComponent` |

## "My motor worked until I tuned it"

Setting `MotorDamping` or `MotorMaximumForce` stops a motor working - **even when set to the exact
value the component already defaults to**.

```csharp
// Spins.
new OneBodyAngularMotorConstraintComponent
{
    A = body,
    TargetVelocity = new Vector3(0, 4, 0),
};

// Does not spin, despite 0.02 being the default already.
new OneBodyAngularMotorConstraintComponent
{
    A = body,
    TargetVelocity = new Vector3(0, 4, 0),
    MotorDamping = 0.02f,
};
```

The setters write to the description and call `TryUpdateDescription()`, which does not reach the
solver while the constraint is still unattached, so the write is lost and the motor is left in a
state that produces no force.

**Pick a component whose defaults already suit the job** rather than tuning these two properties. The
defaults vary a lot and are worth knowing:

| Component | Default maximum force | Default damping |
|---|---|---|
| `OneBodyAngularMotorConstraintComponent` | 10,000,000 | 0.02 |
| `AngularMotorConstraintComponent` | 1,000 | 10 |
| `AngularAxisMotorConstraintComponent` | 1,000 | 10 |
| `BallSocketMotorConstraintComponent` | 1,000 | 10 |

The 1,000 default is not enough to swing a modest arm against gravity - it creeps at a fraction of
the requested speed instead of reaching it. A one-body motor with its far larger budget will drive
the same arm to its target immediately.

## "My motor drives the right axis but fights me on the others"

A motor target is a **whole vector**. Asking for `(0, speed, 0)` also asks for *zero* rotation about
X and Z, and the motor will spend its force budget holding those at zero.

A tilted arm driven about Y therefore sweeps a wide cone at first and then slowly collapses into a
vertical spin: staying out at an angle requires rotation about X and Z, and the motor keeps
cancelling it. Nothing is broken - the motor is doing exactly what it was told.

Use `AngularAxisMotorConstraintComponent` when only one axis should be constrained.

## "Turning the motor off does nothing"

Disabling a motor does not brake anything; it only stops pushing. A hinged flywheel carrying
momentum coasts for a very long time, which is visually indistinguishable from "the toggle is
broken".

`Enabled` does work - it reattaches the constraint - but confirm it by displaying the body's velocity
rather than by watching the object:

```csharp
DebugText.Print($"spin {MathF.Abs(body.AngularVelocity.Y):0.0} rad/s", position);
```

## "My constraint anchor won't accept a StaticComponent"

Constraints join **bodies**. Both ends need a `BodyComponent`, so an immovable anchor must be a
`BodyComponent` with `Kinematic = true`, not a `StaticComponent`.

## Summary

| Symptom | Cause | Fix |
|---|---|---|
| Joint completely rigid | Joined parts overlap; a constraint does not disable collision | Put the pivot in clear air, outside both colliders |
| Motor does nothing, driven axis reads zero | `BallSocketMotor` drives linear velocity, not rotation | Use an angular motor |
| Motor stopped after setting damping or force | Setters do not reach the solver before attachment | Leave both alone; pick a component whose defaults fit |
| Motor creeps far below target | Default `MotorMaximumForce` of 1,000 is too small | Use a one-body motor, or reduce the load |
| Driven axis fine, other axes fought | A motor target constrains all three axes | Use `AngularAxisMotorConstraintComponent` |
| Disabling the motor changes nothing | Disabling stops pushing but does not brake | Display the velocity to confirm |
| Anchor rejects `StaticComponent` | Constraints join bodies only | Kinematic `BodyComponent` |
