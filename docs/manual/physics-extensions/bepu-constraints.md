# Bepu: Why Isn't My Constraint Doing Anything?

Constraints fail quietly. A joint that never moves, a motor that does nothing, and a correctly built
joint all produce the same output: no error, no warning, no log line.

Every section below starts with what you observe. The values were measured in `E05_3D_Constraints_Motors`.

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

## "My motor stopped the moment I set MotorDamping"

`MotorDamping` does **not** read back the number in the component's constructor. The constructors
pass Bepu's `MotorSettings` a *softness*, and `MotorSettings` stores its reciprocal as the damping, so
a component built with `0.02` reports a `MotorDamping` of `50`.

Copying the value out of the constructor and "setting it to the default" therefore makes the motor
2500x softer, and a motor that soft produces no visible force at all:

```csharp
// Spins: leaves the real default of 50 in place.
new OneBodyAngularMotorConstraintComponent
{
    A = body,
    TargetVelocity = new Vector3(0, 4, 0),
};

// Does not spin: 0.02 is the CONSTRUCTOR argument, not the property value.
new OneBodyAngularMotorConstraintComponent
{
    A = body,
    TargetVelocity = new Vector3(0, 4, 0),
    MotorDamping = 0.02f,
};
```

**Read the property to find the real default** before overriding it. These are the measured values,
not the constructor arguments:

| Component | Maximum force | `MotorDamping` reads |
|---|---|---|
| `OneBodyAngularMotorConstraintComponent` | 10,000,000 | 50 |
| `AngularMotorConstraintComponent` | 1,000 | 0.1 |
| `AngularAxisMotorConstraintComponent` | 1,000 | 0.1 |
| `BallSocketMotorConstraintComponent` | 1,000 | 0.1 |

The two-body motors ship far weaker than the one-body one. A force budget of 1,000 with damping 0.1
is not enough to swing a modest arm against gravity - it creeps at a fraction of the requested speed.
Raising both to the one-body figures (10,000,000 and 50) drives the same arm to its target
immediately. The setters themselves work correctly; only the default is misleading.

## "My motor drives the right axis but fights me on the others"

Most motor targets are a **whole vector**. Asking for `(0, speed, 0)` also asks for *zero* rotation
about X and Z, and the motor will spend its force budget holding those at zero.

A tilted arm driven about Y therefore sweeps a wide cone at first and then collapses into a vertical
spin within seconds: staying out at an angle requires rotation about X and Z, and the motor keeps
cancelling it. Nothing is broken - the motor is doing exactly what it was told.

**Use `AngularAxisMotorConstraintComponent`** when only one axis should be driven. It takes a single
`LocalAxisA` and a scalar `TargetVelocity`, leaving the other two axes to gravity, so the cone
survives.

Two effects remain. Near the arm's own pendulum frequency, `sqrt(3g/2L)` for a rod pivoting at its
end, the drive and the swing beat against each other and the orbit wanders into shifting polygons;
pick a speed well clear of it. And the swing in and out is free motion that nothing damps, so the
cone breathes about its equilibrium angle indefinitely - the pendulum nutating, not the motor
faltering.

```csharp
new AngularAxisMotorConstraintComponent
{
    A = anchorBody,
    B = armBody,
    LocalAxisA = Vector3.UnitY,
    TargetVelocity = 2.5f,
    MotorDamping = 50,             // the weak two-body default will only creep
    MotorMaximumForce = 10_000_000,
};
```

## "Turning the motor off does nothing"

Disabling a motor does not brake anything; it only stops pushing. A hinged flywheel carrying
momentum coasts for a long time, which looks exactly like a broken toggle.

Setting `Enabled` to `false` does take the constraint out of the solver. Confirm it by displaying the
body's velocity rather than by watching the object:

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
| Motor stopped after setting `MotorDamping` | The property is the reciprocal of the constructor argument | Read the property for the real default; do not copy the constructor value |
| Motor creeps far below target | Two-body defaults of force 1,000 / damping 0.1 are very soft | Raise both, or use a one-body motor |
| Driven axis fine, other axes fought | A whole-vector motor target constrains all three axes | Use `AngularAxisMotorConstraintComponent` |
| Disabling the motor changes nothing | Disabling stops pushing but does not brake | Display the velocity to confirm |
| Anchor rejects `StaticComponent` | Constraints join bodies only | Kinematic `BodyComponent` |

## See also

- [Bepu: Who Owns the Transform?](bepu-transform-ownership.md)
- [Constraint Motors example](../code-only/examples/constraint-motors.md)
- [Physics Extensions](index.md)