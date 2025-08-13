# Copilot for Stride Community Toolkit

These repository instructions guide GitHub Copilot (and similar AI assistants) to assist development in the Stride Community Toolkit solution.

## Status & Stability (Preview)
- The Stride Community Toolkit is currently in **Preview**.
- Public APIs, namespaces, behaviors, and package layout may change without backward compatibility guarantees until the first stable release.
- Many extensions and helpers originated from multiple community sources (forum posts, samples, gists, experimental repos). Some code paths have not yet been fully reviewed, optimized, or documented.
- Treat sparsely documented or unusual APIs as provisional. Prefer improving them (tests + XML docs + consistency) before broad reuse.
- Prefer well‑documented, core, and recently updated toolkit helpers over unverified examples.

## Project Overview
- A collection of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/) primarily targeting **.NET 8** (some projects may also multi-target newer frameworks like .NET 9).
- Provides library projects, code‑only examples, snippet examples, and documentation to simplify Stride game development.
- F# and VB.NET examples are showcase only (not primary focus).
- Uses latest Stride version with nullable reference types enabled.
- Includes a Blazor example project; when web UI comes up, prefer Blazor-centric solutions over Razor Pages or ASP.NET Core MVC.
- Designed to integrate with a regular Stride Game Studio project; code‑only examples intentionally avoid reliance on the editor UI/assets to demonstrate pure programmatic setup.

## Repository Structure (Summary)
- `src/`: Core toolkit libraries
  - **Stride.CommunityToolkit**: Core library
    - `Engine/`: Game and Entity extensions
    - `Extensions/`: General-purpose extensions
    - `Graphics/`: Graphics utilities
    - `Helpers/`: Helper classes
    - `Mathematics/`: Math utilities (Easing, etc.)
    - `Physics/`: Physics extensions
    - `Rendering/`: Rendering utilities
    - `Scripts/`: Reusable script components
  - **Stride.CommunityToolkit.Bepu**: Bepu Physics integration (primary physics integration)
  - **Stride.CommunityToolkit.Bullet**: Bullet Physics integration (legacy / transitional physics, pending deprecation)  
  - **Stride.CommunityToolkit.DebugShapes**: Debug visualization tools
  - **Stride.CommunityToolkit.ImGui**: ImGui integration
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: Windows-specific features
- `examples/`: Code-only & snippet example projects (C#, F#, VB)
- `docs/`: DocFX sources (manual, API reference, contributing)
- `.github/`: GitHub workflows, release metadata, automation & this instruction file

## Stride Engine Context (Quick Reminders)
- ECS: Entities aggregate Components (Transform, Model, Camera, Rigidbody, Scripts, etc.).
- Entities must be added to a Scene graph to be processed.
- Physics: Prefer Bepu components; Bullet retained only for transition/testing. Avoid mixing both on the same entity.
- Core components commonly manipulated: Transform (position/rotation/scale), Camera, Rigidbody, Script logic.

## Toolkit Patterns
### Extension Method Pattern
```csharp
entity.Add3DCameraController()
      .AddGizmo(graphicsDevice)
      .SetPosition(Vector3.UnitY);
```
Guidelines:
- Return the modified instance (fluent chaining) where it’s natural.
- Validate inputs early (`ArgumentNullException.ThrowIfNull`).
- Avoid burying heavy allocations or long-running work in simple-sounding extension names.

### Code-Only Scene Creation
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

## Coding & Contribution Guidelines
- Use latest C# features (file-scoped namespaces, target-typed `new`, pattern matching, spans where beneficial).
- Keep nullable reference warnings at zero.
- Public APIs: complete XML docs (`<summary>`, `<param>`, `<returns>`, `<example>` when useful). Mark experimental with a note: “STATUS: Preview – subject to change”.
- Naming: `Stride.CommunityToolkit.<LibraryName>` for new libs; PascalCase for types/methods; camelCase for parameters.
- Terminology / capitalization: Use “Bepu” (capital B only) in identifiers and XML docs; never “BEPU” or “bepu”. Use “Bullet” (capital B) for Bullet physics.
- One public type per file; avoid unrelated multi-class files.
- No `#region` blocks; rely on clear structure.
- Validation: `ArgumentNullException.ThrowIfNull()`; meaningful exception messages for invalid states.
- Suggestion preference order:
  1. Existing reviewed toolkit extension/helper
  2. New small, composable extension (documented) 
  3. Direct Stride API usage
  4. External snippet (must justify + ensure license compatibility)
- Performance:
  - Cache frequently used component references inside update loops.
  - Avoid per-frame allocations (consider pooling / struct patterns where appropriate).
  - Dispose GPU/graphics resources deterministically (`using` / `Dispose`).
- Threading: Mutations to scene graph, entities, components, graphics resources must occur on main thread.
- Physics: Don’t combine Bepu and Bullet physics components on same entity.
- Shaders: After adding/removing/changing shader properties, manually regenerate the associated `*.cs` file (AI should remind contributors).
- Experimental / provisional APIs: consider an `[Experimental]` attribute (future) or note in summary.
- Tests: Keep deterministic; avoid relying on real-time frame counts.

## Documentation Guidelines
- Docs generated with DocFX from `docs/`.
- Update conceptual docs + XML comments when changing public APIs.
- New libraries: update navigation, TOC, and contributing guides (`docs/contributing/toolkit/library-project.md`).
- Provide concise, runnable examples; reduce noise/boilerplate.

## Verification & Provenance
- Imported code from external/community sources must have:
  - Compatible license (or original author permission).
  - Normalized naming/patterns to match toolkit style.
  - XML docs added or improved.
- Refactor legacy “static manager” style toward extension-based or instance-centric patterns.
- Mark unclear logic or magic numbers with `// TODO:` plus an issue link.

## Common Code-Only Example Pattern
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

## AI Assistance Guidance
- Improve or extend existing helpers instead of duplicating similar logic.
- Do NOT introduce unrelated frameworks/patterns (e.g., Unity managers, large DI containers, Rx) unless explicitly requested.
- Highlight potential breaking changes when modifying public APIs (Preview status).
- Prefer Bepu examples over Bullet unless addressing migration or legacy parity.
- Remind about shader code regeneration if shaders are touched.
- For Blazor content: keep solutions Blazor-appropriate; avoid server-only MVC/Razor patterns unless necessary.
- Avoid speculative APIs; ground suggestions in existing patterns.

## Maintenance
> [!IMPORTANT]
> Keep this document current (architectural shifts, new subsystems, deprecations) so AI assistance stays accurate.

- Update on structural/convention changes.
- Prune outdated or redundant guidelines.
- Add new exceptions/patterns explicitly.
- Revisit after introducing new physics systems, rendering pipelines, or scripting paradigms.

## Quick Checklist (Before Merging)
- [ ] XML docs complete / updated
- [ ] Nullability warnings resolved
- [ ] No unnecessary allocations in hot paths
- [ ] Fluent extensions return `this` where appropriate
- [ ] Examples updated (if API changes)
- [ ] Conceptual + API docs updated
- [ ] Preview / experimental status noted (if relevant)
- [ ] Shader regeneration reminder (if shaders changed)
- [ ] Provenance clarified for imported code

---
If something here becomes outdated or ambiguous, update it promptly—concise, accurate guidance improves AI output quality and reduces maintenance overhead.