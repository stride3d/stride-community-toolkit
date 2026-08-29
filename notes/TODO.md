# TODO

Work that is agreed and waiting, ordered by what to do first. Distinct from
[ARCHITECTURE.md](ARCHITECTURE.md), which is unagreed API-design observations.

Most of this came out of one long session investigating a `Body2DComponent` memory runaway; the
measurements referred to below are from that work and are recorded in
`docs/manual/physics-extensions/bepu-transform-ownership.md`. Section 2 came out of a later audit of
every 2D example, prompted by the spawn menu freezing on the default Polygon.

---

## 1. Finish PR stride3d/stride#3349

Reviewers have been waiting since 11 Aug. The code and docs work is done and unpushed in the working
trees of the `stride` and `stride-docs` clones; what remains is the reply.

Done, uncommitted, builds clean, all 6 engine and 13 toolkit `Body2D` tests pass:

- ~~Mechanical review fixes~~ — "Stride's default scale" and "simulated by Bepu's ordinary 3D solver"
  dropped, velocities written unconditionally through a single `ref` into `bodyRef.Velocity` (one
  pair of array lookups instead of three), `ref var inverseInertia` instead of copy-modify-assign,
  no empty `///` lines left, and both `ISimulationUpdate` summaries restructured to say what the
  method *is* with the behaviour moved to `<remarks>`.
- ~~`ZTolerance` should throw~~ — now `ArgumentOutOfRangeException` for non-finite and non-positive,
  paired with `[DataMemberRange(0.0001, 4)]`. Both test suites were asserting the old silent-revert
  behaviour and were rewritten; the toolkit copy got the same change, so deleting it later cannot
  change behaviour under callers.
- ~~Delete the hull tuning block~~ — gone from both copies, along with `HasConvexHull` and the three
  `Hull*` constants. Nothing outside the two files referenced them. Only hull-collider bodies were
  ever affected, so among the 2D shapes that is Triangle and Polygon, and among the 3D shapes the
  stress pile uses it is Cone, Teapot, TriangularPrism and Torus — everything else went through an
  analytic collider and never reached the block. **Needs a visual pass before pushing**, see below.
- ~~Split the docs by audience~~ — class `<remarks>` now carries only what a user can observe
  (Z is engine-managed, a tilt at attach time is frozen not reset, energy is not conserved the way a
  native 2D solver would), mechanism moved to plain `//` comments. Same treatment applied to the
  toolkit copy. New manual page `en/manual/physics/2d-bodies.md` in the `stride-docs` `bepu-2d`
  branch, linked from `toc.yml`, `physics/index.md`, `rigid-bodies.md` and
  `kinematic-rigid-bodies.md`, and it says outright that the component is still maturing and that a
  solver-side constraint is being investigated.

Still open:

- **Reply to Eideren** — lead with the rank table (full tensor fine, all-zeroed fine, partly-zeroed
  fails 3/3 at 20k), then answer the constraint question. Position agreed: current approach as the
  interim, his processor fallback (collect the components and apply the corrections right after
  `ISimulationUpdate.SimulationUpdate`, in parallel) as the concrete next step, custom constraint as
  a follow-up PR. Include the `MaximumRecoveryVelocity` measurement — it made no difference to the
  runaway, 3/3 — since that is what justifies deleting the hull tuning block rather than retuning it.
- **Eyeball the hull-collider examples without the tuning** — the two copies are now identical apart
  from four unavoidable differences (licence header, namespace and the extra `using`, the paragraph
  explaining why the copy exists, and the velocity block, since `BodyComponent.BodyReference` is
  `internal` and unreachable from outside the engine assembly). That makes the toolkit the place to
  test the engine change. Worth a look, in order: `Example01_Basic2DScene_StressPile` on
  TriangularPrism at 10k and 20k, which is the exact configuration behind the inertia measurements;
  then Cone, Teapot and Torus in the same example; then Triangle and Polygon in
  `Example01_Basic2DScene_SpawnMenu`. Expect piles to be bouncier and to settle less readily — that is
  the tuning doing what it did. What would change the plan is a *runaway*, since the measurement says
  `MaximumRecoveryVelocity` made no difference to that, 3/3.

## 2. 2D shapes — found by auditing the 2D examples

The first three items below are one investigation: the 2D spawn menu froze whenever several default
Polygons landed on each other. The evidence — incidence tables, side-count sweeps, the traced chain
from NaN contact depth to the hang — is in `notes/upstream/bepu-hull-contact-nan.md`, with a runnable
repro in `examples/code-only/_Temp2DProbe`. Only what is left to *do* is repeated here.

- **File the Bepu hull-contact `NaN` issue** — verified unfixed on master `16ecf9cf`, and no existing
  issue covers it (tracker searched 20 Aug). The bepuphysics2 clone carries a three-file branch
  payload, all verified: a direct `CollisionBatcher` test, a demo, and its one-line `DemoSet`
  registration. Both are expected-to-fail, so they attach to the issue and become the fix PR's
  regression material. Paste-ready title and body in
  `notes/upstream/bepu-hull-contact-nan-issue.md` — push the branch, swap in the compare URL, file it.
- **File `Tree.Add` never returning once a pose is `NaN`, separately** — it turns a recoverable `NaN`
  into a hard hang with no diagnostic, and it is the same unguarded-recursion family as the
  `Tree.Refit2WithCacheOptimization` item in §5. Worth its own report because it would still bite on
  any *future* `NaN` long after the contact bug is fixed.
- **Nothing in the toolkit contributes to the freeze** — the chain is traced end to end, so the
  upstream fix closes it here with no change on our side. Recorded so it is not re-investigated.
- **Letter meshes use box colliders on purpose** — letter-shaped convex hulls jostling each other is
  exactly the configuration above. Do not "improve" `Example_CubicleCalamity/Setup/FallingLetters.cs`
  into hull colliders.
- **Decide what the toolkit does in the meantime** — the incidence sweep closed the question of
  dodging by side count: there is no safe `Sides` value. 5 is the *worst* axis-aligned (~40% per
  overlapping pair — and it is what the spawn menu example currently ships, `Size = new Vector2(1, 5)`),
  7 fails at 37%, and even 3 and 4, clean when axis-aligned, fail at 17% once the bodies have
  rotated. Preference stands: leave the default, document the hazard next to
  `PolygonProceduralModel.Sides` (now covering Triangle and custom-vertex polygons too), and let the
  upstream fix land. The only real mitigation available today is not spawning hull shapes
  overlapping each other.
- **`Create2DPrimitive` writes back into the caller's options** — `options.Size ??= ...` for Capsule
  and Rectangle mutates the object the caller passed in
  (`src/Stride.CommunityToolkit/Games/GameExtensions.cs:72`). Reusing one options instance across
  shapes silently carries the size over; measured: a capsule then a circle from the same instance
  gives a circle of radius 0.25 instead of 0.5. `AddBepu2DPhysics` explicitly encourages that reuse
  in its own comment. Fix is a local variable instead of a write-back; the mutation buys nothing,
  since the class defaults already agree with the collider defaults.
- **`Depth` is ignored unless `Size` is also set** — for Square, Circle and Triangle the null-size
  branch of `Get2DColliderShape` returns a collider with its own default Z (1), so
  `new() { Depth = 0.2f }` alone silently keeps a collider 1 unit deep. Rectangle, Capsule and
  Polygon honour it. Same file, `src/Stride.CommunityToolkit.Bepu/EntityExtensions.cs:112`.
- **Square's collider ignores `Size.Y`** — `new() { Size = new(size.Value.X, size.Value.X, depth) }`
  while the mesh is built from `Size` as given, so a Square with `Size = (2, 1)` draws 2x1 and
  collides as 2x2. Either use both components or reject a non-square size.
- **Capsule throws for a wide capsule** — `Length = size.Y - 2 * size.X` goes negative when
  `Size.Y < 2 * Size.X`, and Stride's `CapsuleCollider.Length` setter validates greater-than-zero,
  so `Size = (0.5f, 0.8f)` throws. The mesh silently clamps to `radius * 2.01f` instead. Pick one
  behaviour and apply it to both.
- **`PolygonCollider` is the only hull collider not using `SharedHullCache`** — cone, teapot, torus
  and triangular prism all share hulls; polygon builds a fresh `DecomposedHulls` per body. Confirmed
  from a running scene: two polygon bodies got shape indices 1 and 2. That is the per-body hull cost
  and the static-`BufferPool`-from-a-finalizer race, both already documented, still live on one path.
- **Bullet's 2D collider mapping is missing shapes and flags** —
  `src/Stride.CommunityToolkit.Bullet/EntityExtensions.cs:135`: Triangle and Polygon fall through to
  a message-less `InvalidOperationException`; the null-size branches for Circle and Capsule omit
  `Is2D = true` that the sized branches set; Rectangle and Square hardcode Z to `0` and the `depth`
  parameter is never read; and Square uses `Size.Y` where the Bepu path uses `Size.X`.

## 3. Toolkit memory work — measured payoff

- **Shared-model helper** — `Create3DPrimitive` builds a mesh and a pair of GPU buffers per call, so
  10,000 spheres cost 1.5 GB against 400 MB when the model is shared. Retires a workaround that
  Example01, Example22 and the stress pile all hand-roll.
- **`NumberOfTextureCoordinates = 1`** — the default of 10 inflates every vertex from 48 to 84 bytes
  by duplicating one UV ten times. One line, 43% off every mesh the toolkit generates.
- **Cached mesh mutation bug** — `PrimitiveProceduralModelBase.Generate` mutates `data.Vertices` in
  place for `LocalOffset` and `Scale`, and five 2D procedural models hand out *shared* cached
  instances. Silent and cumulative: the second model built with a `Scale` comes out wrong.

## 4. Toolkit correctness and tidy-up

- **Rewrite `ModelComponentExtensions.GetMeshData`** — it does `*(Vector3*)(bytePtr + vHead)`, which
  assumes the position is the first element of every vertex and is a full `float3`. Any imported mesh
  with a different layout silently yields garbage. Rewrite on `VertexBufferHelper` from Stride
  PR #2858 (present in 4.4.0-beta5); it is a net deletion and removes `unsafe` from the file.
- **Drop the `.ToArray()` round-trips** — five procedural models allocate the array, wrap it in a
  `Span`, then copy it into a second array. Matches Stride's own direction in #2368 / #2369.
- **Cache hygiene** — the procedural model caches are plain `Dictionary` mutated without
  synchronisation, unbounded, and `PolygonProceduralModel` builds a string key per vertex per lookup.
- **Four small ones** — `Get3DColliderShape` throws a message-less `InvalidOperationException` for
  `InfinitePlane`; `Procedural2DModelBuilder` ignores its `depth` argument for the mesh while the XML
  doc claims it makes the shape 3D; `Capsule2DProceduralModel` has a branch that is always taken and
  four lines of commented-out code; `PolygonProceduralModel` validates its points twice.
- **Delete `DebugTextDropdown.Draw()` and `Position`** — unused by anything now that both examples
  register overlay sections, and it is exactly the trap the spawn-menu example fell into: a dropdown
  drawn standalone ignores the overlay's reposition and hide keys.
- **DocFX metadata silently fails for four projects, and a clean checkout would notice** — found
  Aug 2026 while auditing the manual. `Stride.CommunityToolkit.DebugShapes`, `.ImGui`, `.ImGuiNet`
  and `.Linux` (plus `.Windows`) reference `Stride.AssetCompiler`, which pulls SSH.NET 2023.0.1 and
  trips **NU1903**. DocFX reports it as `[Failure] Msbuild failed when processing …` and then
  **exits 0**, so nothing fails. Because `docs/api/*.yml` is gitignored, the local build only passes
  on stale yml already on disk: on a fresh clone those namespaces produce no API pages at all, and
  every `xref` into them breaks — `manual/rendering/debug-shapes.md` links into one. Confirm against
  a real CI log before changing anything; the fix is upstream, in the §5 package cleanup.

## 5. Upstream reports and PRs

- **Tell Norbo about bepu #2495** — the technique he advised there, an infinite moment of inertia
  about a *specific* axis, is the exact configuration that corrupts the narrow phase. The
  highest-value report we have, because it is his own recommendation. Now reproduced with no Stride
  at all, single-threaded and deterministic 3/3, and it needs *both* a rank-1 tensor and a
  triangular-prism hull — cube hull, box and sphere are all fine with the same tensor. Write-up in
  `notes/upstream/bepu-rank1-inertia-corruption.md`, runnable harness in `examples/code-only/_Temp2DProbe`.
- **Bepu: `Tree.Refit2WithCacheOptimization`** — unguarded recursion, stack overflow from a perfectly
  regular lattice of touching bodies, deterministic in about ten seconds. Precedent: commit
  `7d0b80d4` fixed the same class of bug for tree *queries*; `Tree_Refit2.cs` is untouched since 2023.
- **Stride: `RestoreHelper` RID fix** — verified working in the local clone, ready to PR.
- **Stride: cylinder over-allocation** — `GeometricPrimitive.Cylinder` reserves `tessellation * 4`
  vertices and writes `* 2`, shipping 64 zeroed vertices per cylinder to the GPU. One character.
- **Stride: `ConvexHullCollider` finalizer** — returns unmanaged buffers to a *static* `BufferPool`
  from a finalizer, racing the simulation's worker threads.
- **Bepu: intermittent `AccessViolationException`** — no longer unexplained, and no longer a
  separate report. It is the multithreaded face of the rank-1 inertia corruption above: the same
  configuration dies as an `AccessViolationException` in `NarrowPhase.ExecutePreflushJob` with many
  threads and as a deterministic `IndexOutOfRangeException` in `NarrowPhase.UpdateConstraint` with
  one. `Stack overflow`, `Internal CLR error (0x80131506)` and silent process death are the same
  corruption surfacing elsewhere. Fold into the report rather than filing separately.
- **Stride: `sources/Directory.Packages.props` cleanup** — four dead `PackageVersion` entries, the
  `ServiceWire` → `System.IO.Pipes 4.3.0` chain behind most of the legacy packages, and the SSH.NET
  advisory. The toolkit has no central package file of its own, so this is entirely an upstream
  change — but the advisory reaches us anyway, see the docs-build item in §4.
- **Stride docs: instancing manual page** — the manual has no page on instancing at all
  (`grep -ri instancing en/manual` returns nothing). A full draft exists: title, issue body, proposed
  location and page content.
- **NuGet signature verification** — raise `DOTNET_NUGET_SIGNATURE_VERIFICATION` with maintainers.

## 6. Example hygiene

Small, found while auditing every 2D example. All nine build clean.

- **`Example_2D_Playground` needs finishing or dropping** — commented-out blocks throughout,
  unused usings (`System.Xml.Linq`, `System.Reflection`), and it calls `Add3DGround` and
  `Add3DCameraController` in a 2D playground. It deliberately carries no metadata block (D42), so it
  is invisible to the docs and both launchers either way — this is only about whether it stays.
- **Two examples are `enabled: false` until they build again** — `Example04_MyraUI` (blocked on the
  Stride 4.4 package split) and `Example07_CubeClicker` (not in the `.slnx`, so nothing builds it).
  Both keep their hand-written doc pages, which the generator never touches.
- **Two example folders break the `Example<NN>_<Name>` convention** — the `Example_`-prefixed demo
  games. Renaming touches the `.slnx`, `.sdpkg`, `.csproj` and namespace. Decide rather than drift.

## 7. Text, letters and lighting follow-ups

Distilled from `notes/plans/cubicle-calamity-scoring.md` when that plan was retired (Aug 2026).
Only toolkit-level work lives here; game-specific features and decisions are tracked in
`examples/code-only/Example_CubicleCalamity/README.md` under "Future improvements".

- **Screen-corner orientation gizmo, as a toolkit feature.** Every code-only example faces "where
  the hell is X" with no editor viewport to answer it; a world-space gizmo either buries itself in
  the scene's content or sits at a misleading offset. An axis widget pinned to a screen corner the
  way editor viewports do it generalises to every example, and belongs in `ARCHITECTURE.md` if
  pursued. (The interim placement of the demo game's two markers is that game's own decision,
  tracked in its README.)
- **`LetterMeshFactory` style parameters** — stroke width, glyph width, spacing and depth are
  constants today, and the stroke is woven into the shared segment grid (`TopY`, `MiddleY`, ... all
  derive from it), so making it configurable means passing a metrics/style object down into every
  glyph builder rather than exposing one number. The bar-built glyphs would follow automatically;
  the hand-sketched polygons (V, X, Y, Z, K, the R leg, the Q tail, D's chamfers) have tuned
  constants that need to scale with the stroke or be re-authored. The existing area-invariant tests
  generalise: run them per style over a few stroke values. Worth doing once a second real consumer
  besides Cubicle Calamity and the gallery appears.
- **Lighting for solid lettering / model showcases** — `AddAllDirectionLighting` (six directionals
  down the world axes) turned out to be the difference between black-faced letters and a readable
  gallery in `Example01_Letters3D`, same as in Cubicle Calamity. `AddStudioLighting` (key/fill/rim,
  yaw-steerable, key-only shadows) now exists in `GameExtensions` next to it — verified against the
  letters gallery, where it models shape visibly better than uniform light. Still open: (a) the
  cheaper all-direction equivalent — three lights plus an ambient term instead of six directionals —
  and (b) sweeping the examples to see which ones read better under the studio rig and swapping
  them over.
- **`TextPositionMode` cannot centre on screen** — corners and explicit pixels only, so Cubicle
  Calamity's game-over banner needs a four-line `ScreenCentreTextScript` recomputing its position
  every frame. A centre option belongs in the component.
- **Skybox source cubemap has no mip chain** — `skybox_texture_hdr.dds` ships `dwMipMapCount = 1`,
  so the GGX prefilter's importance sampling reads mip 0 for all 1024 samples: the slowest and
  noisiest path. Hypothesis, not measured: shipping a mipped DDS (or generating mips at load) should
  improve reflection quality and prefilter speed more than any size change. Needs a before/after
  comparison; do not change the texture without one.

## 8. Later

- **Custom one-body 2D constraint** — only once Eideren has picked a direction. Prototype in the
  toolkit first, where nothing needs review, score it against the current approach with the harness,
  then a follow-up PR. Stride's own `CharacterMotionConstraint` is a complete worked template, and
  `Solver.Register<T>()` is public, so no engine change is needed.
- **`related:` on `Example01_Basic3DScene_VBasic`** — the last of the 59 examples without one. The
  rest of the cross-linking sweep is done.
- **Keep `examples/code-only/_TempMemProbe`** — the memory measurement rig, and no longer a deletion
  candidate. It is the only thing that can say whether the constraint work above is actually an
  improvement, and it stays useful for any future allocation question.
- **Keep `examples/code-only/_Temp2DProbe`** — the 2D rig: shape/side/radius/depth sweeps, a
  pose-`NaN` detector, a hull inspector, and two Stride-free Bepu reproductions. The write-ups moved
  to `notes/upstream/`; the runnable repros stay here, and they are the fastest way to re-check any
  of the Bepu claims above.

---

## Four things to watch, not tasks

**Do not tune friction yet.** Bepu's August fix removes a `1/N` contact-count divisor, making friction
2–4× stronger and, more importantly, consistent regardless of how many contacts a manifold has. Ross
retuned his own demos in the same commit (`FrictionDemo` went from `3` to `0.75`). It is **not** in
`2.5.0-beta.28`, which is what Stride 4.4.0-beta5 resolves to. Anything tuned today will be far too
grippy when Stride bumps.

**The `AccessViolationException` is explained.** It was thought to be a separate, unexplained
failure from the partly-zeroed inertia tensor. It is not: a pure-Bepu repro produces it from exactly
that tensor, and reduces it to a deterministic `IndexOutOfRangeException` when run single-threaded.
The fix already shipped (scaling instead of zeroing) covers it. See
`notes/upstream/bepu-rank1-inertia-corruption.md`.

**Beware spawn overlap when measuring.** A first pass at the pure-Bepu repro used a grid spacing
narrower than the shape and appeared to show that the inertia lock was irrelevant. It is not — with
a spacing wider than the shape, only the rank-1 tensor fails. Space the grid wider than the shape
before drawing any conclusion. Precision matters though: the failure mode of a dense *grid* of
interpenetrating bodies over many steps is distinct from a single overlapping *pair*. The incidence
sweep's box control shows an overlapping pair of analytic boxes is always clean over 8 steps, at any
offset and rotation tried — for a pair, only the hull path produces non-finite state, and it does so
on step one.

**Do not revisit `OutOfPlaneInertiaScale` speculatively.** If Bepu makes a rank-1 inverse inertia
tensor safe, going back to `= 0` becomes viable and is marginally more exact — truly infinite
out-of-plane inertia rather than 10,000× stiff. It is still not worth it: it would require a minimum
Bepu version the toolkit cannot enforce, since Bepu arrives through whatever Stride resolves, and the
measured behaviour is indistinguishable — every scale from 1e-1 to 1e-12 is stable, so the constant is
not load-bearing for the crash. If Bepu instead rules a rank-1 tensor unsupported and adds validation,
scaling is the only option and the question is closed for good.
