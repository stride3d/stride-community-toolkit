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

- A collection of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/), primarily targeting **.NET 10** (some projects may also multi-target newer frameworks).
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
- `benchmarks/`: BenchmarkDotNet-based performance tests (primary suite)
- `tests/`: Unit and regression test projects (xUnit, targeting net10.0)
- `docs/`: DocFX sources (manuals, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation, and this instruction file

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
- Only **awake** bodies are synced back to their transform. A dynamic body that settles and falls asleep stops overwriting the transform, so direct transform writes suddenly appear to work while the collider is left behind. A "moving mesh with no collisions" almost always means this.
- `Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a `BodyComponent`, but a `CompoundCollider` with no shapes never attaches to the simulation, leaving an inert component. For a purely visual entity use the non-physics `Create3DPrimitive` overload by passing `Primitive3DEntityOptions` instead.
- `Create3DPrimitive` has both a Bepu overload (`Bepu3DPhysicsOptions`) and a plain one (`Primitive3DEntityOptions`). Passing an explicitly typed options object selects the intended overload and avoids `CS0121` ambiguity when both namespaces are imported.

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

## Common code-only example pattern

```csharp
using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere)
                     .AddRigidBody(RigidBodyTypes.Dynamic);
    entity.Transform.Position = new Vector3(0, 10, 0);
    entity.Scene = rootScene;
});
```

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
$bitmap = New-Object System.Drawing.Bitmap 1200, 900
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen(0, 0, 0, 0, $bitmap.Size)
$bitmap.Save("$env:TEMP\shot.png", [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose(); $bitmap.Dispose()
```

- Prefer positioning the window from the example itself (`game.Window.Position`, `game.Window.AllowUserResizing`) over forcing it with a `SetWindowPos` P/Invoke. Forcing a resize leaves Stride's `Window.ClientBounds` out of sync with the captured region, which can make correctly rendered UI look as though it is missing.
- Capture two screenshots a few seconds apart to confirm that animation or physics is actually progressing.

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