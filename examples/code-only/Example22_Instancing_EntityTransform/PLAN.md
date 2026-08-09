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

## 3. Phase 2 — user-managed GPU buffers (skip upload when asleep)

Switch the fast type to the `InstancingUserBuffer` path
(`engine/Stride.Engine/Engine/InstancingUserBuffer.cs`): the render feature then treats buffers as
user-managed (`BuffersManagedByUser`, `InstancingRenderFeature.cs:158`) and never uploads.
We own two `Buffer<Matrix>` (dynamic, structured), upload with `SetData` from the game's
`GraphicsContext.CommandList` only when something moved. Fixes the F3+F6 upload half:
settled pile = zero CPU *and* zero PCIe traffic per frame.

Open points: where to run the upload (an `ISceneRenderer`/script with access to the command list),
buffer growth policy, and setting `BoundingBox`/`InstanceCount` ourselves (both are settable on
`InstancingUserBuffer`).

## 4. Phase 3 — promote to toolkit + tests/benchmarks

- Move the polished type(s) into `Stride.CommunityToolkit.Bepu` (it needs `BodyComponent` for the
  sleep check; a Bepu-free variant without sleep-skip could live in `Stride.CommunityToolkit.Engine`).
- Optional Bepu-native variant: read poses straight from Bepu's active set instead of
  `TransformComponent`, updating only awake bodies.
- Consider a scene-processor or helper so registration/unregistration is automatic again
  (entity removed from scene must not leave a stale `TransformComponent` in the list).
- **Unit tests:** `InvertRigid` vs `Matrix.Invert` equivalence on random rigid transforms;
  swap-remove bookkeeping; bounding-box correctness vs stock implementation.
- **Benchmarks:** small BenchmarkDotNet project (e.g. `benchmarks/`) comparing per-frame update at
  1k/10k/100k instances: stock gather+invert vs fast (rigid, SIMD-general, parallel on/off).
  In-game FPS overlay stays the integration-level check; micro-benchmarks guard regressions.

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

## 7. Status

- [x] Analysis of the Stride instancing hot path
- [x] Phase 1: `FastEntityTransformInstancing` + timed stock master + key 3 + overlay timings
- [x] Phase 1: visual verification (see section 6b — sleep-skip confirmed, 7.5× on gather/invert)
- [ ] Phase 2: user-managed buffers, upload only when dirty
- [ ] Phase 3: toolkit promotion, unit tests, BenchmarkDotNet project
- [ ] Phase 4: upstream PRs (needs discussion with Stride maintainers first)
- [ ] Phase 5: write up v2 proposals for a Stride discussion/issue
