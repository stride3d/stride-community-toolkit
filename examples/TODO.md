# Example Backlog

Ideas for new examples and the state of each one, covering both `code-only/` and `snippets/`.

This is the planning side of the examples. What an example *is* - levels, categories, complexity
scoring and the metadata block - is documented in [code-only/README.md](code-only/README.md), and the
contribution steps are in [the contributing guide](../docs/contributing/examples/index.md). Nothing
here duplicates those; this file only tracks what has been suggested, what is being built, and what
was decided against.

> [!IMPORTANT]
> **Bepu examples have their own build plan:**
> [`code-only/PLAN_Bepu_Examples.md`](code-only/PLAN_Bepu_Examples.md). That document owns the
> committed batch - nine specified examples with settled naming decisions, per-example specs, a build
> order, and the findings from building them. This file does not duplicate it: Bepu rows below either
> point at it or are ideas it has not claimed. **Check it before starting any Bepu example**, and
> retire it into the docs once the batch is finished; this backlog outlives it.

## Status

| Status | Meaning |
|---|---|
| Idea | Collected, not yet judged worth building |
| Agreed | Worth building, nobody has started |
| Building | Someone is working on it |
| Done | Example exists - link it, then leave the row as a record |
| Declined | Decided against, with the reason kept so it is not re-proposed |

## Adding an idea

Add a row under the matching category, with where it came from in **Source** - a forum thread, a
Discord question, an issue, or "in-repo" for something already flagged in the code. Provenance is
worth keeping: a suggestion that came from someone actually stuck on the problem is better evidence
than one invented in the abstract.

When an idea becomes an example, set it to **Done**, link the folder, and make sure the example
carries an `---example-metadata` block, since the launcher and the metadata generator read it.

## Coverage snapshot

Taken 2026-08-11, from the `category:` fields of the examples that carry metadata.

| Category | Examples with metadata |
|---|---|
| Shapes | 6 |
| Rendering | 3 |
| Physics | 3 |
| UI | 1 |
| Scripts | 1 |
| Input | 0 |
| Interaction | 0 |
| Audio | 0 |
| Gameplay | 0 |
| Performance | 0 |
| Integration | 0 |

Two things this shows, both worth acting on:

- **Six of the eleven documented categories have no example at all.** Those are the obvious gaps to
  aim new ideas at, rather than adding a seventh Shapes example.
- **Only 14 `Program.cs` files carry a metadata block, out of 63 project folders under `code-only/`.**
  Some of those folders are not examples, so the real shortfall needs an audit rather than assuming
  49 are missing - but the snapshot above describes only the examples that have metadata, so it
  undercounts actual coverage.

## Sources reviewed

What has already been mined for ideas, so it is not searched twice.

| Source | Reviewed | Notes |
|---|---|---|
| `stride/samples/Physics/BepuSample` | 2026-08-11 | **The best source for Bepu example code.** It is the old standalone demo ported into Stride and kept current - the `Components/Utils/` set is identical file for file (collision, constraint editor and toggle, gravity gun, overlap tester, raycast, rope spawner, spawner, thrower, time control, trigger usage), plus `Extensions/` helpers and a Cube Mixer scene the old demo did not have. **Where an idea below exists in both, read this one**: same concepts, current API, in-tree. |
| `Stride.BepuPhysics` standalone repo (pre-port) | 2026-08-11 | The original integration and demo, before the core was merged into Stride. Now only worth reading for what BepuSample dropped: **cars, character controller, third-person camera, navigation, 2D and soft bodies**. For everything else BepuSample supersedes it. **Treat its code as reference, never as something to copy** - the core was revised repeatedly after the port, so its API usage is stale. The `Body2DComponent` we replaced came from here. |
| `bepuphysics2/Demos/Demos` (upstream Bepu) | 2026-08-11 | Norbo's own demos, in raw BepuPhysics2 with no Stride at all - **idea-level reference only, none of the code ports**. **On physics technique these outrank BepuSample where the two disagree** (per `.github/copilot-instructions.md`): the sample shows something is possible, the demos show how the physics author intended it and often explain why the obvious approach misbehaves. BepuSample remains the better reference for *Stride API usage*. Authoritative on what the engine can do, which is its value: the rows it produced were each checked against Stride's wrapper first, and `SweepCast`, `ContinuousDetectionMode`, `SolverSubStep`, `PoseGravity` with a per-body toggle, and `ApplyImpulse` are all exposed. Several demos are engine-internal (`CustomVoxelCollidableDemo`, `SolverContactEnumerationDemo`, `SimpleSelfContainedDemo`) and out of scope for a Stride example. |
| `stride/samples/Physics/PhysicsSample` | 2026-08-11 | Bullet-based, so none of its code carries over to Bepu. Scanned for transferable *ideas* only. Most - raycasting, triggers, impulse on keypress, character - are already covered by rows below or by existing toolkit examples. It contributed the reset-out-of-bounds idea. |

## Backlog

### Shapes

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|

### Physics

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Box2D contact and sensor events | Beginners | Idea | in-repo | Listed as high priority in [Example18 IMPROVEMENTS.md](code-only/Example18_Box2DPhysics/IMPROVEMENTS.md). Would need the Box2D wrapper extraction below first. |
| A tour of the collider shapes | Getting Started | Idea | BepuSample `Colliders.sdscene` | One of each primitive collider dropped side by side. **Not claimed by the Bepu plan** - its source scene is one of those that crash on launch (see that plan's appendix), so it was never harvested. Building it procedurally sidesteps the crash entirely. Would also be the natural place to demonstrate that `Size` means a full extent for box-like shapes and a *radius* for round ones ([ARCHITECTURE.md](../ARCHITECTURE.md) item 1). |
| Convex hull vs mesh collider | Beginners | Agreed | BepuSample `Convex And Mesh Collider.sdscene` | Claimed by the Bepu plan as `Example25_MeshColliders` (Tier 2). Its source scene also crashes, so expect to tune by eye. |
| Material properties: friction, restitution, damping | Beginners | Agreed | BepuSample `Material Properties.sdscene`; bepuphysics2 `FrictionDemo.cs`, `BouncinessDemo.cs` | Claimed by the Bepu plan as `Example24_PhysicsMaterials` (Tier 2), which rates it the highest value-per-line on its list. |
| Bepu constraints - servo vs motor vs limit | Beginners | **Done** | BepuSample `Constraint.sdscene`, `Cube Mixer.sdscene` | Built as [`Example15_Constraint_Motors`](code-only/Example15_Constraint_Motors). Findings written up in [bepu-constraints.md](../docs/manual/physics-extensions/bepu-constraints.md). |
| A rope from a chain of constraints | Intermediate | **Done** | bepuphysics2 `RopeStabilityDemo.cs` (primary); BepuSample `Ropes.sdscene` | Built as [`Example15_Constraint_Rope`](code-only/Example15_Constraint_Rope), following the upstream demo rather than the Stride sample - `DistanceLimit` with zero lever arms, not ball sockets plus swing limits. |
| Contact events - reacting to collisions | Beginners | Agreed | BepuSample `Components/Utils/CollisionComponent.cs`, `TriggerUsageComponent.cs` | Claimed by the Bepu plan as `Example16_TriggerZones` (Tier 2), which merges contact events and sensor volumes into one example - see also the Interaction row. |
| Slow motion and time control | Beginners | Agreed | BepuSample `Components/Utils/TimeControlComponent.cs` | Claimed by the Bepu plan as `Example26_TimeControl` (Tier 2): `TimeScale`, `Enabled` for pause, and live `PoseGravity` changes. |
| Soft bodies | Advanced | Idea | Bepu repo, `Stride.BepuPhysics.Soft` project, `S2.Softs.sdscene` | Confirmed ported: `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics.Soft` exists in Stride, and has no example anywhere. Old repo is the only sample source - BepuSample dropped it. |
| Cube Mixer | - | Declined | BepuSample, `Assets/Shared/Scenes/Cube Mixer.sdscene` | Not a separate example: the Bepu plan consumed this scene as a source for `Example15_Constraint_Motors` (the mixer blade is the hinge-plus-motor demonstration). |
| One body, many shapes - compound colliders | Beginners | Idea | bepuphysics2, `CompoundDemo.cs` | `CompoundCollider` is central to Stride's Bepu API - every helper builds one - yet nothing explains why, or how to assemble a multi-shape body deliberately. |
| Continuous collision detection for fast movers | Intermediate | Idea | bepuphysics2, `ContinuousCollisionDetectionDemo.cs` | The classic bullet-through-thin-wall problem. Confirmed exposed: `BodyComponent.ContinuousDetectionMode` offers Discrete, Passive and Continuous, and the difference is dramatic and easy to show. The Bepu plan lists this demo as noticed but not yet on its backlog. |
| Solver substeps and stack stability | Intermediate | Idea | bepuphysics2, `SubsteppingDemo.cs`, `PyramidDemo.cs`, `ColosseumDemo.cs` | Why a tall stack wobbles and how substepping firms it up. Confirmed exposed: `BepuSimulation.SolverSubStep`, plus SoftStart settings. Practical for anyone whose piles jitter. |
| Custom gravity - bodies orbiting a planet | Intermediate | Idea | bepuphysics2, `PlanetDemo.cs`, `PerBodyGravityDemo.cs` | Visually striking and teaches per-frame force application. Confirmed exposed: `BepuSimulation.PoseGravity`, a per-body gravity toggle on `BodyComponent`, and `ApplyImpulse`. |
| Ragdoll from constraints | Advanced | Idea | bepuphysics2, `RagdollDemo.cs`, `RagdollTubeDemo.cs` | Builds directly on the constraints example, and is the most recognisable payoff for learning joints. |

### Rendering

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Visualising colliders for debugging | Beginners | Idea | Bepu repo, `Stride.BepuPhysics.DebugRender` project | The toolkit already has `ShowColliders()`; an example showing when and why to use it would be small and useful. |

### UI

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|

### Input

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Third-person camera following a physics body | Intermediate | Idea | Bepu repo, `Components/Camera/ThirdPersonCameraComponent.cs` | The toolkit has `Add3DCameraController` for free flight; following a moving body is a different problem. |

### Interaction

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Throwing objects from the camera | - | Declined | BepuSample `Components/Utils/ThrowerComponent.cs` | Not a separate example: the Bepu plan folds click-to-throw into `Example23_CubeFountain`, which drops that example's complexity from 8 to 6. |
| Gravity gun - pick up, hold and release a body | Intermediate | Agreed | BepuSample `Components/Utils/GravityGunComponent.cs` | Claimed by the Bepu plan as `Example15_Constraint_GravityGun` (Tier 2). Note it is constraint-driven - linear plus angular servo - which is why it joins the constraint family rather than being a raycast example. |
| Trigger volumes | Beginners | Agreed | BepuSample `Components/Utils/TriggerUsageComponent.cs` | Claimed by the Bepu plan as `Example16_TriggerZones` (Tier 2), together with contact events. The plan notes `Example17_SignalR` already contains a working `IContactEventHandler`, but nobody looking for collision detection would find it inside a networking example. |
| Overlap queries | Beginners | Agreed | BepuSample `Components/Utils/OverlapTesterComponent.cs` | Claimed by the Bepu plan as `Example14_ShapeQueries` (Tier 2), family-grouped with `Example14_Raycast`. |
| Shape sweep queries | Intermediate | Agreed | bepuphysics2, `SweepDemo.cs` | Same example as overlap above - `Example14_ShapeQueries`. Confirmed exposed: `BepuSimulation.SweepCast` and `SweepCastPenetrating`, both taking a `CollisionMask`. The plan also adds `RayCastPenetrating` to the existing `Example14_Raycast` alongside it. |

### Audio

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Impact sounds driven by contact force | Intermediate | Idea | derived | Not in the Bepu repo, but its contact events expose impact force, and Audio currently has no example at all. Would combine contact events with audio playback. |

### Gameplay

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Reset a body that falls out of bounds | Getting Started | Idea | PhysicsSample, `AutoResetRigidBody.cs` | Teleport a body back to its start once it drops below a threshold. Tiny, and a pattern every prototype needs. Bullet code, so port the idea rather than the script. |
| Character controller | Intermediate | **Done** | in-repo | Already covered by [`Example20_BepuFirstPersonCharacter`](code-only/Example20_BepuFirstPersonCharacter), which drives Stride's `CharacterComponent` from a `FirstPersonControllerComponent` and its processor. The third-person camera row under Input is the remaining gap, not the controller itself. |
| Vehicle with wheels, engine and gears | Advanced | Idea | Bepu repo, `Components/Car/`, `Car/CarEngine.cs`, `CarEngineGear.cs`, `6.Cars.sdscene` | The largest item here. Good candidate for a composite example that merges constraints, input and camera work. |

### Performance

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Spawning many bodies continuously | Intermediate | Agreed | BepuSample `Cube Fountain.sdscene`, `Components/Utils/SpawnerComponent.cs` | Claimed by the Bepu plan as `Example23_CubeFountain` (Tier 1, next after the two built). Spawns from the **physics clock** via `ISimulationUpdate` with a fractional accumulator, so the rate holds under variable FPS - and cross-links to Example22 for instancing rather than re-teaching it. |

### Integration

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|
| Navigation and pathfinding over physics geometry | Advanced | Idea | Bepu repo, `Stride.BepuPhysics.Navigation` project, `8.Navigation.sdscene` | Confirmed ported, and larger than expected: Stride has `RecastNavigationComponent`, `RecastPhysicsNavigationComponent`, `BepuNavigationBoundingBoxComponent` plus build and pathfinding settings. A whole Recast navmesh feature with no example. |

### Scripts

| Idea | Target level | Status | Source | Notes |
|---|---|---|---|---|

## Toolkit gaps spotted while collecting

Not examples - toolkit API observations that came out of reading the sample projects. Recorded here
so they are not lost; move them if a toolkit backlog is ever created.

> [!NOTE]
> A toolkit backlog now exists for one kind of observation: [`ARCHITECTURE.md`](../ARCHITECTURE.md)
> in the repository root collects **API-design friction** — cases where the shape of an API, rather
> than a bug in it, is what trips people up. Prefer it for that; the table below remains the home for
> "this helper is missing" and "this helper is duplicated".

| Item | Verdict | Notes |
|---|---|---|
| Gamepad input helpers | **Worth adopting** | BepuSample's `Extensions/InputManagerExtensions.cs` has eleven methods the toolkit has no equivalent for: button state, left/right thumbsticks and left/right triggers, each with a per-pad overload and an `...Any` variant that polls every connected pad, with dead-zone handling. The toolkit has no input extensions at all, and `Scripts/Basic3DCameraController.cs` hand-rolls this against `Input.DefaultGamePad.State` - so adopting them would fill a public gap *and* simplify existing toolkit code. |
| `LogicDirectionToWorldDirection` | Already covered | The toolkit has both overloads, including the `upVector` one the sample uses (`Engine/CameraComponentExtensions.cs`). Nothing to do. |
| `GetComponentInChildren<T>` | Already covered, but duplicated | The toolkit defines it **twice in the same namespace** - `Engine/EntityExtensions.cs` and `Engine/EntitySearchExtensions.cs`, the latter with an optional `includeDisabled`. Both are callable as `entity.GetComponentInChildren<T>()`; C# picks the one without optional parameters, so it compiles, but which of two different implementations you get rests on a subtle overload rule. Worth consolidating. |
| `GetWorldPos` / `GetWorldRot` | Low value | One-liners over `WorldMatrix`; `Engine/TransformExtensions.cs` covers the substantial transform helpers already. Only worth adding if a naming convention for world-space accessors is wanted. |

## Housekeeping

Work on the examples themselves rather than new examples.

| Task | Status | Source | Notes |
|---|---|---|---|
| Document the `---example-metadata` block in the contributing guide | Idea | in-repo | [The guide](../docs/contributing/examples/index.md) only describes the `*.csproj` properties the console launcher reads - `ExampleTitle`, `ExampleOrder`, `ExampleEnabled`, `ExampleCategory` - and never mentions the YAML block. Someone following it end to end produces an example with no metadata block at all, which is the likely reason so few have one. |
| Audit which example folders are missing an `---example-metadata` block, and add it | Idea | in-repo | 14 of 63 `code-only/` folders currently have one; see the snapshot above. Worth doing after the guide is fixed, so the gap stops growing. |
| Decide whether the two metadata systems should converge | Agreed | in-repo | **Direction already settled** - Decision 6 of the [Bepu plan](code-only/PLAN_Bepu_Examples.md): new examples drop `ExampleTitle`/`ExampleOrder`/`ExampleEnabled`/`ExampleCategory` from the `.csproj` and treat the `---example-metadata` block as the single source of truth, which `Example19` and `Example21/22` already do. What is left is applying it to existing examples and pointing the launchers at the manifest (Phase 2 of the MetadataGenerator plan). |
| Reconcile the category list | Idea | in-repo | `Scripts` is used by an example but is not one of the categories listed in `code-only/README.md`. Either document it or re-categorise the example. |
| Extract the reusable Box2D wrapper into a library | Idea | in-repo | Scaffolding and goals are already written up in [README_PENDING_LIBRARY.md](code-only/Example18_Box2DPhysics/Box2DPhysics/README_PENDING_LIBRARY.md); the files are deliberately kept free of Stride types so the move is mechanical. |
