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
    - `Rendering/`: Rendering utilities
    - `Scripts/`: Reusable script components
  - **Stride.CommunityToolkit.Bepu**: Bepu physics integration (primary)
  - **Stride.CommunityToolkit.Bullet**: Bullet physics integration (legacy / transitional, pending deprecation)
  - **Stride.CommunityToolkit.DebugShapes**: Debug visualization tools
  - **Stride.CommunityToolkit.ImGui**: ImGui integration
  - **Stride.CommunityToolkit.ImGuiNet**: ImGui.NET bindings and helpers
  - **Stride.CommunityToolkit.Linux**: Linux-specific features
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: Windows-specific features
- `examples/`: Code-only and snippet example projects (C#, F#, VB)
- `benchmarks/`: BenchmarkDotNet-based performance tests
- `tests/`: Unit and regression test projects (xUnit, targeting net10.0)
- `tools/`: Supporting tools, not shipped as packages
  - **Stride.CommunityToolkit.Examples**: Console example launcher
  - **Stride.CommunityToolkit.Examples.Launcher**: Avalonia example launcher
  - **Stride.CommunityToolkit.Examples.MetadataGenerator**: Generates `examples-manifest.json` from example metadata
- `build/`: Repository build scripts (e.g. `pack-local.cs` for local dev NuGet packages)
- `docs/`: DocFX sources (manuals, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation, and this instruction file
- `ARCHITECTURE.md` (root): running backlog of API-design observations — see below

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
| `examples/Directory.Build.props` | Example projects only | Host-only `RuntimeIdentifier`, `SelfContained`, output-path settings that keep the example build small |
| `examples/Directory.Build.targets` | Example projects only | Strips package XML documentation from build output |

Two rules when editing these:

- **MSBuild imports only the *nearest* `Directory.Build.props` / `.targets`.** A nested file must
  explicitly `Import` the one above it, or the parent's settings are silently lost. The files under
  `examples/` do this; preserve it.
- **`StrideVersion` is the single place the Stride version is set.** Reference it as
  `Version="$(StrideVersion)"` in a `PackageReference` rather than hard-coding a version.

## Stride engine context (quick reminders)

- ECS: Entities aggregate Components (Transform, Model, Camera, Rigidbody, Script, etc.).
- Entities must be added to a Scene graph to be processed.
- Physics: Prefer Bepu components; keep Bullet only for transition/testing. Avoid mixing both on the same entity.
- Core components commonly manipulated: Transform (position, rotation, scale), Camera, Rigidbody, Script logic.

### Bepu transform ownership (frequent source of confusion)

- The transform sync is **one-way: physics → `TransformComponent`**. Assigning `Entity.Transform.Position` on an entity that has an attached body moves the mesh only; the body stays where the simulation put it.
- To move a body deliberately: `Teleport(...)` jumps it without checking collisions, while scripted motion that should collide belongs on a body with `Kinematic = true`.
- Prefer setting `LinearVelocity` on a kinematic body over calling `SetTargetPose(...)` from a per-frame `Update`. `SetTargetPose` derives its velocity from `(target - position) / FixedTimeStep`, which assumes exactly one physics tick per call. When the frame rate falls below the physics rate two ticks run on that velocity, the body overshoots the target, the next correction overshoots further, and it diverges to `NaN` within seconds. `SetTargetPose` is safe when the caller runs once per physics tick (`ISimulationUpdate.SimulationUpdate`) or when the frame rate is pinned to the physics rate; otherwise integrate a velocity you compute yourself, and add a small proportional pull towards the ideal position to stop drift.
- Setting `LinearVelocity` does **not** wake a sleeping body — set `Awake = true` as well, or the motion silently stops once the body sleeps.
- `ISimulationUpdate.SimulationUpdate` can run **before** `StartupScript.Start` / `SyncScript.Start`. The component is registered with the simulation as soon as it enters the scene, while `Start` waits its turn in the script system. Resolve component references lazily inside `SimulationUpdate` (`_body ??= Entity.Get<BodyComponent>()`) rather than caching them in `Start`.
- Only **awake** bodies are synced back to their transform. A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform writes suddenly appear to work while the collider is left behind. A "moving mesh with no collisions" almost always means this.
- `Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a `BodyComponent`, but a `CompoundCollider` with no shapes never attaches to the simulation, leaving an inert component. For a purely visual entity use the non-physics `Create3DPrimitive` overload by passing `Primitive3DEntityOptions` instead.
- `Create3DPrimitive` has both a Bepu overload (`Bepu3DPhysicsOptions`) and a plain one (`Primitive3DEntityOptions`). Passing an explicitly typed options object selects the intended overload and avoids `CS0121` ambiguity when both namespaces are imported.

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
- Shaders (*.sdsl): After adding, removing, or changing shader properties, manually regenerate the associated `*.cs` file (remind contributors when shaders are touched).
- Experimental / provisional APIs: consider marking with an `[Experimental]` attribute (future) or note in the XML summary.
- Tests: Use xUnit under `tests/` targeting net10.0; keep deterministic and avoid relying on real-time frame counts.

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

## Architecture notes (`ARCHITECTURE.md`)

[`ARCHITECTURE.md`](../ARCHITECTURE.md) in the repository root collects API-design observations: the
places where the *shape* of an API, rather than a bug in it, is what trips people up. It is a backlog
of observations, not a decision record — nothing in it is agreed or scheduled.

- **Read it before proposing an API change.** The friction may already be recorded, with options and
  impact weighed up.
- **Add to it when friction is noticed**, especially while writing examples, which is where API
  problems surface first. Record the observation even when not acting on it: what was observed, why
  it matters, and what the options are, including the do-nothing one.
- **Keep it current.** Remove items once resolved or rejected, and note which. An item that no longer
  reflects the code is worse than no item.
- Prefer it over burying the observation in a code comment. A comment explains one call site; this
  file is where a pattern across the API gets seen.

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

- Create a folder under `examples/code-only/` named `Example<NN>_<Name>`, optionally with a
  `_<Variant>` suffix. Existing variants each get their own folder (`Example01_Basic3DScene_Primitives`,
  `_MeshLine`, `_FSharp`), not sibling files in a shared folder.
- Add the project to `Stride.CommunityToolkit.slnx`.
- End `Program.cs` with an `---example-metadata` YAML block inside a block comment. Copy the shape
  from a neighbouring example; `Stride.CommunityToolkit.Examples.MetadataGenerator` parses it into
  `examples-manifest.json`.
  - **Quote any value containing `#` or `:`.** `#` starts a YAML comment, so
    `- Uses #:package` is silently truncated to `- Uses` with no error. This is why entries such as
    `"Using helpers: SetupBase3DScene"` are quoted.
- Examples reference toolkit libraries by `ProjectReference`, not `PackageReference`.
- **Do not bind example keys that the camera controller already owns.** `Add3DCameraController`
  (included in `SetupBase3DScene`) claims `W A S D`, `Q E`, the arrow keys, `NumPad 2/4/6/8`,
  `LeftShift`/`RightShift`, `H`, `F2` and `F3`. Binding one of those gives a key that appears to work
  intermittently while also flying the camera — `S` for "stabilise" is a real example of this. Safe
  single letters include `G J K L M N P R T Z`.
- **A key binding lives in three places**: the `IsKeyPressed` call, the on-screen label, and any
  header comment describing the controls. Rename one and the others silently drift, leaving
  documentation that names a key doing nothing. Grep for the old letter after changing a binding.

## Running & debugging examples (AI assistants)

Code-only examples are GUI applications that run until the window is closed, so a plain `dotnet run` cannot be waited on and read back. Use this loop instead: build, launch the built executable with redirected output, wait, terminate, then read the captured log. Verify engine behaviour this way rather than reasoning about it — assumptions about Stride internals are frequently wrong.

### Run an example and capture its console output

```powershell
$out = "$env:TEMP\example-run.txt"
dotnet build examples\code-only\Example02_GiveMeACube\Example02_GiveMeACube.csproj -v q --nologo
$exe = "examples\code-only\Example02_GiveMeACube\bin\Debug\net10.0\Example02_GiveMeACube.exe"
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

### Screenshots

Useful for confirming a visual change, and the resulting PNG can be read back directly.

```powershell
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$screen = [System.Windows.Forms.SystemInformation]::VirtualScreen
$bitmap = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($screen.Left, $screen.Top, 0, 0, $bitmap.Size)
$bitmap.Save("$env:TEMP\shot.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $bitmap.Dispose()
```

`VirtualScreen` is used rather than a fixed size so the capture covers all monitors. A hard-coded
region anchored at `0,0` silently misses the window on a multi-monitor setup, which reads as "the
app never rendered".

- Prefer positioning the window from the example itself (`game.Window.Position`, `game.Window.AllowUserResizing`) over forcing it with a `SetWindowPos` P/Invoke. Forcing a resize leaves Stride's `Window.ClientBounds` out of sync with the captured region, which can make correctly rendered UI look as though it is missing.
- Capture two screenshots a few seconds apart to confirm that animation or physics is actually progressing.

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
- Remind contributors to regenerate shader code when shaders are changed.
- For Blazor content: keep solutions Blazor-appropriate; avoid server-only MVC/Razor patterns unless necessary.
- Avoid speculative APIs; ground suggestions in existing patterns.
- When changing public APIs, update XML docs, examples, and conceptual documentation as needed.
- Validate changes with the most relevant build or tests available.
- Use descriptive, real-word identifier names. Avoid cryptic abbreviations for variables, parameters, or fields (e.g., prefer `textureCoordinates`, `firstEdge`, `secondEdge`, `faceNormal` over `tex`, `e1`, `e2`, `n`). Single-letter names are acceptable only for short-lived loop indices (`i`, `j`, `k`).
- Follow C# naming conventions consistently: PascalCase for types, methods, and properties; camelCase for parameters and local variables. Prefer meaningful names that communicate intent.

## Formatting rules for edits

- Do not add an empty line at the end of a file.
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
- [ ] Shader regeneration reminder (if shaders changed)
- [ ] Provenance clarified for imported code

---
If something here becomes outdated or ambiguous, update it promptly. Concise, accurate guidance improves AI output quality and reduces maintenance overhead.