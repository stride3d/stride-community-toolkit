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
    - `Engine/`: Game and Entity extensions
    - `Extensions/`: General-purpose extensions
    - `Graphics/`: Graphics utilities
    - `Helpers/`: Helper classes
    - `Mathematics/`: Math utilities (e.g., easing)
    - `Physics/`: Physics extensions
    - `Rendering/`: Rendering utilities, including world text (`Rendering/Text`), the debug overlay and display scale
    - `Scripts/`: Reusable script components
  - **Stride.CommunityToolkit.Bepu**: Bepu physics integration (primary)
  - **Stride.CommunityToolkit.Box2D**: Box2D.NET integration for 2D physics (not packed yet)
  - **Stride.CommunityToolkit.Bullet**: Bullet physics integration (legacy / transitional, pending deprecation)
  - **Stride.CommunityToolkit.Charts**: 2D and 3D charts drawn with ShapeBatch (not packed yet)
  - **Stride.CommunityToolkit.DebugShapes**: Debug visualization tools
  - **Stride.CommunityToolkit.ImGui**: ImGui integration
  - **Stride.CommunityToolkit.ImGuiNet**: ImGui.NET bindings and helpers
  - **Stride.CommunityToolkit.Linux**: Linux-specific features
  - **Stride.CommunityToolkit.Shapes**: `ShapeBatch`, the signed-distance-field shape renderer behind the HUD, panel and gizmo examples (see `docs/manual/rendering/shape-batch.md`)
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: Windows-specific features
- `examples/`: Code-only and snippet example projects (C#, F#, VB)
- `benchmarks/`: BenchmarkDotNet-based performance tests
- `tests/`: Unit and regression test projects (xUnit, targeting net10.0), and `tests/gold/`, the golden images that `build/gold-images.cs` compares example captures against
- `tools/`: Supporting tools, not shipped as packages
  - **Stride.CommunityToolkit.Examples**: Console example launcher
  - **Stride.CommunityToolkit.Examples.Launcher**: Avalonia example launcher
  - **Stride.CommunityToolkit.Examples.MetadataGenerator**: Generates `examples-manifest.json` from example metadata
- `build/`: Repository scripts, all .NET file-based apps run with `dotnet run --file build/<name>.cs`: `pack-local.cs` (local dev NuGet packages), `capture-screenshots.cs` (the documentation screenshots), `gold-images.cs` (rendering regression against `tests/gold`)
- `docs/`: DocFX sources (manuals, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation, and this instruction file

Solutions: `Stride.CommunityToolkit.slnx` contains everything; `Stride.CommunityToolkit.Core.slnf`
is a solution filter loading only libraries, tests and tools, because the 56 example projects slow
IDE load noticeably. See [Building the Toolkit](../docs/contributing/toolkit/building.md).

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

### Bepu transform ownership (frequent source of confusion)

- The transform sync is **one-way: physics → `TransformComponent`**. Assigning `Entity.Transform.Position` on an entity that has an attached body moves the mesh only; the body stays where the simulation put it.
- To move a body deliberately: `Teleport(...)` jumps it without checking collisions, while scripted motion that should collide belongs on a body with `Kinematic = true`.
- Prefer setting `LinearVelocity` on a kinematic body over calling `SetTargetPose(...)` from a per-frame `Update`. `SetTargetPose` derives its velocity from `(target - position) / FixedTimeStep`, which assumes exactly one physics tick per call. When the frame rate falls below the physics rate two ticks run on that velocity, the body overshoots the target, the next correction overshoots further, and it diverges to `NaN` within seconds. `SetTargetPose` is safe when the caller runs once per physics tick (`ISimulationUpdate.SimulationUpdate`) or when the frame rate is pinned to the physics rate; otherwise integrate a velocity you compute yourself, and add a small proportional pull towards the ideal position to stop drift.
- Setting `LinearVelocity` does **not** wake a sleeping body — set `Awake = true` as well, or the motion silently stops once the body sleeps.
- `ISimulationUpdate.SimulationUpdate` can run **before** `StartupScript.Start` / `SyncScript.Start`. The component is registered with the simulation as soon as it enters the scene, while `Start` waits its turn in the script system. Resolve component references lazily inside `SimulationUpdate` (`_body ??= Entity.Get<BodyComponent>()`) rather than caching them in `Start`.
- Only **awake** bodies are synced back to their transform. A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform writes suddenly appear to work while the collider is left behind. A "moving mesh with no collisions" almost always means this.
- `Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a `BodyComponent`, but a `CompoundCollider` with no shapes never attaches to the simulation, leaving an inert component. For a purely visual entity use the non-physics `Create3DPrimitive` overload by passing `Primitive3DEntityOptions` instead.
- `Create3DPrimitive` has both a Bepu overload (`Bepu3DPhysicsOptions`) and a plain one (`Primitive3DEntityOptions`). Passing an explicitly typed options object selects the intended overload and avoids `CS0121` ambiguity when both namespaces are imported.
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
entity.Add3DCameraController()
      .AddGizmo(graphicsDevice)
      .SetPosition(Vector3.UnitY);
```
Guidelines:
- Return the modified instance (fluent chaining) where it’s natural.
- Validate inputs early (`ArgumentNullException.ThrowIfNull`).
- Avoid hiding heavy allocations or long-running work behind simple-sounding extension names.

### Code-only scene creation

```csharp
using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}
```

### Components in Game Studio

Every `EntityComponent` and script the toolkit ships must work when added in Game Studio, not only
from code: assembly registered for scanning (`Module.cs`), `[DataContract]`, a `[Display]` name and a
`[ComponentCategory]`, `List<T>` never arrays, no nullable value types, and a processor that runs in
the editor and provisions its own renderer on the owning compositor. The recipe, with the editor
traps, is `docs/contributing/toolkit/game-studio-components.md`; the shape and text components are
the worked examples. Verify against a package from `build/pack-local.cs`, not a `ProjectReference`.

## Coding Style & Conventions

- Use latest C# features (file-scoped namespaces, target-typed `new`, pattern matching, spans where beneficial, primary ctors where suitable).
- Keep nullable-reference warnings at zero.
- Public APIs: include complete XML docs (`<summary>`, `<param>`, `<returns>`, `<example>` when useful) including top level classes.
- Naming: `Stride.CommunityToolkit.<LibraryName>` for new libs; PascalCase for types and methods; camelCase for parameters.
- Terminology / capitalization: Use “Bepu” (capital B only) in identifiers and XML docs; never “BEPU” or “bepu”. Use “Bullet” (capital B) for Bullet physics.
- One public type per file; avoid unrelated multi-class files.
- Avoid `#region`; write self-explanatory code.
- Avoid partial classes unless auto-generated code is involved.
- Validation: prefer `ArgumentNullException.ThrowIfNull()` and provide meaningful exception messages for invalid states.
- Suggestion preference order:
  1. Existing, reviewed toolkit extension/helper
  2. New small, composable extension (documented)
  3. Direct Stride API usage
  4. External snippet (must justify and ensure license compatibility)
- Performance:
  - Cache frequently used component references inside update loops.
  - Avoid per-frame allocations (consider pooling or struct patterns where appropriate).
  - Dispose GPU/graphics resources deterministically (`using` / `Dispose`).
- Threading: Mutations to the scene graph, entities, components, or graphics resources must occur on the main thread.
- Physics: Do not combine Bepu and Bullet physics components on the same entity.
- Shaders (*.sdsl): see [Shaders](#shaders-sdsl) below. Nothing is regenerated by hand; a shader change is verified against the golden images.
- Experimental / provisional APIs: consider marking with an `[Experimental]` attribute (future) or note in the XML summary.
- Tests: Use xUnit under `tests/` targeting net10.0; keep deterministic and avoid relying on real-time frame counts. Rendering changes are verified with `build/gold-images.cs` against `tests/gold` (see [Screenshots and golden images](#screenshots-and-golden-images)).

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

- Prefer modern C# features when they improve clarity: file-scoped namespaces, pattern matching, collection expressions, raw string literals, target-typed `new`, `required`/`init`, and primary constructors where they reduce boilerplate without hiding behavior.
- Use `var` only when the type is obvious from the right-hand side, required by anonymous types, or improves readability.
- Keep nullable reference types enabled and avoid the null-forgiving operator (`!`) unless the invariant is obvious or documented. Prefer nullable analysis attributes such as `[NotNullWhen]`, `[MemberNotNull]`, and `[MaybeNull]` for public contracts.
- Prefer `.editorconfig`, Roslyn analyzers, and project settings to enforce style and quality. Do not suppress analyzer warnings without a clear reason.
- Use `async`/`await` for I/O-bound work. Avoid sync-over-async. In library code, use `ConfigureAwait(false)` when a synchronization context is not required; in Blazor/UI code, preserve the UI context and use `InvokeAsync` when updating UI state.
- Use performance-oriented APIs such as `Span<T>`, `Memory<T>`, pooling, or unsafe code only when they clearly improve correctness or measured performance.
- Prefer specific exception types and meaningful error messages. Do not catch `Exception` broadly unless adding context and rethrowing or handling a known boundary.

## Documentation guidelines

- Docs are generated with DocFX from `docs/`.
- Update conceptual docs and XML comments when changing public APIs.
- New libraries: update navigation, TOC, and contributing guides (`docs/contributing/toolkit/library-project.md`).
- Provide concise, runnable examples that minimize boilerplate.
- Keep `docs/release-notes/index.md` current: every change goes under the heading of the version in
  progress, in the categories of `.github/release.yml` (breaking changes first - a renamed or moved
  public type, a changed property type, a visual change a user would notice). Write the entry with the
  change, not at release time.

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
    `"Using helpers: SetupBase3DScene"` are quoted.
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
  (included in `SetupBase3DScene`) claims `W A S D`, `Q E`, the arrow keys, `NumPad 2/4/6/8`,
  `LeftShift`/`RightShift`, `H`, `F2` and `F3`; `Add2DCameraController` (in `SetupBase2DScene`)
  claims `W A S D`, the arrow keys, the shifts, `H` and `F2`. Binding one of those gives a key that appears to work
  intermittently while also flying the camera — `S` for "stabilise" is a real example of this. Safe
  single letters include `G J K L M N P R T Z`.
- **A key binding lives in three places**: the `IsKeyPressed` call, the on-screen label, and any
  header comment describing the controls. Rename one and the others silently drift, leaving
  documentation that names a key doing nothing. Grep for the old letter after changing a binding.
- **The on-screen help reads top down.** In an example's `DebugOverlay` section the key lines come
  first, one key per line, then a blank line, then the status and explanation lines. A key line names
  its keys as data, never in the text: `new("N", "Next station", Color.Gold)`, and
  `new(["Q", "E"], "Ascend / descend")` for keys that do the same thing - not `new("[N] Next station")`.
  The overlay decorates them when it draws (`DebugOverlay.KeyFormat`, default `[{0}]`, and `KeyColor`,
  a shade lighter than the text), and gives a collapsible title the same shape, `[+] [F2] Camera controls`.
  Multi-word inputs are just keys: `new("Arrow keys", "Move")`, `new("Mouse wheel", "Zoom")`,
  `new("Shift", "Hold to move faster")`, `new("Left drag", "Pan")`. What a key does is capitalised
  like a sentence, with no colon or dash. Never three unrelated keys joined on one line. Keep every
  line to about fifty characters and split a longer sentence over two lines. Live
  numbers use fixed decimals (`{x:0.00}`) so the block does not change width every frame. A reader
  scans the block vertically for the key they want; a long horizontal line hides it.

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

### Build warnings are a debugging tool

- Real defects hide in the warning list. A Stride 4.4 regression that silently broke the ImGui.NET integration was found only through a single `warning CS9193` among 66 warnings.
- Filter with `Select-String ": error|warning CS"`. Filtering by project path also matches unrelated `NU1903` NuGet advisories.

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
- Use descriptive, real-word identifier names. Avoid cryptic abbreviations for variables, parameters, or fields (e.g., prefer `textureCoordinates`, `firstEdge`, `secondEdge`, `faceNormal` over `tex`, `e1`, `e2`, `n`). Single-letter names are acceptable only for short-lived loop indices (`i`, `j`, `k`).
- Follow C# naming conventions consistently: PascalCase for types, methods, and properties; camelCase for parameters and local variables. Prefer meaningful names that communicate intent.

## Formatting rules for edits

- **Keep the file's line endings.** Files in this repository are CRLF. Never convert a file to LF, and
  never mix the two: a tool that writes LF (a heredoc, a script, a whole-file rewrite) must be followed
  by normalising the file back to CRLF, and the result checked (`file <path>` should say "CRLF" and
  nothing else). A line-ending change turns a one-line diff into a whole-file diff and hides the real
  change from review.
- **Do not add an empty line at the end of a file.** The last line ends with one newline and nothing
  after it. If a file already ends with a blank line, it can be removed as part of an edit to that
  file.
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
- [ ] Golden images run, and re-baselined only for reviewed visual changes (if shaders or renderers changed)
- [ ] Provenance clarified for imported code

---
If something here becomes outdated or ambiguous, update it promptly. Concise, accurate guidance improves AI output quality and reduces maintenance overhead.