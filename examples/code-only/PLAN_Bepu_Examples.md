# Plan: New Bepu Physics code-only examples

Source: `D:\Projects\GitHub\stride\samples\Physics\BepuSample`. Originally scoped to the three
scenes that run (`Cube Fountain`, `Ropes`, `Constraint`); after reviewing the remaining scenes and
every utility component, the harvest list grew — see [Build list](#build-list).

**How this doc works:** [Decisions](#decisions) are settled and I build to them.
[Open questions](#open-questions) have `**Your answer:**` placeholders. Answer them, and I move
them up into Decisions.

**Status:** all questions answered except Q1 (one naming call). Ready to build in the
[stated order](#build-order) — Q1 only affects example #4, which is not first.

---

## Decisions

Settled. No need to re-read unless you want to change one.

### Naming and structure

1. **Naming** — numbers may repeat; the part after the underscore must be unique and descriptive.
2. **Family-grouped numbering is preferred** where a genuine family exists; sequential (24+)
   otherwise. Not critical — it may all be reorganised later.
3. **No `Bepu` prefix.** Bepu is the default physics engine; only non-default engines get named
   (`_BulletPhysics`, `_Jitter2Physics`, `_Box2DPhysics`). Existing
   `Example20_BepuFirstPersonCharacter` predates this and stays.
4. **Family membership is by domain, not by shared word.** Physics examples do not join rendering
   families. Specifically: physics material properties do *not* join `Example01_Material`
   (rendering materials), and `MeshCollider` does *not* join `Example01_Basic3DScene_MeshLine`
   (line rendering via vertex buffers). Both are false friends.
5. **Cube Fountain is not an Example15** — spawning/simulation-tick topic, not a constraint topic.
6. **No csproj categorisation.** Drop `ExampleTitle` / `ExampleOrder` / `ExampleEnabled` /
   `ExampleCategory` from new `.csproj` files — the `---example-metadata` block in `Program.cs` is
   the single source of truth. Matches `Example19_Jitter2Physics_Constraints` and `Example21/22`.

### Scope

7. **Motors get a standalone example**, not folded into `Example15_Constraint` (already ~590 lines
   and unfocused).
8. **Gravity gun gets its own example** (`Example15_Constraint_GravityGun`) — it is servo-constraint
   driven, so it joins the constraint family. Click-to-throw stays in the fountain; that drops the
   fountain from complexity 8 to 6.
9. **Trigger zones get a standalone example** despite `Example17_SignalR` already containing a
   working `IContactEventHandler` — nobody searching for collision detection will find it inside a
   networking example.
10. **`Example14_Raycast` gets `RayCastPenetrating` added now** — small addition to a shipped
    example, not a new project.
11. **Rope chain-builder stays local to its example** (a separate `.cs` beside `Program.cs`, as
    Example02/18/20 already do). Promote to `src/Stride.CommunityToolkit.Bepu` later only if useful.
12. **Complexity is rated honestly.** If an example grows, it gets the higher level rather than
    being trimmed to fit a label. Scale from `README.md`: 1-2 Getting Started, 3-5 Beginners,
    6-8 Intermediate, 9-10 Advanced.
13. **I write the Czech metadata** (`cs` title/description), as in Example19/22.
14. **Port code, not scenes.** The playground's `.sdscene`/`.sdprefab`/compositor assets are never
    used; code-only examples build everything procedurally (`SetupBase3DScene`, `AddSkybox`,
    `Create3DPrimitive`). This also means the Colliders-scene crash (see
    [Appendix](#appendix-playground-crash)) cannot follow the code across.

---

## Build list

### Tier 1

#### 1. `Example15_Constraint_Motors` · Beginners · complexity 5

The **servo vs. motor vs. limit** distinction, which `Example15_Constraint` never teaches (it shows
only servos and limits). A *servo* drives toward a target pose and stops; a *motor* drives a target
*velocity* continuously; a *limit* just clamps range.

- `HingeConstraintComponent` — two bodies share a rotation axis (the mixer blade pivot).
- `OneBodyAngularMotorConstraintComponent` — spins the blade continuously.
- `BallSocketMotorConstraintComponent` — motorised pendulum.
- `SwingLimitConstraintComponent` standalone.
- Runtime toggle: `constraint.Enabled = !constraint.Enabled` on a key press.

*Source:* `Constraint.sdscene`, `Cube Mixer.sdscene`, `ConstraintToggleComponent`,
`ConstraintEditorComponent`.

#### 2. `Example15_Constraint_Rope` · Intermediate · complexity 7

There is no rope type — a rope is a runtime-built chain of small dynamic bodies.

- Segment count derived from anchor distance ÷ segment length.
- Each consecutive pair linked by `BallSocketConstraintComponent` (pins the touching ends) **plus**
  `SwingLimitConstraintComponent` (caps bend angle — what stops it folding back on itself).
- Two end ball-sockets tie the first/last segment to the anchors.
- One anchor `Kinematic = true` (fixed pivot), the other dynamic, so it visibly sags and swings.
- Chain-building lives in a local `RopeBuilder.cs`.

*Source:* `Ropes.sdscene`, `RopeSpawnerComponent`, `RopePart.sdprefab`.

#### 3. `Example23_CubeFountain` · Intermediate · complexity 6

Continuous spawner driven by the **physics clock**, not the render loop.

- `ISimulationUpdate.SimulationUpdate(BepuSimulation, float timeStep)` — spawn rate in cubes/sec,
  with a fractional-time accumulator so the rate is stable under variable FPS, plus a hard count cap.
- Spawned cubes use the master/instance split for rendering.
- Click-to-throw — spawn a cube and launch it along the camera forward vector.

*Source:* `Cube Fountain.sdscene`, `SpawnerComponent`, `_Spawner`, `ThrowerComponent`.
*Cross-link, don't re-teach:* `Example02_GiveMeACube_SimulationUpdate` covers `ISimulationUpdate`;
`Example22` covers instancing.

### Tier 2

#### 4. `Example24_PhysicsMaterials` · Beginners · complexity 3 — *name pending Q1*

**Highest value-per-line on the list.** Pure parameter tuning, no new API, very visual: drop
identical shapes onto surfaces differing only in `FrictionCoefficient` (1 vs 1000),
`MaximumRecoveryVelocity` (0.001 = dead, 1000 = bouncy), `SpringFrequency`, `SpringDampingRatio`.
Answers "how do I make something bouncy or slippery?", which nothing currently does.

*Source:* `Material Properties.sdscene`.

#### 5. `Example25_MeshColliders` · Beginners · complexity 4

`MeshCollider` and `ConvexHullCollider` — colliders derived from real geometry. Every existing
toolkit example uses primitive colliders only, so this whole area is untaught. The natural place to
explain the convex-vs-concave tradeoff and why a mesh collider must usually be static.

*Source:* `Convex And Mesh Collider.sdscene`.

#### 6. `Example14_ShapeQueries` · Intermediate · complexity 5

Queries that are *not* raycasts: `simulation.Overlap(shape, pose, buffer)` (what is inside this box
right now) and `simulation.SweepCast(shape, pose, velocity, maxT, out hit)` (what would this shape
hit if moved). Family-grouped with `Example14_Raycast`.

*Source:* `OverlapTesterComponent`.

#### 7. `Example16_TriggerZones` · Beginners · complexity 5

Sensor volumes and collision callbacks: `Trigger` with `OnEnter`/`OnLeave`, `NoContactResponse` for
pass-through volumes, and `IContactHandler.OnStartedTouching/OnStoppedTouching`. Family-grouped with
`Example16_CollisionGroup`/`_CollisionLayer` — those cover *what* collides, this covers *reacting*
to it.

*Source:* `TriggerUsageComponent`, `CollisionComponent`.

#### 8. `Example15_Constraint_GravityGun` · Intermediate · complexity 7

Grab, hold, move and release a body using constraints — raycast to pick, then attach
`OneBodyLinearServoConstraintComponent` + `OneBodyAngularServoConstraintComponent`, tracking a local
grab point and mouse-wheel distance; release removes them. Builds on `Example14_Raycast` for picking.

*Source:* `GravityGunComponent`.

#### 9. `Example26_TimeControl` · Beginners · complexity 3

Runtime control of the simulation itself: `TimeScale` (slow-mo/fast-forward), `Enabled` (pause), and
live `PoseGravity` changes.

*Source:* `TimeControlComponent`.

### Edit to an existing example

- **`Example14_Raycast`** — add `RayCastPenetrating` (multi-hit raycast passing through objects),
  with a metadata `concepts` entry to match.

### Deliberately not harvested

- **`BasicCameraControllerComponent`, `GameProfilerComponent`, `FindAndAttachCameraComponent`,
  `SceneSelectorComponent`, `SceneDescriptionComponent`** — the toolkit already has equivalents
  (`AddProfiler`, `SetupBase3DScene`, `DebugTextPrinter`). Nothing to teach.

### Build order

Tier 1 cheapest-to-most-complex so the pattern is proven before the big one, then Tier 2:

`Constraint_Motors` → `Constraint_Rope` → `CubeFountain` → `PhysicsMaterials` → `MeshColliders` →
`ShapeQueries` → `TriggerZones` → `Constraint_GravityGun` → `TimeControl`, with the
`Example14_Raycast` edit folded in alongside `ShapeQueries` (same family, same session).

---

## Open questions

**Q1. Final name for example #4.** You asked whether it should sit near `Example01_Material` — I
recommend not (see Decision 4: that is rendering materials, this is contact response). But you did
surface a real problem with my name: `MaterialProperties` collides with `Example01_Material`
regardless of number, and "material" in Stride overwhelmingly means the rendering kind. Options:

| Name | Notes |
|---|---|
| `Example24_PhysicsMaterials` | My pick — explicit, no collision, matches how engines usually label it |
| `Example24_FrictionAndBounce` | Most descriptive of what you actually see on screen; least jargon |
| `Example24_MaterialProperties` | Keeps the playground's scene name; ambiguous against Example01 |

- **Your answer:** I agree with your pick `Example24_PhysicsMaterials`.

---

## Answered

Kept for the record; all folded into Decisions above.

- **Gravity gun placement** → own example, family-grouped (Decision 8).
- **Tier 2 numbering** → family-grouped where possible, sequential otherwise (Decision 2).
- **Trigger zones standalone?** → yes (Decision 9).
- **`RayCastPenetrating`** → add now (Decision 10).
- **Build order** → agreed as proposed (see [Build order](#build-order)).
- **Czech metadata** → I write it (Decision 13).
- **Anything else to teach** → list is good for now; revisit after a pass over the Bepu repository.

---

## Appendix: playground crash

Context for why we port code rather than reuse scenes, and a parked lead if you ever chase it.

`Colliders`, and reportedly `Convex And Mesh Collider` / `Material Properties` / `Cube Mixer`,
crash on launch. `Cube Fountain` / `Ropes` / `Constraint` run fine.

- Signature: exception `0x0000087a` (DXGI facility), faulting in `KERNELBASE.dll`, in **both**
  Debug and Release. Release only hides the diagnostics; it does not fix it.
- Debug additionally reports the proximate cause: a comparison-filtering sampler (hardware PCF
  shadow sampling) bound at slot 3 where the pixel shader expects default filtering, in the Opaque
  pass after the shadow-map and GBuffer passes. A real render-state bug, not scene authoring.
- Bisection ruled out: capsule models, and the instanced entities. **Parked lead:** the
  `Models/Cylinder` asset is used by three entities in Colliders and appears in none of the three
  working scenes, while `CubeModel`/`GeoSphere`/`Capsule` all appear in working scenes and are
  therefore cleared. One run would test it (delete the three cylinder entities).
- Proper diagnosis needs a RenderDoc/PIX frame capture, and it is likely an upstream Stride issue.
- Caution if bisecting by hand: edit whole entities via `RootParts` + `Parts`, and verify each run
  actually reached asset compilation — a malformed `.sdscene` fails the build and looks exactly like
  "no crash".

**Note for #5 and #4:** their source scenes (`Convex And Mesh Collider`, `Material Properties`) are
among the crashing ones, so I cannot run them for reference. The concepts are collider/parameter
config and port fine to code — but expect me to iterate on tuning values by eye rather than matching
the playground exactly.
