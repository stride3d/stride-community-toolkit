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

## 4. Phase 3 — promote to toolkit + tests/benchmarks

- Move the polished type(s) into `Stride.CommunityToolkit.Bepu` (it needs `BodyComponent` for the
  sleep check; a Bepu-free variant without sleep-skip could live in `Stride.CommunityToolkit.Engine`).
- Optional Bepu-native variant: read poses straight from Bepu's active set instead of
  `TransformComponent`, updating only awake bodies.
- Consider a scene-processor or helper so registration/unregistration is automatic again
  (entity removed from scene must not leave a stale `TransformComponent` in the list).
- **`game.AddInstancingSupport()` helper**: Example21, Example22 and Example_Bepu_Playground all
  hand-wire `InstancingRenderFeature` onto the `MeshRenderFeature` with the same three lines and the
  same "easy to miss, nothing warns you" comment. One helper removes the trap everywhere, and can
  register the Phase 2 upload renderer too.
- **Unit tests:** `InvertRigid` vs `Matrix.Invert` equivalence on random rigid transforms;
  swap-remove bookkeeping; bounding-box correctness vs stock implementation; and
  `entity.Get<BodyComponent>()` resolving toolkit's `Body2DComponent` so the sleep-skip works for
  2D bodies (needed for the playground).
- **Benchmarks:** small BenchmarkDotNet project (e.g. `benchmarks/`) comparing per-frame update at
  1k/10k/100k instances: stock gather+invert vs fast (rigid, SIMD-general, parallel on/off).
  In-game FPS overlay stays the integration-level check; micro-benchmarks guard regressions.
  Methodology (from the 2026-08-09 peer review):
  - Find the sequential/parallel crossover empirically and set `ParallelThreshold`'s default from it
    (current 2048 is a guess). Include the multi-master case: `InstancingProcessor` already
    dispatches masters in parallel, so nested forking may shift the crossover up.
  - Measure steady-state separately from spawn/growth frames (buffer reallocation spikes are
    expected and amortised); report buffer capacity and resize counts alongside frame metrics.
  - Measure frame-time variance while bodies are active, not just the settled averages.

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
- Adoption itself is deferred to Phase 3 so the prototype API can still change freely.

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
- [ ] Phase 3: toolkit promotion, unit tests, BenchmarkDotNet project
- [ ] Phase 4: upstream PRs (needs discussion with Stride maintainers first)
- [ ] Phase 5: write up v2 proposals for a Stride discussion/issue
