# Bepu: Who Owns the Transform?

Most surprises when working with Bepu physics come from one rule:

> **Transform sync is one-way: physics → `TransformComponent`.**

Once an entity has a body attached, the simulation owns its position and rotation. Assigning
`Entity.Transform.Position` moves the *mesh* only; the body stays exactly where the simulation put
it, and the next frame usually overwrites your change.

Most of the symptoms below produce no error or warning. Each section starts with what you observe.

## "My mesh moves, but nothing collides with it"

You set `Entity.Transform.Position` each frame, the mesh visibly moves, and collisions happen
somewhere else entirely - or not at all. The collider was left behind at the body's real position.

This one has a second, more confusing form. Only **awake** bodies are synced back to their transform.
A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform
writes suddenly appear to start working. The visual moves, the collider does not.

**Instead**, move the body deliberately:

- `Teleport(...)` jumps a body to a new pose without checking collisions along the way. It keeps the
  body's velocity and does not wake it; the toolkit's `TeleportAtRest(...)` also stops and wakes it.
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

## "I want the model without a body"

`Body = BodyMode.None` on the options leaves the physics out altogether. A `BodyComponent` whose
`CompoundCollider` holds no shapes does not attach to the simulation until a shape is added, so a
body added by hand with an empty compound is an inert component rather than no physics.

## "Bepu crashes with an AccessViolationException"

The stack lands somewhere in the solver - `Contact4.ApplyDescription`, `AddToSimulationSpeculative`
- on a physics worker thread, after a few seconds of simulation.

**This one is unresolved.** What is known about it:

- It is **intermittent**, on the order of one run in five with the same binary and the same scene.
  A change that survives one run has not been shown to fix anything.
- Every case observed so far used a hull-backed shape - `TriangularPrism`, `Cone`, `Teapot` or
  `Torus` - at a few thousand bodies. It has not been reproduced with cubes or spheres, which is
  suggestive but not proof, since more bodies and more contacts also make it likelier.
- Sharing hull data across bodies, as described in the next section, does **not** prevent it.

Until the cause is found, keep hull-shaped body counts down, or substitute an analytic collider - a
box, a cylinder or a capsule - where the shape allows it.

## "One hull per body is slow, and freed from a finalizer"

This is a cost and a thread-safety hazard, separate from the crash above.

Stride caches the built Bepu hull against the `DecomposedHulls` **instance** it came from, so a fresh
instance per body means a fresh hull per body: ten thousand identical prisms build, store and
eventually free ten thousand identical hulls. Those hulls hold unmanaged buffers taken from a
**static** `BufferPool`, and Stride returns them **from a finalizer** - so the garbage collector can
hand memory back to that pool while the simulation is allocating from it on its worker threads.

The toolkit shares one hull per distinct shape and size through `SharedHullCache`, so
`CreatePrimitive` builds a single hull no matter how many bodies use it, and nothing is ever
finalized.

**If you build a `ConvexHullCollider` yourself, share the hull data.** Assign the *same*
`DecomposedHulls` instance to every collider rather than calling `ToConvexHullCollider()` per body;
`ToDecomposedHulls()` gives you just the data to hold onto.

```csharp
// Once
var hull = TriangularPrismProceduralModel.New(size).ToDecomposedHulls();

// Per body
Collider = new CompoundCollider { Colliders = { new ConvexHullCollider { Hull = hull } } }
```

## "Spawning bodies in a grid kills the process with a Stack overflow"

The process dies outright - no exception, just `Stack overflow.` and a stack that repeats one frame
thousands of times:

```text
Stack overflow.
Repeated 7990 times:
   at BepuPhysics.Trees.Tree.Refit2WithCacheOptimization(Int32, Int32, Int32, NodeChild ByRef, ...)
   at BepuPhysics.Trees.Tree.Refit2WithCacheOptimization(Buffer`1<Node>)
   at BepuPhysics.CollisionDetection.BroadPhase.Update2(IThreadDispatcher, Boolean)
```

The function recurses once per level of the broad-phase tree. A healthy tree over a few thousand
bodies is a dozen or so levels deep; this one has degenerated into something close to a linked list,
and the recursion has no depth guard. Bepu refines the tree a little every frame, but some scenes
degrade it faster than that repairs it.

The trigger is a **perfectly regular lattice of exactly-touching bodies** - 5,000 spheres one
diameter apart on a grid, all on the same plane, die within about ten seconds. Unlike the
`AccessViolationException` above it is deterministic and not a race: it reproduces single-threaded
and with sleeping disabled.

Break the symmetry and it goes away. Any of these survive indefinitely:

- a few centimetres of random jitter on each spawn position, plus or minus 25 mm in the snippet below - the bodies are just as close, and some
  overlap, which is fine;
- wider spacing, so nothing touches at spawn;
- random placement.

```csharp
// Instead of an exact lattice
entity.Transform.Position = new Vector3(x, y, 0);

// Nudge each one off the grid
entity.Transform.Position = new Vector3(
    x + (Random.Shared.NextSingle() - 0.5f) * 0.05f, y, 0);
```

## "A 2D pile of prisms eats all my memory"

Thousands of `TriangularPrism` bodies with a hand-written rotation lock, and within seconds the
process is holding tens of gigabytes and dies. The stack, if you catch one, is in
`NarrowPhase.ExecutePreflushJob`. The same scene with an ordinary `BodyComponent` is untroubled.

The cause is a lock that zeroes the X and Y terms of the inverse inertia tensor, leaving Z. What
matters is that only *some* terms are zeroed. Three runs each at 20,000 prisms:

| Inverse inertia tensor | Result |
|---|---|
| Full, untouched | no failure |
| Every term zeroed | no failure |
| X and Y zeroed, Z left responsive | **fails 3/3, tens of GB in seconds** |

A fully zeroed tensor is the idiom Bepu's own character demo uses - it means "no torque can rotate
this" - and it is perfectly stable. So the problem is not that the tensor is degenerate; it is that
it is degenerate in *some* directions and not others.

`Body2DComponent` scales the X and Y terms by `1e-4` instead of zeroing them, so the body is four
orders of magnitude harder to rotate out of plane than within it while every diagonal term stays
non-zero, and it clears any X or Y spin that leaks through before every step. Measured across 20 runs
at 8,000 and 20,000 bodies: no runaway, no increase in bodies squeezed out of the pile, and a pile
that settles where the zeroed version kept churning.

**If you write your own per-axis rotation lock, scale the inertia terms rather than zeroing them** -
or zero the whole tensor, if you can live without rotation on every axis. A one-body constraint,
solved alongside the contacts, is the more robust design again where the per-body cost is acceptable.

Why a partly-zeroed tensor misbehaves is not established; the evidence above is empirical.

## "My torus collides as though the hole were filled"

A convex hull is exact for a convex shape, so `TriangularPrism`, `Cone` and `Teapot` collide as they
look. A torus is not convex, and its hull spans the hole.

There is no single-shape fix: an accurate torus needs a compound built from several convex shapes,
or a `MeshCollider`, which is best kept static: a moving mesh works but costs more than a hull and
has no inside.

## Summary

| Symptom | Cause | Fix |
|---|---|---|
| Mesh moves, collider does not | Writing `Transform.Position` on a body-owning entity | `Teleport(...)`, or a kinematic body driven by velocity |
| Transform writes "start working" after a while | Body fell asleep and stopped syncing | Treat sleeping as the real state; do not write the transform directly |
| Velocity-driven motion stops | `LinearVelocity` does not wake a body | Also set `Awake = true` |
| Kinematic body diverges to `NaN` | `SetTargetPose` called more than once per physics tick | Call it from `SimulationUpdate`, or integrate your own velocity |
| Null reference in `SimulationUpdate` | It can run before `Start` | Resolve lazily with `??=` |
| Inert `BodyComponent` | A body whose compound holds no shapes | `Body = BodyMode.None` for no physics; your own shapes in the compound otherwise |
| `AccessViolationException` in the solver | Unknown; intermittent, seen only with hull shapes at scale | None known - keep hull body counts down, or use an analytic collider |
| Slow spawns and finalizer churn with hull shapes | A hull per body, freed from a finalizer into a static pool | Share one `DecomposedHulls` per shape; the toolkit does this already |
| `Stack overflow` in `Refit2WithCacheOptimization` | A degenerate broad-phase tree from a perfectly regular lattice | Jitter the spawn positions, or space the bodies apart |
| Runaway memory in a 2D prism pile | Zeroing *some* inverse inertia terms and not others | Scale the terms instead of zeroing them, as `Body2DComponent` does |
| Torus collides with its hole filled | A convex hull cannot represent a concave shape | Build a compound, or use a mesh collider for statics |

> [!NOTE]
> Do not combine Bepu and Bullet physics components on the same entity.

## See also

- [Bepu: Why Isn't My Constraint Doing Anything?](bepu-constraints.md)
- [Bepu: Bounce and Friction](bepu-contact-materials.md)
- [Physics Extensions](index.md)