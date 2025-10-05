# Copilot for Stride Community Toolkit

These repository instructions guide GitHub Copilot (and similar AI assistants) to help develop the Stride Community Toolkit solution.

## Quick editing & display guidance (short)
- When returning an edit to a single paragraph or section, output only that updated selection (do not include the rest of the file). If context is helpful, add a small window (up to ~10 lines before and after). Include the file path and exact line range for the change when known; if not known, ask the reviewer for the specific lines.

## Status & stability
- The Stride Community Toolkit is currently in **Preview**.
- Public APIs, namespaces, behaviors, and package layout may change without backward-compatibility guarantees until the first stable release.
- Many extensions and helpers originated from community sources (forum posts, samples, gists, experimental repos). Some code paths have not yet been fully reviewed, optimized, or documented.
- Treat sparsely documented or unusual APIs as provisional. Prefer improving them (tests, XML docs, consistency) before broad reuse.
- Prefer well-documented, core, and recently updated toolkit helpers over unverified examples.

## Project overview
- A collection of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/), primarily targeting **.NET 8** (some projects may also multi-target newer frameworks such as .NET 9).
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
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: Windows-specific features
- `examples/`: Code-only and snippet example projects (C#, F#, VB)
- `docs/`: DocFX sources (manuals, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation, and this instruction file

## Stride engine context (quick reminders)
- ECS: Entities aggregate Components (Transform, Model, Camera, Rigidbody, Script, etc.).
- Entities must be added to a Scene graph to be processed.
- Physics: Prefer Bepu components; keep Bullet only for transition/testing. Avoid mixing both on the same entity.
- Core components commonly manipulated: Transform (position, rotation, scale), Camera, Rigidbody, Script logic.

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

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene(); // Camera, lighting, ground
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
});
```

## Coding Style & Conventions
- Use latest C# features (file-scoped namespaces, target-typed `new`, pattern matching, spans where beneficial, primary ctors where suitable).
- Keep nullable-reference warnings at zero.
- Public APIs: include complete XML docs (`<summary>`, `<param>`, `<returns>`, `<example>` when useful).
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
- Tests: Keep deterministic; avoid relying on real-time frame counts.

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

## AI assistance guidance
- When asked to reword or fix grammar for a highlighted or selected paragraph/section, modify only that selection; do not change other parts of the document.
- When showing the updated result for a single paragraph or section, display only the updated selection. Do not render unchanged surrounding content in long documents to reduce scrolling and make changes easier to review.
- Improve or extend existing helpers instead of duplicating similar logic.
- Do NOT introduce unrelated frameworks or patterns (for example, Unity managers, large DI containers, Rx) unless explicitly requested.
- Highlight potential breaking changes when modifying public APIs.
- Prefer Bepu examples over Bullet unless addressing migration or legacy parity.
- Remind contributors to regenerate shader code when shaders are changed.
- For Blazor content: keep solutions Blazor-appropriate; avoid server-only MVC/Razor patterns unless necessary.
- Avoid speculative APIs; ground suggestions in existing patterns.
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
