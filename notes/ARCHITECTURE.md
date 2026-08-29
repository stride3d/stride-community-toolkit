# Architecture notes

A running list of API-design observations for the Stride Community Toolkit: places where the shape of
the API, rather than a bug in it, is what trips people up.

This is a **backlog of observations, not a decision record.** Nothing here is agreed or scheduled.
Items get added when something is noticed in passing - usually while writing an example, which is
where API friction shows up first - and removed when they are resolved or rejected.

The toolkit is in Preview, so breaking changes are on the table where they buy a cleaner long-term
API. Each item below states the impact if changed.

> [!NOTE]
> Add to this file when you notice friction, even if you are not going to act on it. An observation
> written down is worth more than one rediscovered three months later.

---

## 1. `Size` means different things for different primitives

**Observation.** `Primitive3DEntityOptions.Size` is a single `Vector3?` whose interpretation changes
per primitive type. Box-like shapes read it as a **full extent**; round shapes read `X` as a
**radius**, which is a half extent.

```csharp
game.Create3DPrimitive(PrimitiveModelType.Cube,   new() { Size = new Vector3(1f) });   // 1 unit across
game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { Size = new Vector3(1f) });   // 2 units across
```

**Impact.** Silent and visual-only. Passing a diameter to a sphere produces a model at twice the
intended size with no error. It is worst when a collider is supplied by hand, because the mesh and
collider then disagree and objects appear to pass through one another - encountered while writing
`Example15_Constraint_Rope`.

The generated mesh and generated collider *do* read the value identically, so the toolkit is
internally consistent. The trap is entirely in the caller's expectation.

**How other engines avoid it.** None of the major engines expose one polymorphic size; each names the
property after the convention it uses. Godot went as far as making a breaking rename for exactly this
reason.

| Engine | Box | Sphere | Capsule |
|---|---|---|---|
| Unity | `BoxCollider.size` (full) | `SphereCollider.radius` | `radius` + `height` |
| Godot 4 | `BoxShape3D.size` (full) | `SphereShape3D.radius` | `radius` + `height` |
| Unreal | `UBoxComponent::BoxExtent` (**half**) | `USphereComponent::SphereRadius` | `CapsuleRadius` + `CapsuleHalfHeight` |

Two things worth copying. First, **the name carries the semantics** - Unreal says `Extent` and
`HalfHeight` precisely because they are halves. Second, Godot 3 called the box field `extents` and
meant half-extents; Godot 4 renamed it to `size` meaning full extents, a deliberate breaking change
to remove this ambiguity. That is the same choice facing this API.

*(Engine comparisons above are from general knowledge of those APIs, not verified against their
sources in this repository.)*

**Options.**

1. **Document only** - done for now: the XML docs on `Size` list every primitive's convention.
   Zero impact, but the trap remains.
2. **Normalise to bounding-box semantics.** `Size` always means the axis-aligned box the shape fits
   in, so a sphere of `Size = (1,1,1)` is one unit across. Intuitive and matches Unity's unit
   primitives. Breaking: every call site passing a radius silently halves. Loud but mechanical.
3. **Per-primitive option types** - `SphereOptions { Radius }`, `CubeOptions { Size }`. Impossible to
   misread, but multiplies the options surface and complicates the generic
   `Create3DPrimitive(type, options)` entry point.
4. **Make the primitive itself a closed set that carries its own dimensions.** Replace the
   `PrimitiveModelType` enum plus separate `Size` with one argument per shape, each naming exactly
   the parameters that shape needs. A sphere then cannot be handed a `Vector3` at all, and the entry
   point stays single and generic.

   ```csharp
   public abstract record Primitive
   {
       private Primitive() { }                                 // closed: no cases from outside
       public sealed record Sphere(float Radius) : Primitive;
       public sealed record Cube(Vector3 Size) : Primitive;
       public sealed record Capsule(float Radius, float Length) : Primitive;
   }

   game.Create3DPrimitive(new Primitive.Sphere(0.5f), options);
   ```

   Note this fixes the ambiguity **structurally** rather than by documentation, and it subsumes
   item 6: the mesh and collider switches both match over the same closed set, so a shape added to
   one and not the other is caught at the switch rather than by a test.

   Works on C# 14 / net10.0 today. Breaking, and broad - `PrimitiveModelType` appears across nearly
   every example.

   **C# 15 union types would be the same design, stated more directly**, and would add compiler-
   enforced exhaustiveness so a missing case is a build error rather than a `_ => throw`:

   ```csharp
   public union Primitive(Sphere, Cube, Capsule);
   ```

   Not adoptable yet: unions need .NET 11 Preview 2 and `<LangVersion>preview</LangVersion>`, and are
   early preview. Requiring a preview language version of every consumer is a high price for a
   shipped package. The record hierarchy above is the same shape and upgrades to a union later
   without changing call sites much, so waiting for unions is not a reason to defer the decision.
   (Union syntax and requirements verified against the C# 15 union types announcement; the toolkit
   has not been built against .NET 11.)

---

## 2. `IncludeCollider = false` leaves a half-configured body

**Observation.** Setting `Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a
`BodyComponent`, holding a `CompoundCollider` with no shapes. That never attaches to the simulation,
so the entity ends up with a physics component that does nothing.

**Impact.** Two different intentions collide on one flag: "no physics at all" and "physics, but I
will supply the collider myself". The first is served by the non-physics `Create3DPrimitive`
overload; the second works but leaves an inert component if the caller forgets to add shapes.

**Options.** Rename to something intent-revealing (`SuppliesOwnCollider`); or validate and warn when
an attached body has no shapes; or leave as-is and rely on the documented gotcha.

---

## 3. Two `Create3DPrimitive` overloads separated only by their options type

**Observation.** There is a Bepu overload taking `Bepu3DPhysicsOptions` and a plain one taking
`Primitive3DEntityOptions`. With both namespaces imported, a bare `new()` cannot choose between them
and the call fails with `CS0121`.

**Impact.** A confusing compiler error for a common call shape, resolved only by naming the options
type explicitly. Overload resolution is carrying meaning that the method name could carry instead.

**Options.** Distinct names for the physics-creating helper; or a required explicit options argument.
Both are breaking, both are mechanical.

---

## 4. Mass is reachable only by abandoning the generated collider

**Observation.** Mass lives on the collider shape (`ColliderBase.Mass`), not on the body or on the
options. To set it, a caller must switch off `IncludeCollider` and build the whole
`CompoundCollider` by hand - which re-exposes item 1, since the hand-built collider must match the
mesh convention.

**Impact.** Setting one common property costs the entire convenience of the helper.
`Example15_Constraint_Rope` does this, and it is the longest part of `RopeBuilder`.

**Options.** Surface `Mass` (or `Density`) on `Bepu3DPhysicsOptions` and apply it to the generated
shapes. Additive, not breaking.

---

## 5. Fluent return values are inconsistent

**Observation.** The contributor guidance asks extensions to return the modified instance where
natural, but several do not - `SetupBase3DScene` returns `void`, while `AddSkybox` returns an entity.

**Impact.** Small, but it makes chaining unpredictable, so callers stop trying.

**Options.** Return `Game` from the scene-setup helpers. Additive and non-breaking, since a discarded
return value compiles unchanged.

---

## 6. No test asserts that generated meshes and colliders agree

**Observation.** For every `PrimitiveModelType`, the procedural model and the Bepu collider derive
their dimensions from the same `Size` in two separate switch statements
(`Procedural3DModelBuilder` and `EntityExtensions`). Nothing enforces that they stay in step.

**Impact.** They currently agree. A future primitive added to one switch but not the other, or with a
different convention, would produce a mesh that does not match its collider - a defect that is
invisible until something falls through the world.

**Options.** A test that, for each primitive type and a fixed `Size`, asserts the model bounds and the
collider bounds match. Cheap, and it pins down the convention that item 1 documents.

Superseded if item 1 option 4 is taken: matching both switches over a closed set of primitive records
moves this from a test to a compile-time check.

---

## 7. Deriving a collider requires an entity and a model it never reads

**Observation.** `Get3DColliderShape(type, size)` is private. The only public route to it is
`AddBepu3DPhysics`, which throws unless the entity already carries a `ModelComponent` - a guard only,
since it derives the collider from the primitive type and reads nothing out of the mesh.

**Impact.** Combined with the shared-model gap (now agreed work - `TODO.md` §3), sharing a model
becomes a four-step dance that needs a comment to explain itself:

```csharp
var entity = new Entity("Item") { new ModelComponent(sharedModel) };   // model attached only to satisfy the guard
entity.AddBepu3DPhysics(type, options);
entity.Remove<ModelComponent>();                                       // ...and immediately taken off again
```

The collider for a shape and a size is a pure function of two values. Nothing about it needs an
entity, a component, or a mesh.

**Options.** Expose it - `public static ColliderBase ColliderFor(PrimitiveModelType, Vector3?)` —
which is additive and also serves callers who want a collider without any of the helper machinery;
and/or drop the `ModelComponent` guard from `AddBepu3DPhysics`, which is breaking only for code
relying on the throw.

---

## 8. Instancing needs three separate registrations and fails silently if one is missed

**Observation.** Drawing an instanced crowd requires, in three different places: an
`InstancingRenderFeature` in the graphics compositor (`AddInstancingSupport`), a master entity in the
scene carrying a `ModelComponent` and an `InstancingComponent`, and - for the buffered variant - a
renderer in the compositor (`AddInstancingBufferUpload`).

**Impact.** Omit the first and nothing is drawn, with no exception, no warning and no log line. The
code-built compositor wires up transform, skinning, material and lighting but not instancing, so this
catches everyone who does not start from a Game Studio project. Hit while writing `Example22`.

The split also has a lifetime consequence worth knowing: `AddInstancingBufferUpload` registers with
the **compositor**, not the scene, so it outlives any scene swap. Creating one instancing object per
scene leaves every previous one registered and being uploaded every frame.

**Options.** A single helper that sets up the render feature, the master and the upload renderer
together; and/or have the instancing processor warn once if it finds an `InstancingComponent` in a
scene whose compositor has no `InstancingRenderFeature`.

---

## 9. Toolkit instancing does not notice entities leaving the scene

**Observation.** Stride's own `InstanceComponent` unregisters itself from its master when its entity
leaves the scene, because the component goes with it. `EntityInstancing` and `BufferedEntityInstancing`
keep their own list and have no such hook.

**Impact.** An entity removed from the scene stays registered, so the master keeps reading its
transform and drawing it - ghosts of objects that are no longer there. The caller has to remember to
call `Clear()` or remove the instance explicitly, and to do it *before* detaching the entities.
Encountered while adding runtime shape switching to `Example01_Basic2DScene_StressPile`.

**Options.** Subscribe to the entity's scene changes and unregister automatically, matching
`InstanceComponent`'s behaviour; or keep the manual model and make the asymmetry loud in the XML docs
of both types.

---

## 10. `DisplayPosition` is a general screen-corner concept living in `Scripts.Utilities`

**Observation.** `DisplayPosition` names the four window corners plus `None` and `Custom`. It began as
an implementation detail of `DebugOverlay`, which is why it sits in
`Stride.CommunityToolkit.Scripts.Utilities`. It now has three consumers - `DebugOverlay`,
`Basic3DCameraController`, and `EntityTextComponent.ScreenAnchor` - and only the first two are scripts.

**Impact.** Anything that wants to anchor to a corner has to reach into a `Scripts.Utilities`
namespace that has nothing to do with what it is doing. Adding `ScreenAnchor` to
`EntityTextComponent` meant every consumer of that property picks up
`using Stride.CommunityToolkit.Scripts.Utilities;` for an enum naming a corner of the screen, which
reads as a mistake at the call site. Hit while migrating `Example_CubicleCalamity`'s HUD.

The `None` and `Custom` members compound it. They exist for `DebugOverlay`'s needs - opting out
entirely, and deferring to a separate `CustomPosition` property - and mean nothing to a component that
has its own visibility flag and its own explicit-position mode. `EntityTextRenderer` currently maps
both to the top-left, which is a silent fallback for values that should not have been offerable.

**Options.** Move the enum to a neutral namespace (`Stride.CommunityToolkit.Engine`, or a
`Stride.CommunityToolkit.Rendering` shared home) and leave a type forward or a rename note, since the
toolkit is in Preview and breaking changes are acceptable; and/or split the four real corners into a
`ScreenCorner` enum, leaving `DisplayPosition` as `DebugOverlay`'s own type with its `None` and
`Custom` extras. The second is more churn but stops components offering values they cannot honour.

**The same enum cannot say "centre", either.** `TextPositionMode` offers corners and explicit pixels
and nothing else, so Cubicle Calamity's game-over banner needs a four-line `ScreenCentreTextScript`
that recomputes its position every frame. Centring on screen is not an exotic request for a HUD, and
a component that can anchor to four corners but not the middle will be asked for it again. Whatever
shape the corner enum settles into should carry it.

---

## 11. There is no screen-anchored orientation gizmo

**Observation.** Every code-only example eventually faces "where the hell is X" with no editor
viewport to answer it. `AddGroundGizmo` draws the world axes at the origin, which is a world-space
answer to a screen-space question: it either buries itself in the scene's content or sits at a
misleading offset once the camera moves.

**Impact.** Low individually, constant in aggregate. It is the reason the demo game places two
world-space markers by hand, and the reason several examples start with the camera pointed somewhere
arbitrary until someone tunes it.

**Options.** An axis widget pinned to a screen corner, the way editor viewports do it - a small
overlay renderer reading the camera's rotation, drawn last, ignoring depth. Additive, and it
generalises to every example rather than being tuned per scene. It wants whatever the corner-anchor
enum in item 10 becomes.

---

## 12. `LetterMeshFactory` bakes its style into a shared segment grid

**Observation.** Stroke width, glyph width, spacing and depth are constants. The stroke in particular
is woven into the shared segment grid - `TopY`, `MiddleY` and the rest all derive from it - so making
it configurable is not a matter of exposing one number. It means passing a metrics or style object
down into every glyph builder.

**Impact.** None today; there are two consumers and both want the current look. It is recorded
because the shape of the fix is decided by how the type is written now, not later: the bar-built
glyphs would follow a style object automatically, but the hand-sketched polygons - V, X, Y, Z, K, the
R leg, the Q tail, D's chamfers - carry tuned constants that must either scale with the stroke or be
re-authored.

**Options.** A `LetterStyle` record threaded through the factory, with the existing area-invariant
tests generalised to run per style over a few stroke values. Additive. Worth doing once a third
consumer appears and actually wants a different weight - not before, since the re-authoring cost is
real and speculative styling would fix the wrong constants.

---

## 13. `DebugTextDropdown` offers two rendering paths and only one respects the overlay

**Observation.** The type can be rendered two ways: `Draw(debugTextSystem)` at its own `Position`, or
`GetLines()` handed to a `DebugOverlay` section. Both are public and neither is marked as the
default.

**Impact.** The standalone path silently ignores the overlay's reposition and hide keys, so a
dropdown drawn that way stays put and stays visible while everything else on screen moves or
disappears. The spawn-menu example fell into exactly this before being switched to `GetLines()`. The
two paths also disagree about who owns layout - `Position` is meaningful in one and dead in the
other.

**Options.** Keep both but make the asymmetry loud in the XML docs of `Draw` and `Position`, naming
`GetLines()` as the path that participates in the overlay; or have `Draw` register with the overlay
when one exists. Both additive. Deleting `Draw` was considered and rejected - standalone rendering is
wanted for cases where no overlay is in play.

---

## 18. `GetComponentInChildren<T>()` resolves to two different searches depending on an optional argument

**Observation.** Two extension methods share the name and differ in what they search:

- `EntityExtensions.GetComponentInChildren<T>(this Entity)` - a recursive depth-first search that
  **checks the entity itself first** (`entity.OfType<T>()`), despite the name and despite the
  toolkit's own `…AndSelf` convention for that.
- `EntitySearchExtensions.GetComponentInChildren<T>(this Entity, bool includeDisabled = false)` —
  the breadth-first, children-only search inherited from the original StrideToolkit, constrained to
  `ActivableEntityComponent` and skipping disabled components. Its parameterless sibling from upstream
  was renamed `GetComponentInChildrenBFS` to avoid clashing with the first method.

C# prefers the applicable candidate that needs no default-argument substitution, so for any
`T : ActivableEntityComponent`:

```csharp
entity.GetComponentInChildren<Foo>();       // DFS, includes self, ignores Enabled
entity.GetComponentInChildren<Foo>(false);  // BFS, children only, enabled only
```

Same name, same intent at the call site, different traversal, different scope and different
filtering - decided by whether a literal `false` was typed. Noticed while auditing the StrideToolkit
port (August 2026); the two methods have coexisted since the merge.

**Impact.** Silent. Neither call errors, and in a shallow hierarchy with no disabled components both
return the same thing, so the divergence only shows up later - usually as "why did it find the
component on the parent" or "why did it find a disabled one". The `BFS` suffix also leaks an
implementation detail into a name whose sibling carries no `DFS`.

**Options.**

1. Rename the `EntityExtensions` method to say what it does - `GetComponentInDescendantsAndSelf<T>()`
   or similar - and give the `BFS` method its original name back. Breaking, mechanical, and the
   names then match the `Descendants` / `AndSelf` vocabulary the search class already uses.
2. Keep the names and make the DFS variant children-only (drop the self check), so at least the
   scope agrees; the traversal-order and `Enabled` differences remain.
3. Document only: an XML `<remarks>` on each pointing at the other. Zero impact, trap remains.

---

# Upstream (engine) observations

The items above are about the toolkit's own API. The ones below are about what the **engine** makes
the toolkit do for code-only projects. They are recorded here because they decide how much of the
toolkit's code-only layer could ever move into Stride, and which toolkit packages exist only to work
around an engine gap. Each one is something a maintainer could take upstream; none is scheduled.

Context: [Stride issue #1295](https://github.com/stride3d/stride/issues/1295) and
[discussion #1253](https://github.com/stride3d/stride/discussions/1253), which is where the code-only
approach was first proposed and where the toolkit came from.

---

## 14. The asset compiler is required only to copy engine shader sources into `data/db`

**Observation.** A code-only project has no assets of its own, yet it cannot run without the
`Stride.AssetCompiler` build step. The `default.bundle` that step emits (~2.4 MB per project) contains
every engine `.sdsl` file as `/shaders/<Name>.sdsl` plus `StrideDefaultFont`, `StrideDebugSpriteFont`
and the splash screen from `Stride.Engine.sdpkg` - nothing project-specific. At runtime
`ShaderSourceManager` (`Stride.Shaders.Compilers`) resolves shader sources only through the
`IVirtualFileProvider` backed by that database; there is no embedded-resource fallback.

Verified on 4.4.0-beta5 by renaming `data/` under a built `Example01_Basic3DScene` and running it:

```
[EffectCompilerCache]: Warning: Failed to load effect bytecode from application cache: Unable to find shader [LambertianPrefilteringSHNoComputePass1]
[Scheduler]: Error: Unexpected exception while executing a micro-thread.. System.InvalidOperationException: Shader LambertianPrefilteringSHNoComputePass1 could not be found
   at Stride.Shaders.Compilers.ShaderLoaderBase.LoadExternalBuffer(...)
   at Stride.Shaders.Compilers.SDSL.ShaderMixer.MergeSDSL(...)
```

The window opens and stays blank. Note the stack: 4.4's SDSL-to-SPIR-V compiler (`ShaderMixer`,
`SpirvBuilder`) is doing the runtime compilation, so runtime shader compilation is already
cross-platform. The build-time step is not compiling shaders; it is copying their source text.

**Impact.** This single dependency is the reason `Stride.CommunityToolkit.Windows` and
`Stride.CommunityToolkit.Linux` exist - their `.csproj` descriptions say so - and why
`create-project.md` needs two packages instead of one. It also brings the per-project asset build,
the `_StrideCheckVisualCRuntime` registry check, the `obj/` path assumptions that bit the file-based
app example, and a platform-specific package name (`.Windows`) for something that is not about
Windows.

**Options.**

1. Embed the engine `.sdsl` files as assembly resources (or ship a prebuilt `db` as NuGet content in
   `Stride.Rendering` / `Stride.Engine`) and let `ShaderSourceManager` fall back to them when the
   database has no `/shaders/<Name>.sdsl`. The existing `/path` hot-reload lookup keeps working when
   a database is present. Cost is ~2 MB of resources in the engine assemblies. This removes
   `Stride.AssetCompiler` from every code-only project and lets both toolkit platform packages be
   retired. Same treatment for `StrideDefaultFont`, which is the other root asset the bundle carries.
2. Until then, in the toolkit: move the `Stride.AssetCompiler` reference
   (`IncludeAssets="build;buildTransitive"`) into the core package, or rename the platform packages
   to something that says what they do. Editor projects already reference the asset compiler, so a
   duplicate reference unifies rather than conflicts. Breaking for anyone who references `.Windows`
   by name; mechanical.

---

## 15. `SceneSystem` defaults to an empty `GraphicsCompositor` that draws nothing

**Observation.** `SceneSystem`'s constructor sets `GraphicsCompositor = new GraphicsCompositor()`
(`SceneSystem.cs:40`) and `LoadContent` only replaces it when `InitialGraphicsCompositorUrl` points
at an existing asset. With no `GameSettings` asset - the code-only case - the empty compositor stays,
so `new Game().Run()` opens a window and renders nothing, with no warning. The engine already has
`GraphicsCompositorHelper.CreateDefault(...)` in `Stride.Rendering.Compositing`; the toolkit's
`AddGraphicsCompositor()` is a one-line call to it.

**Impact.** The first thing every code-only program has to know is that the compositor exists and
must be set, which is exactly the kind of engine internals code-only is meant to defer. The empty
root `Scene` is already created for this case (issue #1290 was resolved), so the compositor is the one
remaining piece that does not get a usable default.

**Options.** In `SceneSystem.LoadContent`, when `InitialGraphicsCompositorUrl` is null and the
compositor is still the empty default, assign `GraphicsCompositorHelper.CreateDefault(enablePostEffects: false)`.
Editor-created projects always set the URL through `GameSettings`, so they are unaffected. The
toolkit's `AddGraphicsCompositor` would remain as the opt-in for post effects and a clear colour.

---

## 16. There is no post-load hook on `Game`, so `Run(start:)` has to schedule a script

**Observation.** The root scene exists only after `Run()` -> `PrepareContext()` -> `LoadContent()`.
The one instance-level signal the engine offers, `Game.GameStarted`, fires at the end of
`Initialize()` (`Game.cs:403`), *before* `LoadContent`, so the scene is not there yet. The toolkit's
`GameExtensions.Run(start, update)` works around this by adding a microthread to
`game.Script.Scheduler` before calling the engine's `Run`; the microthread runs `start` on the first
frame and loops `update` on `NextFrame()`. It is ~20 lines and uses only public API.

**Impact.** Two things worth knowing, one reassuring and one a gap:

- Running `start` inside a microthread does **not** hide its exceptions. `Scheduler.PropagateExceptions`
  defaults to `true`; a faulting microthread that nothing awaits is rethrown with
  `ExceptionDispatchInfo` from `ScriptSystem.Update`, `GameBase` logs it as
  `[Game]: Error: Unexpected exception` and rethrows, and it escapes `game.Run(...)`. Verified on
  4.4.0-beta5 with a probe that throws from `Start` and, separately, from `Update`: both runs exited
  with a non-zero code and the exception was catchable around `game.Run`. So a typo in `Start` fails
  `dotnet run` loudly, as it should; the message is just logged three times on the way out
  (scheduler, script system, game). The one exception is the live-scripting debugger:
  `GameDebuggerTarget` sets `PropagateExceptions = false`, so under it a faulting `Start` is logged
  and the game keeps running.
- The toolkit side of this has been reshaped (August 2026). `Run` used to offer two overloads told
  apart only by delegate parameter type, `Action<Scene>` versus `Action<Game>`, with `GameContext`
  as the first parameter. Verified against those signatures: an untyped lambda
  `game.Run(start: s => { })` failed with **CS0121** (ambiguous), and an `async` lambda passed to
  the `Action<Scene>` overload compiled silently as `async void`, so the update loop started before
  `start` finished and any exception inside it bypassed the game. The `Action<Game>` overload also
  handed back nothing the caller did not already hold (they call `game.Run` on it) and its `update`
  dropped `GameTime`. The current shape keys everything on `Scene` and pairs sync/async by return
  type, which C# resolves cleanly for method groups and lambdas alike:

  ```csharp
  Run(this Game game, Action<Scene>? start = null, Action<Scene, GameTime>? update = null, GameContext? context = null)
  Run(this Game game, Func<Scene, Task> start,     Action<Scene, GameTime>? update = null, GameContext? context = null)
  ```

  `update` is deliberately synchronous: an async `start` that runs its own
  `while (true) { ...; await game.Script.NextFrame(); }` already *is* an async update loop (the
  `AsyncScript.Execute` idiom), so an async `update` would double the overloads for nothing.
  `context` moved last because nobody passes it positionally and it is the least-used parameter -
  it exists for embedding in a host control, a fixed initial size, or the SDL/headless backends.

**Options.** Upstream, either move `Run(start, update)` into `Game` as instance overloads - it
transplants as-is - or add an instance event raised after `LoadContent` (the composable form
suggested in discussion #1253) and make `Run(start:)` sugar over it.

---

## 17. A consumer-facing MSBuild SDK would hide the package list, but matters less if 14 lands

**Observation.** The engine repository has `sources/sdk/Stride.Build.Sdk`, but its README says it is
consumed by direct `Import` from the source tree and the `Sdk="Stride.Build.Sdk"` NuGet mode is not
used. There is no `Stride.Sdk` a code-only project could name in `<Project Sdk="...">` or, in a
file-based app, `#:sdk Stride.Sdk`; the file-based example instead carries four `#:` lines and a
commented-out `obj/` workaround.

**Impact.** Low on its own. The `#:` lines are short, and most of what an SDK would hide is the
asset-compiler wiring from item 14. If that lands, the remaining boilerplate is one or two package
references, which is not worth an SDK.

**Options.** Defer until item 14 is decided. If the asset compiler stays required, a thin
`Stride.Sdk` that composes `Microsoft.NET.Sdk`, references the engine packages and the asset
compiler, and sets the host RID (what `examples/Directory.Build.props` does by hand) would make the
code-only front door `#:sdk Stride.Sdk` followed by `using var game = new Game();`.
