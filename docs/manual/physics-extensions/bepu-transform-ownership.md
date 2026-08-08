# Bepu: Who Owns the Transform?

Most surprises when working with Bepu physics come from one rule:

> **Transform sync is one-way: physics → `TransformComponent`.**

Once an entity has a body attached, the simulation owns its position and rotation. Assigning
`Entity.Transform.Position` moves the *mesh* only; the body stays exactly where the simulation put
it, and the next frame usually overwrites your change.

None of the symptoms below produce an error or a warning, which is what makes them cost hours. Each
section starts with what you actually observe.

## "My mesh moves, but nothing collides with it"

You set `Entity.Transform.Position` each frame, the mesh visibly moves, and collisions happen
somewhere else entirely - or not at all. The collider was left behind at the body's real position.

This one has a second, more confusing form. Only **awake** bodies are synced back to their transform.
A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform
writes suddenly appear to start working. The visual moves, the collider does not.

**Instead**, move the body deliberately:

- `Teleport(...)` jumps a body to a new pose without checking collisions along the way.
- For scripted motion that *should* collide, use a body with `Kinematic = true` and drive it with
  velocity.

## "I set LinearVelocity and the body stops after a while"

Setting `LinearVelocity` does **not** wake a sleeping body. The motion runs until the body sleeps,
then silently stops.

**Set `Awake = true` as well** whenever you assign a velocity to a body that might have gone to
sleep.

## "My kinematic body flies off to NaN"

This one appears within seconds when driving a body with `SetTargetPose(...)` from a per-frame
`Update`.

`SetTargetPose` derives its velocity from `(target - position) / FixedTimeStep`, which assumes
exactly **one physics tick per call**. When the frame rate falls below the physics rate, two ticks
run on that velocity, the body overshoots, the next correction overshoots further, and it diverges to
`NaN`.

`SetTargetPose` is safe when the caller runs once per physics tick - from
`ISimulationUpdate.SimulationUpdate` - or when the frame rate is pinned to the physics rate.
Otherwise, integrate a velocity you compute yourself, and add a small proportional pull towards the
ideal position to stop drift.

## "My component reference is null inside SimulationUpdate"

`ISimulationUpdate.SimulationUpdate` can run **before** `StartupScript.Start` and `SyncScript.Start`.
A component is registered with the simulation as soon as it enters the scene, whereas `Start` waits
its turn in the script system.

**Resolve references lazily** rather than caching them in `Start`:

```csharp
public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
{
    _body ??= Entity.Get<BodyComponent>();
    // ...
}
```

## "I disabled the collider but the entity still has a body"

`Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a `BodyComponent`. A `CompoundCollider`
with no shapes never attaches to the simulation, so you are left with an inert component rather than
no physics.

For a purely visual entity, use the non-physics `Create3DPrimitive` overload by passing
`Primitive3DEntityOptions` instead.

## "CS0121: the call is ambiguous"

`Create3DPrimitive` has a Bepu overload taking `Bepu3DPhysicsOptions` and a plain one taking
`Primitive3DEntityOptions`. With both namespaces imported, a bare `new()` cannot pick between them.

Pass an explicitly typed options object so the intended overload is selected:

```csharp
var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule, new Primitive3DEntityOptions
{
    Material = material,
});
```

## Summary

| Symptom | Cause | Fix |
|---|---|---|
| Mesh moves, collider does not | Writing `Transform.Position` on a body-owning entity | `Teleport(...)`, or a kinematic body driven by velocity |
| Transform writes "start working" after a while | Body fell asleep and stopped syncing | Treat sleeping as the real state; do not write the transform directly |
| Velocity-driven motion stops | `LinearVelocity` does not wake a body | Also set `Awake = true` |
| Kinematic body diverges to `NaN` | `SetTargetPose` called more than once per physics tick | Call it from `SimulationUpdate`, or integrate your own velocity |
| Null reference in `SimulationUpdate` | It can run before `Start` | Resolve lazily with `??=` |
| Inert `BodyComponent` | `IncludeCollider = false` still attaches a body | Use the `Primitive3DEntityOptions` overload |
| `CS0121` ambiguity | Two `Create3DPrimitive` overloads | Pass an explicitly typed options object |

> [!NOTE]
> Do not combine Bepu and Bullet physics components on the same entity. Bepu is the primary
> integration; Bullet is legacy and pending deprecation.
