# Copilot for Stride Community Toolkit

These repository instructions guide GitHub Copilot (and similar AI assistants) to help develop the Stride Community Toolkit solution.

## Quick editing & display guidance (short)

- When returning an edit to a single paragraph or section, output only that updated selection (do not include the rest of the file). If context is helpful, add a small window (up to ~10 lines before and after). Include the file path and exact line range for the change when known; if not known, ask the reviewer for the specific lines.

## Status & stability

- The Stride Community Toolkit is currently in **Preview**.
- Public APIs, namespaces, behaviors, and package layout may change without backward-compatibility guarantees until the first stable release.
- Breaking-change suggestions are acceptable because the toolkit is in Preview, not beta; prefer cleaner long-term APIs when they improve correctness, naming, maintainability, or usability, and document migration impact.
- Clearly call out breaking-change suggestions and explain the migration impact.
- Many extensions and helpers originated from community sources (forum posts, samples, gists, experimental repos). Some code paths have not yet been fully reviewed, optimized, or documented.
- Treat sparsely documented or unusual APIs as provisional. Prefer improving them (tests, XML docs, consistency) before broad reuse.
- Prefer well-documented, core, and recently updated toolkit helpers over unverified examples.
- A public member with no caller is **not** dead: the examples lag the library. Propose a removal with its call count rather than deleting it in passing.

## Project overview

- A collection of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/), targeting **.NET 10**. Every project single-targets `net10.0` via the root `Directory.Build.props`; nothing multi-targets today.
- Provides library projects, code-only examples, snippet examples, and documentation to simplify Stride game development.
- F# and VB.NET examples are showcase-only (not the primary focus).
- Uses the latest Stride version with nullable reference types enabled.
- Includes a Blazor example project; when a web UI is present, prefer Blazor-centric solutions over Razor Pages or ASP.NET Core MVC.
- Designed to integrate with a regular Stride Game Studio project; code-only examples intentionally avoid relying on editor UI or assets to demonstrate pure programmatic setup.

## Repository structure (summary)

- `src/`: Core toolkit libraries
  - **Stride.CommunityToolkit**: Core library
    - `Audio/`: runtime sound without assets: procedural and `.wav` sound sources, a listener that follows an entity
    - `Collections/`: small extensions on dictionaries, lists, enumerables and `Random` over collections
    - `Engine/`: Game, Scene, Entity, Transform, Camera and Prefab extensions; the `SceneSetup` family; `CreatePrimitive`
    - `Mathematics/`: easing and tweens, `MathUtilEx`, `RaySegment`, extensions on `Random`
    - `Rendering/`: rendering utilities, by subfolder: `Compositing/` (compositor helpers, render-texture cameras), `Gizmos/`, `Instancing/`, `Overlay/` (`TextOverlay`, `KeyMenu`, `EntityInfoRenderer`), `ProceduralModels/` (`Primitive2D`, `Primitive3D` and their models), `Text/` (screen and world text components, `TextMesh` lettering), `Utilities/` (`MeshBuilder`, `EarClipping`, `TextureCanvas`); the root holds textures, materials and `DisplayScale`
    - `Scripts/`: reusable script components: the camera controllers and the profiler
  - **Stride.CommunityToolkit.Bepu**: Bepu physics integration (primary)
  - **Stride.CommunityToolkit.Box2D**: Box2D.NET integration for 2D physics (not packed yet)
  - **Stride.CommunityToolkit.Bullet**: Bullet physics integration (legacy / transitional, pending deprecation; maintained, not extended)
  - **Stride.CommunityToolkit.Charts**: 2D and 3D charts drawn with ShapeBatch (not packed yet)
  - **Stride.CommunityToolkit.DebugShapes**: Debug visualization tools
  - **Stride.CommunityToolkit.Effects**: toolkit pieces that ship a shader of their own: post-processing colour transforms and GPU picking
  - **Stride.CommunityToolkit.ImGui**: ImGui integration
  - **Stride.CommunityToolkit.ImGuiNet**: ImGui.NET bindings and helpers
  - **Stride.CommunityToolkit.Linux**: the asset compiler's Linux native libraries, for code-only projects on Linux
  - **Stride.CommunityToolkit.Shapes**: `ShapeBatch`, the signed-distance-field shape renderer behind the HUD, panel and gizmo examples (see `docs/manual/rendering/shape-batch.md`)
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: empty since preview.67, a forwarder to the core (which holds `WindowsDpiManager` and brings in the asset compiler); to be unlisted
- `examples/`: Code-only and snippet example projects (C#, F#, VB): `examples/code-only/` and `examples/snippets/`
- `benchmarks/`: BenchmarkDotNet-based performance tests
- `tests/`: Unit and regression test projects (xUnit, targeting net10.0), the gold scenes (`Stride.CommunityToolkit.GoldScenes`), and `tests/gold/`, the golden images that `build/gold-images.cs` compares those scenes against
- `tools/`: Supporting tools, not shipped as packages
  - **Stride.CommunityToolkit.Examples**: Console example launcher
  - **Stride.CommunityToolkit.Examples.Launcher**: Avalonia example launcher
  - **Stride.CommunityToolkit.Examples.MetadataGenerator**: Generates `examples-manifest.json` from example metadata
- `build/`: Repository scripts, all .NET file-based apps run with `dotnet run --file build/<name>.cs`: `pack-local.cs` (local dev NuGet packages), `capture-screenshots.cs` (the documentation screenshots), `gold-images.cs` (rendering regression against `tests/gold`)
- `docs/`: DocFX sources (manuals, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation, and this instruction file

Solutions: `Stride.CommunityToolkit.slnx` contains everything, 130 projects of which 107 are
examples; `Stride.CommunityToolkit.Core.slnf` is a solution filter loading only libraries, tests and
tools, because the examples slow IDE load noticeably. Build the filter for library work and the full
solution before reporting an API change done, since only it compiles the examples.
See [Building the Toolkit](../docs/contributing/toolkit/building.md).

## Build configuration lives outside the .csproj files

> [!IMPORTANT]
> Project files are deliberately sparse. If a `.csproj` appears to be missing `TargetFramework`,
> `Nullable`, `ImplicitUsings`, a Stride version, or package metadata, that is not an oversight —
> it is supplied by one of the files below. Check these before "fixing" a project file, and prefer
> changing the shared file over adding a local override.

| File | Applies to | Supplies |
|---|---|---|
| `Directory.Build.props` (root) | Every project in the repository | `TargetFramework` (net10.0), `ImplicitUsings`, `Nullable`, `StrideVersion` |
| `src/CommonSettings.props` | Library projects, imported explicitly | Package metadata: version, licence, authors, icon, readme, SourceLink |
| `build/HostRuntime.props` | Imported by the `Directory.Build.props` under `examples/`, `tests/` and `tools/`; never by `src/` | Host-only `RuntimeIdentifier`, `SelfContained`, output-path settings that keep those builds small |
| `build/HostRuntime.targets` | Imported by the `Directory.Build.targets` under the same three folders | Strips package XML documentation and native symbol files from build output |

Two rules when editing these:

- **MSBuild imports only the *nearest* `Directory.Build.props` / `.targets`.** A nested file must
  explicitly `Import` the one above it, or the parent's settings are silently lost. The files under
  `examples/`, `tests/` and `tools/` do this; preserve it.
- **`StrideVersion` is the single place the Stride version is set.** Reference it as
  `Version="$(StrideVersion)"` in a `PackageReference` rather than hard-coding a version.

## Stride engine context (quick reminders)

- ECS: Entities aggregate Components (Transform, Model, Camera, Rigidbody, Script, etc.).
- Entities must be added to a Scene graph to be processed.
- Physics: Prefer Bepu components; keep Bullet only for transition/testing. Avoid mixing both on the same entity.
- Core components commonly manipulated: Transform (position, rotation, scale), Camera, Rigidbody, Script logic.
- **`TransformComponent.UseTRS`** (default `true`) makes the engine rebuild `LocalMatrix` from
  `Position`, `Rotation` and `Scale` every frame; with it `false` the assigned matrix is used and the
  three properties are ignored. The toolkit's transform helpers throw when it is off. It is
  `[DataMemberIgnore]`, so a prefab instance always comes back with it on.
- **Every component needs a public parameterless constructor** (analyser `STRDIAG010`), for the
  serializer and Game Studio. A class with no constructor gets one from the compiler; declaring any
  other constructor removes it, so declare the parameterless one too. Several components of one type
  on one entity need `[AllowMultipleComponents]`; `LightComponent`, `ScriptComponent` and the toolkit's
  text components opt in.
- `MathUtil.ZeroTolerance` is `1e-6f` and `MathUtil.IsZero` tests against it; use them instead of a
  literal tolerance.
- **Losing window focus pauses the whole audio engine.** `AudioSystem` hooks `Game.Deactivated` and
  calls `AudioEngine.PauseAudio()`, and `SoundInstance.Play` then returns *silently* — no exception,
  no queued playback. The click that brings the window back is delivered before the matching
  `ResumeAudio()`, so the first sound after refocusing is dropped and every later one works, which
  reads as a flaky sound bug. Check `AudioEngine.State` and resume before playing if it matters.
- A `SoundInstance` plays one sound at a time: replaying one that is still sounding cuts it off. For
  effects that can overlap, keep several instances and cycle through them.
- **Front faces wind clockwise as seen by the camera** — the Direct3D convention
  (`RasterizerStateDescription.DefaultFrontFaceCounterClockwise` is `false`). Do not trust prose that
  says counter-clockwise; read the raster state. Winding a closed mesh the wrong way does not draw it
  wrong, it draws it **inside-out**: the near faces cull away and the camera looks into a hollow
  shell whose parallax moves unnaturally. When generating geometry, assert the winding in a test —
  the geometric right-hand normal of each triangle must point *opposite* its outward lighting normal.
  Assert the lighting normals too: a face normal from a cross product with its edges in the wrong
  order points into the solid, and the mesh looks right until it is lit.

### Bepu transform ownership (frequent source of confusion)

- The transform sync is **one-way: physics → `TransformComponent`**. Assigning `Entity.Transform.Position` on an entity that has an attached body moves the mesh only; the body stays where the simulation put it.
- To move a body deliberately: `Teleport(...)` jumps it without checking collisions, while scripted motion that should collide belongs on a body with `Kinematic = true`.
- Prefer setting `LinearVelocity` on a kinematic body over calling `SetTargetPose(...)` from a per-frame `Update`. `SetTargetPose` derives its velocity from `(target - position) / FixedTimeStep`, which assumes exactly one physics tick per call. When the frame rate falls below the physics rate two ticks run on that velocity, the body overshoots the target, the next correction overshoots further, and it diverges to `NaN` within seconds. `SetTargetPose` is safe when the caller runs once per physics tick (`ISimulationUpdate.SimulationUpdate`) or when the frame rate is pinned to the physics rate; otherwise integrate a velocity you compute yourself, and add a small proportional pull towards the ideal position to stop drift.
- Setting `LinearVelocity` does **not** wake a sleeping body — set `Awake = true` as well, or the motion silently stops once the body sleeps.
- `ISimulationUpdate.SimulationUpdate` can run **before** `StartupScript.Start` / `SyncScript.Start`. The component is registered with the simulation as soon as it enters the scene, while `Start` waits its turn in the script system. Resolve component references lazily inside `SimulationUpdate` (`_body ??= Entity.Get<BodyComponent>()`) rather than caching them in `Start`.
- Only **awake** bodies are synced back to their transform. A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform writes suddenly appear to work while the collider is left behind. A "moving mesh with no collisions" almost always means this.
- `CreatePrimitive` attaches a body through the physics engine the scene setup chose (`PhysicsSetupBase.Of(game).AddBody`); the core alone attaches nothing. `PrimitiveEntityOptions.Body` picks `Dynamic` (the default), `Static`, `Kinematic` or `None`; `None` is how a purely visual entity is made. A ground helper sets the mode itself and builds from a copy of the options it is given.
- Spawning thousands of bodies on a **perfectly regular lattice**, exactly touching, degenerates Bepu's broad-phase tree and kills the process with a `Stack overflow` in `Tree.Refit2WithCacheOptimization` within seconds. It is deterministic and not a threading problem. Jitter the spawn positions by a millimetre or space the bodies apart.
- Hull-backed shapes (`TriangularPrism`, `Cone`, `Teapot`, `Torus`) at a few thousand bodies can crash with an intermittent `AccessViolationException` inside the solver. The cause is **not known**; it appears in roughly one run in five, so do not treat a single clean run as evidence that anything fixed it.
- **Never zero *some* terms of `BodyInertia.InverseInertiaTensor` to lock individual axes** — in dense piles the solve diverges and the narrow phase allocates until the process dies (tens of GB in seconds). Zeroing the *whole* tensor is fine and is what Bepu's own character demo does; a full tensor is fine; a partly-zeroed one fails 3/3 at 20k bodies. Scale the terms down instead, as `Body2DComponent` does, or use a one-body constraint.

Full write-up: [Bepu: Who Owns the Transform?](../docs/manual/physics-extensions/bepu-transform-ownership.md).

### Bepu constraints (joints and motors)

- **A constraint does not stop the bodies it joins from colliding.** A joint built so its parts share space jams and never moves, which looks like a frozen scene rather than an error. Ball sockets make this easy to hit, because the joint forces the two anchor points to coincide — pin an arm's top to an anchor's *centre* and the arm is required to end up inside it. Put pivots in clear air.
- Constraints join **bodies**: both ends need a `BodyComponent`, so an immovable anchor is a kinematic body, never a `StaticComponent`.
- `MotorDamping` does **not** read back the value passed to the component's constructor — Bepu stores its reciprocal, so a component built with `0.02` reports `50`. Read the property to learn the real default before overriding it; copying the constructor argument makes the motor 2500x softer and it silently stops producing force.
- Motor names describe the joint they pair with, not what they drive. `BallSocketMotor` drives **linear** velocity at the socket point; for rotation use `OneBodyAngularMotor`, `AngularMotor`, or `AngularAxisMotor`.
- Most motor targets are a whole vector: `(0, speed, 0)` also demands *zero* rotation about X and Z. Use `AngularAxisMotorConstraintComponent` when only one axis should be driven.
- Disabling a motor stops it pushing but does not brake anything. Confirm the toggle by displaying the body's velocity, not by watching the object.

Full write-up: [Bepu: Why Isn't My Constraint Doing Anything?](../docs/manual/physics-extensions/bepu-constraints.md).

## Toolkit patterns
### Extension method pattern

```csharp
game.AddGraphicsCompositor()
    .AddCleanUIStage()
    .ConfigurePostEffects(fx => fx.Bloom.Enabled = true);
```
Guidelines:
- Return the modified instance (fluent chaining) where it’s natural; a helper that creates something returns the thing it created (`Add3DCameraController` returns the controller, `AddGizmo` the gizmo).
- Validate inputs early (`ArgumentNullException.ThrowIfNull`). A method that returns a lazy sequence (`yield`) validates in a non-iterator wrapper, so a null argument throws at the call and not at the first `foreach`.
- Where a method has a `ref`/`out` form for per-frame maths and a by-value twin, the `ref` form is the one implementation and the twin forwards to it and takes its documentation by `<inheritdoc cref="..."/>`.
- Avoid hiding heavy allocations or long-running work behind simple-sounding extension names.

### Scene setup and code-only scene creation

A scene is configured by a `SceneSetup` passed to `game.Run`: `SceneSetup3D` (camera, directional
light, ground, gradient skybox) or `SceneSetup2D`, with the physics engine chosen by the package in use
(`.UseBepu()`, `.UseBullet()`) and the rest as properties (`Profiler`, `TextOverlay`, `PostEffects`,
`Ground`, `Camera`). `SceneSetup.None` runs the game with nothing added.

```csharp
using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    var entity = game.CreatePrimitive(Primitive3D.Capsule(), new() { Material = game.CreateMaterial(Color.Gold) });

    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}
```

Shapes are closed record sets: `Primitive3D.Sphere(0.5f)`, `Primitive2D.Circle(0.5f)` and the other
factories name exactly the dimensions a shape has. `ProceduralModelBuilder` builds the mesh and each
physics package derives the collider by switching over the same set, so a shape added to one and
missed in the other is a compile error.

### Components in Game Studio

Every `EntityComponent` and script the toolkit ships must work when added in Game Studio, not only
from code: assembly registered for scanning (`Module.cs`), `[DataContract]`, a `[Display]` name and a
`[ComponentCategory]`, a public parameterless constructor (see the engine reminders above), `List<T>`
never arrays, no nullable value types, and a processor that runs in the editor and provisions its own
renderer on the owning compositor (`SceneRendererRegistration.Ensure`). The recipe, with the editor
traps, is `docs/contributing/toolkit/game-studio-components.md`; the shape and text components are
the worked examples. Verify against a package from `build/pack-local.cs`, not a `ProjectReference`.

## Coding Style & Conventions

- Use latest C# features (file-scoped namespaces, target-typed `new`, pattern matching, collection expressions, the `field` keyword, `params ReadOnlySpan<T>`, spans where beneficial, primary ctors where suitable). C# 14 extension blocks are **not** used yet: Visual Studio's IntelliSense for them is not ready, so extension methods stay in static classes.
- Keep nullable-reference warnings at zero.
- Public APIs: include complete XML docs (`<summary>`, `<param>`, `<returns>`, `<example>` when useful) including top level classes.
- Naming: `Stride.CommunityToolkit.<LibraryName>` for new libs; PascalCase for types and methods; camelCase for parameters.
- Terminology / capitalization: Use “Bepu” (capital B only) in identifiers and XML docs; never “BEPU” or “bepu”. Use “Bullet” (capital B) for Bullet physics.
- One public type per file; avoid unrelated multi-class files.
- Avoid `#region`; write self-explanatory code.
- Avoid partial classes unless auto-generated code is involved.
- Braces: an `if` body keeps its braces unless the statement is very short (`if (x) return;`). A `throw`, an assignment with a long right-hand side or a call spanning most of a line keeps them. The code base is inconsistent here because several people wrote it; do not strip braces as a line-saving move.
- Validation: prefer `ArgumentNullException.ThrowIfNull()` and provide meaningful exception messages for invalid states. Say what is wrong and what to do: "The transform is driven by its matrix; set UseTRS to true to use this method", not "Must use TRS".
- Suggestion preference order:
  1. Existing, reviewed toolkit extension/helper
  2. New small, composable extension (documented)
  3. Direct Stride API usage
  4. External snippet (must justify and ensure license compatibility)
- Performance:
  - Cache frequently used component references inside update loops.
  - **No LINQ, capturing lambdas or boxing on a path that can run every frame** (`Update`, a script loop, a renderer, a camera lookup people poll until it appears). Stride's `FastCollection` family has struct enumerators that LINQ boxes, and a lambda that captures a local allocates a closure per call. A nine-line `foreach` beats a one-line `FirstOrDefault` there; setup-time code (scene building, mesh generation) can stay compact.
  - Avoid per-frame allocations (consider pooling or struct patterns where appropriate).
  - Dispose GPU/graphics resources deterministically (`using` / `Dispose`).
- Threading: Mutations to the scene graph, entities, components, or graphics resources must occur on the main thread. `ScriptSystem.RunOnGameThread` and `RunOnGameThreadAsync` get there from any other thread.
- Physics: Do not combine Bepu and Bullet physics components on the same entity.
- Shaders (*.sdsl): see [Shaders](#shaders-sdsl) below. Nothing is regenerated by hand; a shader change is verified against the golden images.
- Experimental / provisional APIs: consider marking with an `[Experimental]` attribute (future) or note in the XML summary.
- Tests: Use xUnit under `tests/` targeting net10.0; keep deterministic and avoid relying on real-time frame counts. Rendering changes are verified with `build/gold-images.cs` against `tests/gold` (see [Screenshots and golden images](#screenshots-and-golden-images)). Internals are visible to the test project, so a private helper worth testing can be made `internal`.
- When a refactor is meant to change nothing, prove it: a geometry invariant test (counts, winding, bounds, normals) for a mesh function, the golden images for a renderer, the existing suite for the rest.

### Shaders (SDSL)

- The recipe for a package that ships a shader - folder, build reference, the engine's function libraries, dither, timing scope, effects - is [docs/contributing/toolkit/shaders.md](../docs/contributing/toolkit/shaders.md); this section is the compiler facts.
- Stride 4.4 compiles SDSL through its **SPIR-V** toolchain (`sources/shaders/` in the engine clone):
  SDSL to SPIR-V, then SPIRV-Cross to HLSL for Direct3D 11. The old HLSL-based compiler is gone.
- **Key classes are generated at build time** by a Roslyn source generator that the
  `Stride.Shaders.Compilers` package wires up for every `.sdsl`/`.sdfx` in the project. There is no
  `*.sdsl.cs` on disk and nothing to regenerate; do not add `<None Update>` / `LastGenOutput` blocks
  to a project file, they are a vestige of the old Visual Studio custom tool. `Stride.AssetCompiler`
  is what stages `Effects/` into the package.
- **Reserved words that are easy to trip over:** `linear` and `half` (interpolation modifier and a
  type) cannot be identifiers. The parse error points at the wrong line; look for these first.
- Integer streams are flat (`nointerpolation`) automatically; the keyword itself is parsed and
  ignored. `StructuredBuffer` and `Buffer<T>` can be read in the pixel stage. Put constants in an
  explicit `cbuffer` and resources in an `rgroup`; loose uniforms land in an implicit `Globals`
  block that the engine has marked as a future breaking change.
- Split a shader into mixins by concern (`ShapeShader : ShaderBase, ShapeDistance, ShapeColor` is
  the model): pure function libraries are callable qualified without inheriting them, which is
  how the engine ships `Math`, `ColorUtility` and `HSVUtils`.
- **Verify, do not infer.** The compiler writes every effect's per-stage HLSL and SPIR-V
  disassembly next to the game: `bin/.../cache/effects/<Effect>/<hash>_vs.hlsl`, `_ps.hlsl`,
  `.spvdis`. Read those for register counts, interpolation and buffer bindings.
- A shader refactor that should change nothing is proven with the golden images; a deliberate
  visual change is reviewed on the contact sheet and then re-baselined with `--update`.

## Modern C# / .NET guidance

- Prefer modern C# features when they improve clarity: file-scoped namespaces, pattern matching, collection expressions, raw string literals, target-typed `new`, `required`/`init`, and primary constructors where they reduce boilerplate without hiding behavior. A primary constructor is always public, so a class whose constructor is meant to stay `internal` keeps the explicit one.
- Use `var` only when the type is obvious from the right-hand side, required by anonymous types, or improves readability.
- Keep nullable reference types enabled and avoid the null-forgiving operator (`!`) unless the invariant is obvious or documented. Prefer nullable analysis attributes such as `[NotNullWhen]`, `[MemberNotNull]`, and `[MaybeNull]` for public contracts.
- Prefer `.editorconfig`, Roslyn analyzers, and project settings to enforce style and quality. Do not suppress analyzer warnings without a clear reason.
- Use `async`/`await` for I/O-bound work. Avoid sync-over-async. In library code, use `ConfigureAwait(false)` when a synchronization context is not required; in Blazor/UI code, preserve the UI context and use `InvokeAsync` when updating UI state.
- Use performance-oriented APIs such as `Span<T>`, `Memory<T>`, pooling, or unsafe code only when they clearly improve correctness or measured performance.
- Prefer specific exception types and meaningful error messages. Do not catch `Exception` broadly unless adding context and rethrowing or handling a known boundary.

## Documentation guidelines

- Docs are generated with DocFX from `docs/`.
- Update conceptual docs and XML comments when changing public APIs; an `xref` in the manual names an overload by its full signature, so a merged or re-signed overload breaks the link.
- New libraries: update navigation, TOC, and contributing guides (`docs/contributing/toolkit/library-project.md`).
- Provide concise, runnable examples that minimize boilerplate.
- Write manual pages as reference documentation, in the style of the Microsoft .NET docs:
  - State what a feature is, how to use it, and its limits. Use task-named headings ("Load textures",
    "Pick shapes"), short sentences, present tense and second person.
  - Prefer tables for parameters, members and comparisons, numbered steps for procedures, and DocFX
    alerts (`> [!NOTE]`, `> [!IMPORTANT]`, `> [!WARNING]`) for what goes wrong.
  - Leave out history, anecdotes and how a problem was found. Explain a concept when it teaches the
    reader something they need; keep the explanation short.
  - End with a **See also** list.
- Release notes entries are one or two factual sentences: what changed and how to use it.
- Keep `docs/release-notes/index.md` current: every change goes under the heading of the version in
  progress, in the categories of `.github/release.yml` (breaking changes first - a renamed or moved
  public type, a changed property type, a visual change a user would notice). Write the entry with the
  change, not at release time. A breaking or visible change gets its own `####` subsection with a
  Before / After / Note table, the format used since preview.67.

## Verification & provenance

- Imported code from external/community sources must have:
  - A compatible license (or original author permission).
  - Normalized naming/patterns to match toolkit style.
  - XML docs added or improved.
- Refactor legacy “static manager” patterns toward extension-based or instance-centric designs.
- Mark unclear logic or magic numbers with `// TODO:` plus an issue link.

## Working across into the Stride engine repository

Some toolkit limitations are really engine limitations, and a few toolkit features need a small
change in Stride to be possible at all.

- **Name the real cause.** When a problem traces to Stride rather than the toolkit, say so plainly
  instead of quietly working around it. A workaround that conceals an engine bug is more expensive
  later than the bug.
- **A minimal fix is welcome.** If a Stride source clone is available locally, locating the cause and
  proposing a surgical fix is in scope and encouraged. Example from practice: .NET file-based apps
  could not build a Stride project because `Stride.AssetCompiler.targets` concatenated `$(ProjectDir)`
  with `$(IntermediateOutputPath)`, which is relative for a normal project but absolute for a
  file-based app. Three lines changed to `[System.IO.Path]::Combine(...)` fixed it.
- **Prove the fix is non-breaking.** For that change, the relative-path case was verified to produce a
  byte-identical result, so existing projects were provably unaffected. Do this before proposing
  anything that touches shared build logic.
- **Stay shallow unless asked.** Propose the fix, show the diff, and stop. Do not refactor
  surrounding engine code, chase adjacent issues, or begin a broader cleanup without being asked.
  Depth into the engine is opt-in, on request.
- **Leave it for the maintainer.** Make the change on a branch and leave it uncommitted so it can be
  reviewed and tested against a real engine build. Do not commit or push to the engine repository.
- **Mention, do not silently fix.** Unclear or missing XML documentation, typos, and suspicious
  patterns noticed in passing are worth reporting. Fixing them as a side effect of unrelated work
  makes the diff harder to review and is out of scope unless requested.

### Building the engine clone: native link fails with `could not open 'kernel32.lib'`

Stride links its native DLLs (`libstridevr`, `libstridecrash`, ...) by calling the bundled `lld.exe`
from `Stride.Dependencies.LLVM.Windows` and relying on lld to find the MSVC toolset and the Windows
SDK by itself. With Visual Studio 2026 (VS 18) and lld 22 that self-discovery fails: in a plain
process, or in Visual Studio launched from the Start menu, every native link dies with
`lld : error : could not open 'kernel32.lib'`, even though the SDK and the C++ workload are installed.
Installing or uninstalling the ARM64 build tools does not change this; the x64 link fails first.

The fix is two **user-level environment variables**, the ones a developer prompt would set, which lld
honours for the toolset and, from there, the SDK. Set them once with `setx` (new processes only, so
restart Visual Studio afterwards), adjusting the edition and toolset version to the machine:

```
setx VCINSTALLDIR "C:\Program Files\Microsoft Visual Studio\18\Professional\VC\"
setx VCToolsInstallDir "C:\Program Files\Microsoft Visual Studio\18\Professional\VC\Tools\MSVC\14.51.36231\"
```

- Both are needed. `VCINSTALLDIR` alone finds the SDK but not the CRT (`libcmt.lib`, `oldnames.lib`,
  `libcpmt.lib`). The toolset version folder is under `VC\Tools\MSVC\`.
- **`VCToolsInstallDir` pins a toolset version.** When a Visual Studio update replaces the toolset,
  the native link fails on the CRT again until the variable is updated to the new folder.
- Do not set `LIB`, and do not launch Visual Studio from a developer prompt as a substitute: the
  prompt's x64 `LIB` wins over lld's discovery and the ARM64 link then fails with
  `kernel32.lib: machine type x64 conflicts with arm64`. If a developer prompt is the only option,
  pass `-p:StrideNativeWindowsArm64Enabled=false` to skip the ARM64 build (it is optional and
  auto-skipped with a warning when the ARM64 toolset is absent).
- Diagnose quickly by linking nothing but libraries, which tests discovery without a build:
  `lld.exe -flavor link -nologo -machine:x64 -dll -noentry -defaultlib:libcmt -defaultlib:oldnames -out:probe.dll kernel32.lib`
  exits 0 when discovery works; repeat with `-machine:arm64`.
- Undo with `reg delete HKCU\Environment /v VCINSTALLDIR` and the same for `VCToolsInstallDir`.

## Reference repositories (read them before writing physics code)

Two sibling clones are often available next to this one. Neither is required, but when present they
answer questions faster and more reliably than reasoning from the API surface.

| Path | What it is | When to read it |
|---|---|---|
| `../stride/sources/` | The Stride engine source | Confirming what a wrapper actually does; locating the cause of an engine-level limitation |
| `../bepuphysics2/Demos/Demos/` | The Bepu author's own demos | Before writing anything non-trivial with Bepu |

Guidance for the Bepu demos specifically:

- **They outrank the Stride Bepu playground** (`../stride/samples/Physics/BepuSample/`) where the two
  disagree. The playground demonstrates that something is *possible*; the demos show the way the
  physics author intended, and often explain in comments why the obvious approach misbehaves.
  Concrete case: the playground builds ropes from rigid ball sockets plus swing limits, while
  `RopeStabilityDemo` builds them from `DistanceLimit` with zero lever arms and explains that the
  naive version is exactly what goes unstable.
- Read the matching demo *first* and note what it warns about, rather than porting a scene and then
  debugging the physics.
- Useful map: ropes → `RopeStabilityDemo`, `RopeTwistDemo`; friction and bounce → `FrictionDemo`,
  `BouncinessDemo`; contacts and triggers → `ContactEventsDemo`, `CollisionTrackingDemo`; queries →
  `SweepDemo`, `CollisionQueryDemo`, `RayCastingDemo`; stacking → `PyramidDemo`, `ColosseumDemo`;
  solver stability → `SubsteppingDemo`.

## Adding a new example

- Create a folder under `examples/code-only/` named `E<NN>_<Dimension>_<Subject>`, optionally with
  a `_<Qualifier>` suffix: `E11_3D_ShapeBatch_Gallery`, `E06_Box2D_Junkyard`, `E01_3D_BasicScene_FSharp`.
  The dimension is `2D` or `3D`, or the library when that is the point (`Box2D`, `Jitter2`,
  `Audio`); the number groups examples by topic, not by order of creation. Each variant gets its
  own folder, not sibling files in a shared one.
- Add the project to `Stride.CommunityToolkit.slnx`.
- End `Program.cs` with an `---example-metadata` YAML block inside a block comment. Copy the shape
  from a neighbouring example; `Stride.CommunityToolkit.Examples.MetadataGenerator` parses it into
  `examples-manifest.json`.
  - **Quote any value containing `#` or `:`.** `#` starts a YAML comment, so
    `- Uses #:package` is silently truncated to `- Uses` with no error. This is why entries such as
    `"Using helpers: SceneSetup3D, CreatePrimitive"` are quoted.
  - An example built on a toolkit package that is not on NuGet yet gets a "run it from a clone"
    note on its page and on the index automatically. When a package joins the publish list in
    `.github/workflows/dotnet-nuget.yml`, take it off `DocPaths.PackagesNotOnNuGet` in the
    generator and regenerate the docs.
- Examples reference toolkit libraries by `ProjectReference`, not `PackageReference`.
- **DPI awareness comes from code, not a manifest.** An example calls
  `WindowsDpiManager.EnablePerMonitorV2();` before `new Game()` and has no `app.manifest`.
  `E08_DpiAware` is the one example that shows the manifest route instead. Never both: with a
  manifest present Windows refuses the call and keeps the manifest's setting.
- **Do not bind example keys that the camera controller already owns.** `Add3DCameraController`
  (included in `SceneSetup3D`) claims `W A S D`, `Q E`, the arrow keys, `NumPad 2/4/6/8`,
  `LeftShift`/`RightShift`, `H`, `F2` and `F3`; `Add2DCameraController` (in `SceneSetup2D`)
  claims the arrow keys, the shifts, `H` and `F2`, and `W A S D` only when `EnableWasdMovement` is on. Binding one of those gives a key that appears to work
  intermittently while also flying the camera — `S` for "stabilise" is a real example of this. Safe
  single letters include `G J K L M N P R T Z`.
- **A key binding lives in three places**: the `IsKeyPressed` call, the on-screen label, and any
  header comment describing the controls. Rename one and the others silently drift, leaving
  documentation that names a key doing nothing. Grep for the old letter after changing a binding.
- **The on-screen help reads top down.** In an example's `TextOverlay` section the key lines come
  first, one key per line, then a blank line, then the status and explanation lines. A key line names
  its keys as data, never in the text: `new("N", "Next station", Color.Gold)`, and
  `new(["Q", "E"], "Ascend / descend")` for keys that do the same thing - not `new("[N] Next station")`.
  The overlay decorates them when it draws (`TextOverlay.KeyFormat`, default `[{0}]`, and `KeyColor`,
  a shade lighter than the text), and gives a collapsible title the same shape, `[+] [F2] Camera controls`.
  Multi-word inputs are just keys: `new("Arrow keys", "Move")`, `new("Mouse wheel", "Zoom")`,
  `new("Shift", "Hold to move faster")`, `new("Left drag", "Pan")`. What a key does is capitalised
  like a sentence, with no colon or dash. Never three unrelated keys joined on one line. Keep every
  line to about fifty characters and split a longer sentence over two lines. Live
  numbers use fixed decimals (`{x:0.00}`) so the block does not change width every frame. A reader
  scans the block vertically for the key they want; a long horizontal line hides it.
- Random values come from a `Random` the example owns, through the toolkit's extensions on it
  (`random.NextColor()`, `random.NextVector3(min, max)`); a seeded `new Random(seed)` then reproduces
  the scene.

## Running & debugging examples (AI assistants)

Code-only examples are GUI applications that run until the window is closed, so a plain `dotnet run` cannot be waited on and read back. Use this loop instead: build, launch the built executable with redirected output, wait, terminate, then read the captured log. Verify engine behaviour this way rather than reasoning about it — assumptions about Stride internals are frequently wrong.

### Run an example and capture its console output

```powershell
$out = "$env:TEMP\example-run.txt"
dotnet build examples\code-only\E02_3D_GiveMeACube\E02_3D_GiveMeACube.csproj -v q --nologo
$exe = "examples\code-only\E02_3D_GiveMeACube\bin\Debug\net10.0\E02_3D_GiveMeACube.exe"
$process = Start-Process $exe -PassThru -RedirectStandardOutput $out -WorkingDirectory (Split-Path $exe)
Start-Sleep -Seconds 12
if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
Get-Content $out | Select-String "DIAG"
```

### Where temporary diagnostics actually surface

- **Top-level statements and the `game.Run(start:/update:)` callbacks**: `Console.WriteLine` reaches the redirected stream.
- **Inside a `SyncScript` / `AsyncScript` / `StartupScript`**: `Console.WriteLine` does *not* reach it. Use the script's own logger (`Log.Info`, `Log.Warning`).
- **Inside a render feature or game system**: use `GlobalLogger.GetLogger("Name")`.
- Stride writes log lines to both the console and the redirected stream, so captured output shows each line twice. Expect the duplicates or pipe through `Select-Object -Unique`.

### Making per-frame diagnostics readable

Gate on a frame counter to keep the log short, but always include the first few frames:

```csharp
_frames++;
if (_frames > 3 && _frames % 120 != 0) return;

Log.Warning($"DIAG position={Entity.Transform.Position}");
```

Gating on `% N` alone can produce no output at all when the run is short or the frame rate is low, which is easily misread as "the code never ran". Prefix diagnostic lines with a unique token such as `DIAG` so they can be filtered out of Stride's own logging.

### Screenshots and golden images

Capture is **in-engine**, never off the screen: with `STRIDE_TOOLKIT_CAPTURE` set to a PNG path
(and optionally `STRIDE_TOOLKIT_CAPTURE_FRAME`), every example saves its render target at that
frame and exits. Capture runs on a fixed timestep with one update per draw, hides the profiler
readout and pins auto-exposure, so the same frame is the same image on every run.

```powershell
dotnet run --file build/capture-screenshots.cs -- --review --only shape-batch --keep-png   # look at one example
dotnet run --file build/gold-images.cs                                                    # compare the gold scenes with tests/gold, on WARP
dotnet run --file build/gold-images.cs -- --only shapes-2d --noise                        # what drift looks like with no change
dotnet run --file build/gold-images.cs -- --only shapes-2d --update                       # accept a reviewed change
```

- `--review` writes to `screenshots-review/`, the gold script to `screenshots-review/gold/`, each
  with an `index.html` contact sheet. Nothing is committed by either.
- The raw PNG carries whatever alpha the renderer left; flatten it or view it through the contact
  sheet, and never composite it onto black.
- The gold rule is Stride's: any pixel off by 3 or more fails; `tests/gold/thresholds.jsonc` relaxes
  it per image, with a reason. A scene driven by unseeded randomness, real-time text or the network
  cannot be a golden - run `--noise` on a candidate first.
- A golden is only valid for the renderer that made it; goldens meant to match across machines are
  captured with `--warp` (Stride's software adapter).
- To see the moment a scene reaches, capture two frames apart (`--frame`), not two runs.
- The scene ids are in `tests/gold/scenes.jsonc`; `grep` the gold scenes for the type you changed to
  know which `--only` to run. Not every renderer has a scene: the flat 2D primitives and the prism do
  not, which is why they have geometry tests instead.

The full write-up is in [Contributing: examples](../docs/contributing/examples/index.md#screenshots).

### Do not assert a mechanism you have not read

Explaining *why* something misbehaves is not the same as observing *that* it does. Inferring a cause
from behaviour alone produces confident, wrong explanations that then get written into comments and
documentation, where they outlive the bug.

From practice: a motor stopped working after a property was set, and this was reported as a Stride
wrapper bug — the setter "did not reach the solver". Reading the base class disproved it in a minute
(the field is passed straight to `Solver.Add`), and one line printing the property's real default
revealed the actual cause: the value is the reciprocal of the constructor argument, so "setting it to
the default" made it 2500x softer. A rebuild of the engine packages was nearly requested to fix a bug
that did not exist.

- Before naming a cause, read the code path or measure the value. Both are cheap.
- Before proposing an engine fix, confirm the engine is actually at fault.
- When a claim was wrong, correct it everywhere it was written, not just in conversation.
- The same applies to review comments, from a person or a tool: check each against the code before
  acting. Recent rounds included "TimeSpan / TimeSpan does not compile" (it has since .NET Core 3.0),
  "the wrapper lost its null check" (the extension call reaches the validated core) and "this
  switch dynamically computes the mask" (it was a constant). Say which comments hold and which do not.

### Build warnings are a debugging tool

- Real defects hide in the warning list. A Stride 4.4 regression that silently broke the ImGui.NET integration was found only through a single `warning CS9193` among 66 warnings.
- Filter with `Select-String ": error|warning"`, not `warning CS` alone: Stride's analysers report as `STRDIAG` and would be filtered out. Filtering by project path also matches unrelated `NU1903` NuGet advisories.

### Always clean up

- Remove every temporary diagnostic, then confirm with `git status` before reporting the work as complete.

## AI assistance guidance

- Inspect the existing implementation before proposing changes; do not invent APIs or patterns that are not present in the repository.
- Prefer minimal, focused diffs that preserve existing style and project structure.
- When asked to reword or fix grammar for a highlighted or selected paragraph/section, modify only that selection; do not change other parts of the document.
- Improve or extend existing helpers instead of duplicating similar logic.
- Do NOT introduce unrelated frameworks or patterns (for example, Unity managers, large DI containers, Rx) unless explicitly requested.
- Highlight potential breaking changes when modifying public APIs.
- Because the toolkit is still in Preview, do not avoid breaking-change proposals solely for backward compatibility. Prefer the cleaner long-term API, document the impact, and update examples/docs together with the change.
- Prefer Bepu examples over Bullet unless addressing migration or legacy parity.
- When a shader changes, run the golden images before and after, and say which goldens were re-baselined and why.
- For Blazor content: keep solutions Blazor-appropriate; avoid server-only MVC/Razor patterns unless necessary.
- Avoid speculative APIs; ground suggestions in existing patterns.
- When changing public APIs, update XML docs, examples, and conceptual documentation as needed.
- Validate changes with the most relevant build or tests available.
- **Leave finished work uncommitted.** The maintainer reviews file by file and commits what is approved; do not commit, push or open a pull request unless asked for that specific action, in that context. Public renames and removals are proposed in a table with call counts, not applied.
- Use descriptive, real-word identifier names. Avoid cryptic abbreviations for variables, parameters, or fields (e.g., prefer `textureCoordinates`, `firstEdge`, `secondEdge`, `faceNormal` over `tex`, `e1`, `e2`, `n`). Single-letter names are acceptable only for short-lived loop indices (`i`, `j`, `k`).
- Follow C# naming conventions consistently: PascalCase for types, methods, and properties; camelCase for parameters and local variables. Prefer meaningful names that communicate intent.

## Formatting rules for edits

- **Keep the file's line endings.** Files in this repository are CRLF in the working copy (the index
  holds LF; `core.autocrlf` converts). Never convert a file to LF, and never mix the two: a tool that
  writes LF (a heredoc, a script, a whole-file rewrite) must be followed by normalising the file back
  to CRLF, and the result checked with `git ls-files --eol <path>`, which should read `w/crlf`. A
  line-ending change turns a one-line diff into a whole-file diff and hides the real change from review.
- **`sed -i` in Git Bash rewrites a CRLF file as LF**, and `git diff` hides it behind the autocrlf
  warning. Edit with `perl -pi` (which keeps `\r`) or PowerShell, never `sed -i`, and never leave a
  file whose last line ends in a lone `\r`: git then treats it as binary and diffs the whole file.
- **Files end on their last character, with no trailing newline.** Every source and Markdown file in
  the repository ends on its closing brace or final sentence; an editor that shows an empty last line
  is showing a newline that should not be there. Tools that append a final newline (`Write`, heredocs,
  `printf '%s\n'`) must be followed by trimming it.
- After an edit, check the touched region for a doubled blank line or a blank line left between a
  doc comment and its member; the file should read as if the removed text had never been there.
- Trailing whitespace is trimmed (`.editorconfig`); do not introduce any.
- When moving or copying code, preserve existing blank lines.
- When adding new code, separate logical blocks with a single blank line. It is acceptable to group closely related declarations or multiple similar statements without intervening blank lines.

## Maintenance

> [!IMPORTANT]
> Keep this document current (architectural shifts, new subsystems, deprecations) so AI assistance remains accurate.

- Update for structural or convention changes.
- Prune outdated or redundant guidelines.
- Add new exceptions or patterns explicitly.
- Revisit after introducing new physics systems, rendering pipelines, or scripting paradigms.

## Quick checklist (before merging)

- [ ] XML docs complete / updated
- [ ] Nullability warnings resolved
- [ ] No unnecessary allocations in hot paths
- [ ] Fluent extensions return `this` where appropriate
- [ ] Examples updated (if API changes)
- [ ] Conceptual + API docs updated
- [ ] Release-notes entry written under the version in progress (Before / After / Note table for a breaking or visible change)
- [ ] Golden images run, and re-baselined only for reviewed visual changes (if shaders or renderers changed)
- [ ] Core filter and full solution both build with no warnings
- [ ] Provenance clarified for imported code

---
If something here becomes outdated or ambiguous, update it promptly. Concise, accurate guidance improves AI output quality and reduces maintenance overhead.