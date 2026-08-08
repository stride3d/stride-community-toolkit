# Plan: New Bepu Physics code-only examples

Source: `D:\Projects\GitHub\stride\samples\Physics\BepuSample`. Originally scoped to the three
scenes that run (`Cube Fountain`, `Ropes`, `Constraint`); after reviewing the remaining scenes and
every utility component, the harvest list grew — see [Example specs](#example-specs).

**How this doc works:** [Decisions](#decisions) are settled and I build to them.
[Open questions](#open-questions) have `**Your answer:**` placeholders. Answer them, and I move
them up into Decisions.

---

## Decisions

These are answered and locked in. No need to re-read unless you want to change one.

1. **Naming** — numbers may repeat; the part after the underscore must be unique and descriptive.
   Examples that extend an existing topic reuse its number (`Example15_Constraint_*`); genuinely
   new topics take the next free number (23+).
2. **No `Bepu` prefix in names.** Bepu is the default physics engine; only non-default engines get
   named (`_BulletPhysics`, `_Jitter2Physics`, `_Box2DPhysics`). Existing
   `Example20_BepuFirstPersonCharacter` predates this and stays as-is.
3. **Cube Fountain is not an Example15.** It is a spawning/simulation-tick topic, not a constraint
   topic, so it gets its own number.
4. **No csproj categorisation.** Drop `ExampleTitle` / `ExampleOrder` / `ExampleEnabled` /
   `ExampleCategory` from new `.csproj` files — the `---example-metadata` block in `Program.cs` is
   the single source of truth. Matches `Example19_Jitter2Physics_Constraints` and `Example21/22`.
5. **Motors get a standalone example**, not folded into `Example15_Constraint` (already ~590 lines
   and unfocused).
6. **Rope chain-builder stays local to its example** (a separate `.cs` file beside `Program.cs`,
   which the repo already does in Example02/18/20). Promote to
   `src/Stride.CommunityToolkit.Bepu` later only if it proves useful.
7. **Complexity is rated honestly.** If an example grows, it gets the higher level rather than
   being trimmed to fit a label. Scale from `README.md`: 1-2 Getting Started, 3-5 Beginners,
   6-8 Intermediate, 9-10 Advanced.
8. **Port code, not scenes.** The playground's `.sdscene`/`.sdprefab`/compositor assets are never
   used; code-only examples build everything procedurally (`SetupBase3DScene`, `AddSkybox`,
   `Create3DPrimitive`). This also means the Colliders-scene crash (see
   [Appendix](#appendix-playground-crash)) cannot follow the code across.

---

## Example specs

### Tier 1 — agreed, ready to build

#### 1. `Example23_CubeFountain` · Intermediate · complexity 8

Continuous spawner driven by the **physics clock**, not the render loop.

- `ISimulationUpdate.SimulationUpdate(BepuSimulation, float timeStep)` — spawn rate in cubes/sec,
  with a fractional-time accumulator so the rate is stable under variable FPS, plus a hard count cap.
- Spawned cubes use the master/instance split for rendering.
- **Click-to-throw** — spawn a cube and launch it along the camera forward vector (~10 lines).
- **Gravity gun** — raycast-pick a body, then hold it by attaching
  `OneBodyLinearServoConstraintComponent` + `OneBodyAngularServoConstraintComponent`, tracking a
  local grab point and mouse-wheel distance; release removes the constraints. This is the bulk of
  the complexity (see Q1).

*Source:* `Cube Fountain.sdscene`, `SpawnerComponent`, `_Spawner`, `ThrowerComponent`,
`GravityGunComponent`.
*Cross-link, don't re-teach:* `Example02_GiveMeACube_SimulationUpdate` already covers
`ISimulationUpdate`; `Example22` covers instancing; `Example14_Raycast` covers picking.

#### 2. `Example15_Constraint_Rope` · Intermediate · complexity 7

There is no rope type — a rope is a runtime-built chain of small dynamic bodies.

- Segment count derived from anchor distance ÷ segment length.
- Each consecutive pair linked by `BallSocketConstraintComponent` (pins the touching ends) **plus**
  `SwingLimitConstraintComponent` (caps bend angle — what stops it folding back on itself).
- Two end ball-sockets tie the first/last segment to the anchors.
- One anchor `Kinematic = true` (fixed pivot), the other dynamic, so it visibly sags and swings.
- Chain-building lives in a local `RopeBuilder.cs`.

*Source:* `Ropes.sdscene`, `RopeSpawnerComponent`, `RopePart.sdprefab`.

#### 3. `Example15_Constraint_Motors` · Beginners · complexity 5

The **servo vs. motor vs. limit** distinction, which `Example15_Constraint` never teaches (it only
shows servos and limits).

- `HingeConstraintComponent` — two bodies share a rotation axis (the mixer blade pivot).
- `OneBodyAngularMotorConstraintComponent` — drives angular *velocity* continuously (spins the blade).
- `BallSocketMotorConstraintComponent` — motorised pendulum.
- `SwingLimitConstraintComponent` standalone.
- Runtime toggle: `constraint.Enabled = !constraint.Enabled` on a key press.

*Source:* `Constraint.sdscene`, `Cube Mixer.sdscene`, `ConstraintToggleComponent`,
`ConstraintEditorComponent`.

### Tier 2 — harvested from the remaining scenes (new, pending your OK)

Verified as genuine gaps: I grepped `examples/code-only` for each API and found nothing, except
where noted.

#### 4. `Example24_MaterialProperties` · Beginners · complexity 3

**My pick for highest value-per-line.** Pure parameter tuning, no new API, very visual: drop
identical shapes onto surfaces that differ only in `FrictionCoefficient` (1 vs 1000),
`MaximumRecoveryVelocity` (0.001 = dead, 1000 = bouncy), `SpringFrequency`, `SpringDampingRatio`.
Answers "how do I make something bouncy/slippery?", which nothing currently does.

*Source:* `Material Properties.sdscene`.

#### 5. `Example25_MeshColliders` · Beginners · complexity 4

`MeshCollider` and `ConvexHullCollider` — colliders derived from real geometry. Every existing
toolkit example uses primitive colliders only, so the whole "non-primitive collider" area is
untaught. Also the natural place to explain the convex-vs-concave tradeoff and why a mesh collider
must usually be static.

*Source:* `Convex And Mesh Collider.sdscene`.

#### 6. `Example26_ShapeQueries` · Intermediate · complexity 5

Queries that are *not* raycasts: `simulation.Overlap(shape, pose, buffer)` (what is inside this
box right now) and `simulation.SweepCast(shape, pose, velocity, maxT, out hit)` (what would this
shape hit if moved). `Example14_Raycast` is ray-only.

*Source:* `OverlapTesterComponent`.

#### 7. `Example27_TimeControl` · Beginners · complexity 3

Runtime control of the simulation itself: `TimeScale` (slow-mo/fast-forward), `Enabled` (pause),
and live `PoseGravity` changes. Small, and a natural companion to the fountain.

*Source:* `TimeControlComponent`.

#### 8. `Example28_TriggerZones` · Beginners · complexity 5

Sensor volumes and collision callbacks: `Trigger` with `OnEnter`/`OnLeave`, `NoContactResponse`
for pass-through volumes, and `IContactHandler.OnStartedTouching/OnStoppedTouching`.

*Partial overlap:* contact handling does exist in the repo — `Example17_SignalR`'s
`ContactTriggerHandler` — but buried inside a networking example, so it teaches nobody looking for
physics triggers. See Q3.

*Source:* `TriggerUsageComponent`, `CollisionComponent`.

### Not a new example

- **`RayCastPenetrating`** (multi-hit raycast that passes through objects) — real gap, but belongs
  as an addition to `Example14_Raycast`, not a separate project. See Q4.
- **`BasicCameraControllerComponent`, `GameProfilerComponent`, `FindAndAttachCameraComponent`,
  `SceneSelectorComponent`, `SceneDescriptionComponent`** — the toolkit already has equivalents
  (`AddProfiler`, `SetupBase3DScene`, `DebugTextPrinter`). Nothing to harvest.

---

## Open questions

**Q1. Gravity gun — in the fountain, or its own example?**
Your earlier answer said put throw *and* gravity gun in the fountain, so that is what spec #1
currently says. I want to flag the cost once, then drop it: the sample's `GravityGunComponent` is
~155 lines and works by attaching and driving two servo constraints on the grabbed body. Bundled
in, it roughly doubles the example and shifts its centre of gravity away from "spawning on the
physics tick". Split out it would be `Example28_GravityGun` (Intermediate, ~7) and is a strong
topic on its own — object manipulation via servo constraints, building on Example14's raycast.
Throw is genuinely cheap either way and stays in the fountain regardless.
- **Your answer:** Yes, let's move Gravity Gun to its own example. By the way, family-grouped sequencing is preferable, but not essential becuase it might be all reoganized later.

**Q2. Tier 2 numbering.** I assigned 24-28 sequentially. Alternative: put the collision-flavoured
ones in existing families instead — `Example16_TriggerZones` (beside CollisionGroup/CollisionLayer)
and `Example14_Raycast_ShapeQueries` (beside Raycast). That groups better in generated docs but
repeats numbers more heavily. Sequential, family-grouped, or a mix?
- **Your answer:** Family-grouped sequencing is preferable where possible otherwise sequential numbering is fine.

**Q3. Does `Example28_TriggerZones` earn its place** given `Example17_SignalR` already contains a
working `IContactEventHandler`? My view: yes — nobody looking for "how do I detect a collision"
will find it inside a SignalR example. But you know the docs/discoverability story better.
- **Your answer:** Yes, I agree that it should be a standalone example. The SignalR example is not discoverable for users looking for collision detection.

**Q4. Extend `Example14_Raycast` with `RayCastPenetrating`?** It is a small addition to a shipped
example rather than new work, so it touches a file that already works. Do it now, later, or not at
all?
- **Your answer:** Yes, do it now. It is a small addition and will be useful for users looking for multi-hit raycasting.

**Q5. Build order.** I would do Tier 1 first (Motors → Rope → Fountain, cheapest to most complex,
so the pattern is proven before the big one), then Tier 2 starting with Material Properties. Any
preference, or examples you want prioritised for a talk/article?
- **Your answer:** I agree with your proposed build orer. Starting with the simpler exa

**Q6. Czech descriptions.** The metadata block carries `en` + `cs` for title and description.
Should I write the Czech myself (as in Example19/22), or leave `cs` blank for you to fill?
- **Your answer:** You can write the Czech descriptions yourself. I trust your judgment and it will save time.

**Q7. Anything else to teach** that was not in these scenes — from your own toolkit backlog or
questions you see people asking?
- **Your answer:** For now this is a good list. We can go crazy once I point you to Bepu repositry and you can see what is there. But for now, let's focus on these examples.

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
  actually reached asset compilation — a malformed `.sdscene` fails the build and looks exactly
  like "no crash".
