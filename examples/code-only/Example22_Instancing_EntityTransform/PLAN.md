# Instancing Performance Plan

Goal: make `InstancingEntityTransform`-style instancing (Example22) faster using modern C#
(spans, SIMD, parallelism, pooling), first as an example-local prototype, later as a toolkit
feature, and finally as surgical upstream PRs to Stride itself.

All Stride file references below are relative to `D:\Projects\GitHub\stride\sources`.

## 1. Findings — where the per-frame time goes (N = 20,000 instances)

| # | Cost | Where | Detail |
|---|------|-------|--------|
| F1 | Gather loop | `engine/Stride.Engine/Engine/InstancingEntityTransform.cs:57-64` | Single-threaded; three heap dereferences per instance (`instance.Entity.Transform.WorldMatrix`) + 64 B copy. Cache-miss dominated. |
| F2 | Inverse loop | `engine/Stride.Engine/Engine/InstancingUserArray.cs:86-91` | Full scalar 4×4 `Matrix.Invert` per instance, single-threaded. Stride math is not SIMD. Likely the biggest CPU line item. |
| F3 | GPU upload | `engine/Stride.Rendering/Rendering/InstancingRenderFeature.cs:161-162` | World + inverse buffers re-uploaded every frame: 20k × 64 B × 2 ≈ 2.5 MB/frame, even when nothing moved. |
| F4 | Allocation churn | `InstancingEntityTransform.cs:52`, `Processors/InstancingProcessor.cs:61-67` | Exact-size `Matrix[]` re-allocated on growth (1.25 MB LOH at 20k); GPU buffers disposed + recreated on growth with zero headroom. |
| F5 | O(n) removal | `InstancingEntityTransform.cs:41` | `List.Remove` per instance → clearing a 20k pile is O(n²). |
| F6 | No dirty tracking | whole path | Once Bepu puts the pile to sleep, F1+F2+F3 are 100% redundant but still run every frame, forever. |

Inverse matrices are only consumed by the shader for normal transforms
(`engine/Stride.Rendering/Rendering/Transformation/TransformationInstancing.sdsl:31-40`).

## 2. Phase 1 — example-local prototype (no engine changes) ✅ this session

New file `FastEntityTransformInstancing.cs` in this folder:

- **`FastEntityTransformInstancing : InstancingUserArray`** — a drop-in replacement for
  `InstancingEntityTransform`. It cannot reuse `InstanceComponent` auto-registration
  (`AddInstance`/`RemoveInstance` are `internal` to Stride.Engine), so the example registers
  entities explicitly. What it fixes:
  - F1: caches `TransformComponent` refs at registration (one dereference instead of three),
    iterates via `CollectionsMarshal.AsSpan`, gathers in parallel (`Parallel.ForEach` over ranges).
  - F2: rigid-transform inverse (transpose rotation + rotate-negate translation, ~10× cheaper —
    valid for physics bodies, which never scale) with a SIMD `System.Numerics.Matrix4x4.Invert`
    fallback (`AssumeRigidTransforms = false`); Stride `Matrix` and `Matrix4x4` are layout-identical,
    so `Unsafe.As` bridges them for free. Runs in the same parallel pass as the gather,
    fused with the bounding-box reduction.
  - F4: power-of-two array growth instead of exact-size reallocation.
  - F5: O(1) swap-remove via an index dictionary.
  - F6 (CPU half): when every registered `BodyComponent` is asleep and the set is unchanged,
    skips gather + invert + bbox entirely. (GPU upload still happens — engine-managed; see Phase 2.)
- **`TimedInstancingEntityTransform : InstancingEntityTransform`** — stock behaviour + a
  `Stopwatch` around `Update()`, so the overlay can show stock vs fast timings side by side.

`Program.cs` gains key **3** (drop cubes on the fast master) and overlay lines showing per-frame
update cost of both masters plus a "skipped (all asleep)" indicator.

**How to verify visually:** drop 20k with SHIFT+1 (stock) vs SHIFT+3 (fast), let the pile settle,
compare the update-ms overlay numbers and FPS. Expect the fast update cost to drop to ~0 ms once
bodies sleep, and to be several× lower than stock while they fall.

## 3. Phase 2 — user-managed GPU buffers (skip upload when asleep) ✅ implemented

`FastBufferedEntityTransformInstancing : InstancingUserBuffer` in this folder, key **4**. On the
`InstancingUserBuffer` path the render feature treats buffers as user-managed
(`BuffersManagedByUser`, `InstancingRenderFeature.cs:158`) and never uploads them itself. Fixes the
F3+F6 upload half: settled pile = zero CPU *and* zero PCIe traffic per frame.

Design (the open points, resolved):

- **Reuses Phase 1 by composition**: wraps a `FastEntityTransformInstancing` for registration,
  parallel gather, rigid inverse and sleep-skip; adds only buffer ownership on top. No duplicated
  gather code.
- **Where the upload runs**: a tiny `SceneRendererBase` (`InstancingBufferUploadRenderer`) inserted
  as the FIRST child of the compositor's `Game` collection. Verified frame order in
  `engine/Stride.Engine/Rendering/Compositing/GraphicsCompositor.cs:203-243`:
  processors (gather) → `Game.Collect` → `Extract` → `Prepare` → `Game.Draw` → flush.
  So `CollectCore` creates/grows buffers on the main thread *before* Extract touches them, and
  `DrawCore` uploads via `drawContext.CommandList` *before* the camera renderer records the scene,
  giving same-frame data with no latency. (The toolkit's `AddSceneRenderer` appends after the scene
  renderer, which would add a frame of latency — hence manual `Children.Insert(0, ...)`.)
- **Growth policy**: power-of-two capacity; `InstanceCount` is clamped each frame to the capacity of
  the buffers the processor already handed to the render feature, so a growth frame draws the old
  capacity once and the full count the next frame — never a null or undersized buffer in Extract
  (which would throw: `bufferUploaded[null]`).
- **Buffer retirement**: replaced buffers are disposed two `Collect`s later, because the frame that
  triggered the growth still has the old buffer bound via `RenderInstancing`/`Prepare`.
- `BoundingBox` and `InstanceCount` are settable on `InstancingUserBuffer`; set after each gather.

**How to verify visually**: drop with 4, let the pile settle - both the update AND upload lines
show "skipped"; compare against key 3 where the engine still uploads every frame.

## 4. Phase 3 — promote to toolkit + tests/benchmarks ✅ implemented

Shipped API (the prototypes in this folder are gone; the example now consumes the toolkit):

| Type / helper | Project | Purpose |
|---|---|---|
| `EntityInstancing` | `Stride.CommunityToolkit` (`Rendering.Instancing`) | The fast gather: cached transform refs, fused parallel gather + rigid inverse + bbox, pooled arrays, O(1) swap-remove. No physics dependency. |
| `BepuEntityInstancing` | `Stride.CommunityToolkit.Bepu` | Adds the sleep skip via `CanSkipUpdate()`. |
| `BufferedEntityInstancing` | `Stride.CommunityToolkit` | Owns its GPU buffers; wraps any `EntityInstancing`. `IDisposable`. |
| `InstancingBufferUploadRenderer` | `Stride.CommunityToolkit` | Compositor hook: creates/grows buffers in Collect, uploads in Draw. |
| `game.AddInstancingSupport()` | `Stride.CommunityToolkit` | Adds `InstancingRenderFeature` to the `MeshRenderFeature`; idempotent. Removes the hand-wiring every example repeated. |
| `game.AddInstancingBufferUpload(...)` | `Stride.CommunityToolkit` | Inserts the upload renderer ahead of the scene renderer; reuses an existing one. |

**Key design decision.** The sleep skip needs Bepu, but the gather had to stay Bepu-free so it could be
unit-tested and benchmarked without a simulation (`BodyComponent.Awake` is `false` with no
`BodyReference`, so a headless harness would report "all asleep" and measure nothing). They are split
by a small `protected` hook protocol - `CanSkipUpdate`, `OnInstanceAdded`, `OnInstanceRemoved(index,
lastIndex)`, `OnInstancesCleared` - which lets `BepuEntityInstancing` keep a body list in lockstep
with the transforms through swap-removes at zero per-frame cost. `InstanceHooks_MirrorSwapRemoveOrdering`
covers that contract without needing physics.

Adopted by: Example22 (four-way comparison, keys 1-4) and Example_Bepu_Playground (key I). The
playground's `Create2DPrimitive` -> `Remove<ModelComponent>` waste its own TODO complained about is
gone: instances are now bare `Entity` + `AddBepu2DPhysics`, no throwaway model or GPU buffers, and
its master is created once instead of per keypress.

### 4a. Benchmark results (BenchmarkDotNet, short job, 2026-08-09)

`benchmarks/Stride.CommunityToolkit.Benchmarks` -> `InstancingGatherBenchmarks`. Baseline reproduces
`InstancingEntityTransform.Update` and calls the real `InstancingUserArray.Update` for the inverse and
bounding-box half, so it is engine code, not an imitation. Ratios are vs stock; lower is better.

| N | Stock | Fast sequential | Fast parallel | Parallel, general inverse |
|---|---|---|---|---|
| 256 | 8.5 us | **4.8 us (0.56)** | 29.0 us (3.40) | 35.1 us (4.12) |
| 1024 | 32.1 us | **26.5 us (0.83)** | 36.1 us (1.12) | 36.6 us (1.14) |
| 2048 | 69.3 us | 36.9 us (0.53) | **36.5 us (0.53)** | 38.5 us (0.56) |
| 4096 | 162.2 us | 64.7 us (0.40) | **47.0 us (0.29)** | 48.3 us (0.30) |
| 8192 | 292.3 us | 133.5 us (0.46) | **50.0 us (0.17)** | 53.4 us (0.18) |
| 32768 | 1369.1 us | 670.0 us (0.49) | **152.5 us (0.11)** | 218.3 us (0.16) |

- **`ParallelThreshold = 2048` is confirmed, not guessed.** Sequential and parallel are level at 2048
  (36.9 vs 36.5 us); sequential is 6x better at 256, parallel 2.7x better at 8192. The guessed default
  landed on the crossover.
- **Sequential alone is ~2x stock at every size** - that is the cached transform refs plus the rigid
  inverse, with no threading involved. **Parallel reaches 9x stock at 32768.**
- **The rigid inverse earns its keep mainly at scale**: level with the SIMD general inverse up to
  8192, 1.43x faster at 32768.
- **The parallel path allocates ~7-9 KB per call** (`Parallel.ForEach` + `Partitioner` + closures)
  against 56 B sequential. At 60 fps that is ~0.5 MB/s of Gen0 churn for one master. Worth replacing
  with Stride's pooled `Dispatcher.For` - noted as a follow-up, not done.
- Caveat: short job on a working machine, so the error bars are wide (`Stock` at 4096 has an error as
  large as its mean). The ordering was stable across two runs; treat the ratios as indicative.

### 4c. Phase 3 leftovers (deliberately not done)

- **Automatic unregistration.** An entity leaving the scene stays registered, unlike the engine's
  `InstanceComponent`. A scene processor or a component wrapper could restore that; it needs care,
  because the whole point of explicit registration is avoiding per-instance component overhead.
- **Bepu-native gather**: read poses straight from Bepu's active set instead of `TransformComponent`,
  touching only awake bodies. Would beat the sleep skip in the partly-settled case, which is common.
- **`Dispatcher.For` instead of `Parallel.ForEach`** to kill the ~8 KB/frame parallel allocation
  (see 4a); Stride's dispatcher pools its state and is already used by `InstancingProcessor`.
- **Multi-master benchmark.** `InstancingProcessor` dispatches masters in parallel, so several
  masters each forking again may shift the crossover up. Only the single-master case was measured.
- **Growth-frame vs steady-state split** for the buffered path (peer-review methodology point):
  buffer reallocation spikes are expected and amortised, but they were not measured separately.
- **`Body2DComponent` verification** (see section 8).

### 4b. Which existing examples benefit (analysed 2026-08-09)

| Toolkit piece | Example22 | Example_Bepu_Playground | Example21 |
|---|---|---|---|
| Phase 1 fast entity-transform type | yes | yes (key I) | no - and don't change it |
| Phase 2 buffered type / static buffers | yes (settled pile) | yes (key O) | yes |
| `AddInstancingSupport()` helper | yes | yes | yes |

- **Example_Bepu_Playground** `AddInstancedShapes` (key I) is exactly the Phase 1 pattern:
  `InstancingEntityTransform` + physics entities. Adopting the fast type also removes the wasteful
  `Create2DPrimitive` -> `Remove<ModelComponent>` dance its own TODO complains about - with explicit
  `AddInstance(entity)` the instances can be bare `Entity + Body2DComponent`, no throwaway model.
  At 100 instances the speed gain is microseconds; the sleep-skip and the cleaner pattern are the
  real wins. Its instances carry `Body2DComponent`, hence the unit test above.
- **Example21** uses `InstancingUserArray` with matrices set once; the stock `matricesUpdated` flag
  already makes its per-frame CPU cost ~zero, so Phase 1 has nothing to offer and the example should
  stay as the canonical minimal sample. But it pays F3 forever: `bufferUploaded` is cleared every
  `Extract`, so a perfectly static wall re-uploads 2,000 x 64 B x 2 = 256 KB/frame. A Phase 2/3
  "static buffer" variant (upload once, done) fixes that, as would upstream dirty-tracking (F3 PR).
- Adoption done in Phase 3 for Example22 and the playground. **Example21 was left alone on purpose**:
  it is the canonical minimal instancing sample and its per-frame CPU cost is already ~zero. Its only
  remaining waste is the redundant upload (F3), which is an upstream fix, not an example change.

## 5. Phase 4 — surgical, non-breaking upstream PRs to Stride

Each is small and independently reviewable:

1. Swap-remove + index in `InstancingEntityTransform.Add/RemoveInstance` (order is rebuilt every
   frame, so stability is not required). Fixes F5.
2. Growth headroom (2×) for `WorldMatrices`/`WorldInverseMatrices` and for the GPU buffers in
   `InstancingProcessor.TransferData`. Fixes F4.
3. SIMD inverse: `System.Numerics.Matrix4x4.Invert` via `Unsafe.As` in `InstancingUserArray.Update`
   (two-line change). Fixes most of F2.
4. Parallelise the invert+bbox loop with `Dispatcher.For` chunks (Stride already parallelises one
   level up, but a single master gets one thread).
5. `CollectionsMarshal.AsSpan` + cached `TransformComponent` list in the gather loop. Fixes F1.

## 6. Phase 5 — bigger / breaking ideas (Stride v2 discussion material)

- Drop the CPU inverse + inverse buffer entirely: shader permutation that reconstructs the normal
  transform from the world matrix (exact for uniform scale). Halves CPU matrix work and upload.
- Pack instances as 4×3 (48 B) instead of 4×4 (64 B): −25% memory/bandwidth (what Unity/UE do).
- Span-first `IInstancing` v2: `UpdateWorldMatrices(ReadOnlySpan<Matrix>)`, or a writer model that
  maps the dynamic buffer write-discard and hands out a `Span<Matrix>` — zero managed copy, zero GC.
- Transform change-versioning so unchanged instance sets skip the whole path engine-side.

## 6b. Measured results (Phase 1, 2026-08-09)

**10,000 cubes, all settled on the ground — the clean steady-state comparison:**

| | Update phase | Draw phase | FPS | Instancing update |
|---|---|---|---|---|
| Stock (key 1) | 2.76 ms | 2.90 ms | 348 | **0.53 ms every frame** |
| Fast (key 3) | 2.44 ms | 2.44 ms | **411** | **skipped (all asleep)** |

Sleep-skip confirmed working. The Draw-phase delta (0.46 ms) matches the 0.53 ms of stock
instancing work that was removed, which is the expected result — the instancing update runs inside
`InstancingProcessor.Draw`. Net **+18% FPS** on a settled pile.

**20,000 cubes, some escaping the ground into the void (bodies never all sleep, so this measures the
gather/invert work itself rather than the skip):**

| | Frame | FPS | Instancing update |
|---|---|---|---|
| Stock (key 1) | 5.54 ms | 180.6 | 1.27 ms |
| Fast (key 3) | ~3.30 ms | 303 | **0.17 ms** |

**7.5× faster on the actual gather + invert work**, with no sleep-skip involved.

Caveat on this second run: the two measurements were not in comparable scene states (cubes still
falling and escaping, so the awake-body count differed between runs). The frame-time gap is larger
than the instancing saving alone explains; in the clean 10k test the two match, so the excess is
most likely measurement confound rather than a second mechanism. Do not quote the 20k frame times
as a like-for-like result — the 0.17 ms vs 1.27 ms update figures are the trustworthy part.

Remaining cost after Phase 1: even when the fast master skips, the engine still re-uploads the
unchanged buffers every frame (F3) — 1.28 MB/frame at 10k, 2.5 MB/frame at 20k. That is Phase 2.

**20,000 cubes, all settled (Phase 2 verification, 2026-08-09) — the full ladder, one variable at
a time:**

| Kind | Frame | FPS | Status |
|---|---|---|---|
| 1 stock | 4.18 ms | 239 | update 1.94 ms + upload, every frame |
| 3 fast (Phase 1) | 3.19 ms | 313 | update skipped, engine still uploads 2.5 MB/frame |
| 4 fast+buffers (Phase 2) | 3.04 ms | **329** | update skipped, upload skipped |

Phase 1 (sleep-skip) buys ~1.0 ms/frame at this count; Phase 2 (no redundant upload) buys a further
~0.15 ms/frame. Total: **+38% FPS over stock** on a settled 20k pile, and the instancing system's
steady-state cost is now literally zero — the remaining 3 ms frame is rendering and engine overhead
that instancing cannot touch. The Phase 2 delta also directly measures what the F3 upstream fix
(dirty-tracking the upload) would be worth to every Stride user: ~0.15 ms/frame per 20k static
instances, scaling linearly.

## 7. Status

- [x] Analysis of the Stride instancing hot path
- [x] Phase 1: `FastEntityTransformInstancing` + timed stock master + key 3 + overlay timings
- [x] Phase 1: visual verification (see section 6b — sleep-skip confirmed, 7.5× on gather/invert)
- [x] Phase 2: `FastBufferedEntityTransformInstancing` + upload renderer + key 4
- [x] Phase 2: visual verification (see section 6b - update AND upload skipped, 239 -> 329 FPS at 20k)
- [x] Peer review round 1 (2026-08-09) addressed: `ParallelThreshold` sequential path for small
      counts, hoisted merge lock, `needUpload` cleared when the set empties, `IDisposable` on the
      buffered type (engine never disposes user-owned buffers), and buffer retirement re-documented
      after verifying Stride fences GPU-side destruction (Vulkan `TemporaryResourceCollector`;
      D3D11 defers natively) - the two-frame delay protects the managed wrapper binding, not the GPU
- [x] Peer review round 2 addressed: `ref` instead of `in` for the non-readonly `Matrix` (avoids
      hidden defensive copies), and `AssumeRigidTransforms` captured once per update so parallel
      ranges cannot disagree
- [x] Phase 3: toolkit promotion (`EntityInstancing`, `BepuEntityInstancing`,
      `BufferedEntityInstancing`, `InstancingBufferUploadRenderer`, `AddInstancingSupport`,
      `AddInstancingBufferUpload`), 13 unit tests, `InstancingGatherBenchmarks`, Example22 and
      Example_Bepu_Playground migrated. Prototypes deleted from this folder.
- [ ] Phase 3: visual re-verification of Example22 and the playground after the migration (user)
- [ ] Phase 4: upstream PRs (needs discussion with Stride maintainers first)
- [ ] Phase 5: write up v2 proposals for a Stride discussion/issue

## 8. Follow-up: `Body2DComponent` (separate from this plan)

Reviewed and reworked 2026-08-10, ahead of a Stride PR. See the file's own XML docs for the design.

**Stride already has a `Body2DComponent`** at `engine/Stride.BepuPhysics/Stride.BepuPhysics._2D/`
(namespace `Stride.BepuPhysics`, so the two names collide for anyone importing both). Comparison:

| | Stride's | Toolkit's |
|---|---|---|
| Rotation lock | inverse-inertia mask via `RotationLock` | inverse-inertia zeroing, same effect |
| Plane correction | separate `Simulation2DComponent` **teleports** bodies back (via `[Obsolete]` position setters) | per-body **velocity** correction before the solve |
| Setup | user must add a second scene component | self-contained |
| Sleeping bodies | iterates `ActiveSet`, so sleepers cost nothing | dispatched for all bodies, now early-outs on `!Awake` |
| Kinematic toggle | `#warning`, unhandled - lock is silently lost | detected and reapplied |
| Hull stability | none | recovery velocity / spring damping / frequency caps |

The toolkit version is better on correction quality, setup and kinematic handling; Stride's is better
on sleeping-body dispatch cost. A merged version would take the velocity correction and keep an
`ActiveSet`-driven dispatch.

**Changes made 2026-08-10:** deleted dead `HasConvexHullOld` and the unreachable recursion in
`HasConvexHull` (a `CompoundCollider` implements `ICollider` but is not a `ColliderBase`, so compounds
cannot nest); strongly typed it; early-out for sleeping bodies; out-of-plane velocity now cleared even
inside `ZTolerance`, so slow drift cannot accumulate up to the threshold; rotation lock reapplied after
a kinematic/dynamic switch; magic numbers named; full XML docs.

**The design matches every other engine, which settled two open questions.** Stride's own Bullet
integration does exactly this for 2D shapes (`Stride.Physics/Elements/RigidbodyComponent.cs:336-339`):
`LinearFactor = (1,1,0)`, `AngularFactor = (0,0,1)`. Unity, Unreal and Godot all expose the same idea
as per-axis freeze flags. Zeroing the inverse inertia *is* the angular factor and clearing
out-of-plane velocity *is* the linear one; the positional correction exists only because Bepu has no
linear factor to set and the solver can still add Z velocity after the pre-solve hook runs.
Consequently: (1) a configurable "flatten initial tilt" option was prototyped and **removed** - no
other engine re-orients an existing tilt, freezing is the standard, and less code won; (2) nothing
structural was taken from the old Stride version, because its `ActiveSet` iteration only beat a
*naive* per-body loop - with the `!Awake` early-out, per-body dispatch is cheaper overall, since a
central walk would cost every all-3D game a per-step iteration for a feature it does not use.

**Moved into Stride** on branch `bepu-2d` (2026-08-10): `sources/engine/Stride.BepuPhysics/
Stride.BepuPhysics/Body2DComponent.cs`, namespace `Stride.BepuPhysics`. The dead
`Stride.BepuPhysics._2D/` folder was deleted - a sibling of the project directory, so the SDK glob
never compiled it, and nothing in any solution or project referenced it. The in-engine copy takes the
one optimisation only available there: `BodyReference` is `internal`, so inside the assembly the
per-step work resolves it once instead of through five separate public accessors. The toolkit copy
stays until the PR lands, since the toolkit builds against the published Stride packages.

**Testing.** `engine/Stride.BepuPhysics/Stride.BepuPhysics.Tests/BepuTests.cs` already runs real
simulations through `GameTestBase` + `RunGameTest`, building entities with real bodies and colliders.
Moving the component into Stride makes that harness available, which is the right level for it - unit
tests are not, since the behaviour only exists inside a stepping simulation. Tests worth writing:

- Z drift stays within `ZTolerance` over N steps, including under a stack of bodies.
- X/Y angular velocity stays zero; a body given an X/Y spin does not tumble.
- **Bodies actually fall asleep** - the sleep skip in `BepuEntityInstancing` depends on it, and the
  commented-out `Awake = true` is exactly what used to prevent it.
- A convex-hull pile does not gain energy (total kinetic energy trends down once settled).
- Kinematic -> dynamic keeps the body in the plane (regression for the fix above).
- Cheap and harness-free: `entity.Get<BodyComponent>()` resolves a `Body2DComponent`, which is what
  makes the sleep skip work for 2D bodies.
