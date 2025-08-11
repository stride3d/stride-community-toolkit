# GitHub Copilot Custom Instructions for Stride Community Toolkit

This document provides specific guidance for GitHub Copilot when working on the Stride Community Toolkit repository. It serves as context for understanding the project structure, development workflow, and best practices.

## Project Overview
- The Stride Community Toolkit is a comprehensive collection of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/), targeting .NET 8
- The toolkit provides libraries, code-only examples, and documentation to simplify and accelerate Stride game development
- The F# or VB.NET code-only examples are only a showcase of the toolkit's capabilities and not a primary focus
- Uses Stride Game Engine latest version with nullable reference types enabled
- The repo also includes a Blazor project for one of the examples; prioritize Blazor-specific solutions over Razor Pages or ASP.NET Core MVC
- The toolkit is designed to be used also in a regular Stride project created from the Game Studio template. The code-only examples are meant to demonstrate how to use the toolkit in a straightforward way without relying on the Game Studio UI.

## Repository Structure
- `src/`: Core toolkit libraries
  - **Stride.CommunityToolkit**: Core toolkit
    - `Engine/`: Game and Entity extensions
    - `Extensions/`: General-purpose extensions
    - `Graphics/`: Graphics utilities
    - `Helpers/`: Helper classes
    - `Mathematics/`: Math utilities (Easing, etc.)
    - `Physics/`: Physics extensions
    - `Rendering/`: Rendering utilities
    - `Scripts/`: Reusable script components
  - **Stride.CommunityToolkit.Bepu**: Bepu Physics integration
  - **Stride.CommunityToolkit.Bullet**: Bullet Physics integration  
  - **Stride.CommunityToolkit.DebugShapes**: Debug visualization tools
  - **Stride.CommunityToolkit.ImGui**: ImGui integration
  - **Stride.CommunityToolkit.Skyboxes**: Skybox utilities
  - **Stride.CommunityToolkit.Windows**: Windows-specific features
- `examples/`: Code-only and snippet-based example projects in C#, F#, and VB
- `docs/`: Documentation source (DocFX), including manual, API reference, and contributing guides
- `.github/`: CI/CD workflows, release, and configuration files

## Stride Game Engine Context
- **Entity-Component-System (ECS)**: Entity containers hold components (TransformComponent, ModelComponent, etc.)
- **Scene Management**: Entities must be added to scenes to be processed
- **Key Components**: Transform (position/rotation/scale), Model (3D mesh), Camera, Rigidbody (physics), Script (behavior)
- **Physics Engines**: Bepu Physics is the primary 3D physics engine, and Bullet Physics is also supported till it is deprecated

## Toolkit Patterns

### Extension Method Pattern
Extensions follow consistent naming and fluent API design:
```csharp
// Typical extension methods that support method chaining
entity.Add3DCameraController()
      .AddGizmo(graphicsDevice)
      .SetPosition(Vector3.UnitY);
```

### Code-Only Scene Creation
The toolkit showcases programmatic game development:
```csharp
using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene(); // Creates camera, lighting, ground
    game.AddSkybox();
    
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
});
```

## Coding & Contribution Guidelines
- **Prefer latest C# and .NET features** for code including nullable reference types, implicit usings, file-scoped namespaces
- **XML Documentation**: All public APIs must have comprehensive XML documentation with summary, param, and example tags
- **Extension Method Conventions**: Use PascalCase for classes/methods, camelCase for parameters, enable method chaining where appropriate
- **Validation**: Use `ArgumentNullException.ThrowIfNull()` for parameter validation
- **Follow the naming convention**: `Stride.CommunityToolkit.<LibraryName>` for new libraries
- **Add new libraries** to documentation and CI/CD workflows as described in `docs/contributing/toolkit/library-project.md`
- **For examples**, ensure discoverability and update the relevant documentation includes
- **Use .NET 8** as the default target unless otherwise specified
- **Prefer concise, well-documented, and idiomatic C#** code
- **Do not use `#region` directives**; prefer clear, self-documenting code
- **When refactoring**, don't create multiple classes in a single file; keep each class in its own file

## Documentation
- Documentation is generated with DocFX. All docs are in the `docs/` folder
- Update documentation and API references when adding or changing features
- See `docs/contributing/documentation/index.md` for DocFX usage

## Special Notes
- **Use and reference the latest .NET features** where possible
- **When suggesting code**, prefer toolkit helpers and extensions over raw Stride API usage
- **For new features**, update this file as the project evolves
- **Prioritize Blazor-specific solutions** when relevant
- **Performance**: Cache component references, avoid allocations in update loops, dispose graphics resources properly
- **Threading**: Most Stride operations must occur on the main thread
- **Validation**: Always validate input parameters and provide meaningful error messages
- **Shaders**: When you make changes to shaders, adding, updating or removing properties, ask the user to regenerate shader `*.cs` file manually otherwise the changes will not be reflected in the code

## Common Code-Only Example Usage Patterns
```csharp
// Typical 3D scene setup with toolkit extensions
using var game = new Game();
game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene(); // Camera, lighting, ground
    game.AddSkybox();
    
    // Create primitives with physics
    var entity = game.Create3DPrimitive(PrimitiveModelType.Sphere);
    entity.AddRigidBody(RigidBodyTypes.Dynamic);
    entity.Transform.Position = new Vector3(0, 10, 0);
    entity.Scene = rootScene;
});

```

## Maintenance

> [!IMPORTANT]  
> **Note to developers:** Please keep this document up to date as the project evolves. Update these instructions whenever you introduce new technologies, make architectural changes, or establish important new conventions. This ensures GitHub Copilot continues to provide relevant and accurate assistance.

- Review and update `.github/copilot-instructions.md` whenever the solution or project structure changes
- Document any new patterns, best practices, or exceptions that Copilot should be aware of
- Remove outdated guidance and clarify ambiguous instructions as needed
- Refer to the [GitHub Copilot documentation](https://docs.github.com/en/copilot/customizing-copilot/adding-repository-custom-instructions-for-github-copilot) for details on customizing Copilot for your repository