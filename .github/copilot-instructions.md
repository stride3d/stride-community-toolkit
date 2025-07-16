# GitHub Copilot Custom Instructions for Stride Community Toolkit

This document provides specific guidance for GitHub Copilot when working on the Stride Community Toolkit repository. It serves as context for understanding the project structure, development workflow, and best practices.

## Project Overview
- The Stride Community Toolkit is a set of C# helpers and extensions for the [Stride Game Engine](https://www.stride3d.net/), targeting .NET 8
- The toolkit provides libraries, code-only examples, and documentation to simplify and accelerate Stride game development.
- The repo also includes a Blazor project for one of the examples; prioritize Blazor-specific solutions over Razor Pages or ASP.NET Core MVC.

## Repository Structure
- `src/`: Core toolkit libraries (e.g., Stride.CommunityToolkit, Stride.CommunityToolkit.Bepu, Stride.CommunityToolkit.Windows, etc.)
- `examples/`: Code-only and snippet-based example projects in C#, F#, and VB.
- `docs/`: Documentation source (DocFX), including manual, API reference, and contributing guides.
- `.github/`: CI/CD workflows, release, and configuration files.

## Coding & Contribution Guidelines
- Prefer latest C# and .NET features for code.
- Follow the naming convention: `Stride.CommunityToolkit.<LibraryName>` for new libraries.
- Add new libraries to documentation and CI/CD workflows as described in `docs/contributing/toolkit/library-project.md`.
- For examples, ensure discoverability and update the relevant documentation includes.
- Use .NET 8 as the default target unless otherwise specified.
- Prefer concise, well-documented, and idiomatic C# code.
- Do not use `#region` directives; prefer clear, self-documenting code.
- When refactoring, don't create multiple classes in a single file; keep each class in its own file.

## Documentation
- Documentation is generated with DocFX. All docs are in the `docs/` folder.
- Update documentation and API references when adding or changing features.
- See `docs/contributing/documentation/index.md` for DocFX usage.

## Special Notes
- Use and reference the latest .NET features where possible.
- When suggesting code, prefer toolkit helpers and extensions over raw Stride API usage.
- For new features, update this file as the project evolves.
- Prioritize Blazor-specific solutions when relevant.

## Maintenance

> [!IMPORTANT]  
> **Note to developers:** Please keep this document up to date as the project evolves. Update these instructions whenever you introduce new technologies, make architectural changes, or establish important new conventions. This ensures GitHub Copilot continues to provide relevant and accurate assistance.

- Review and update `.github/copilot-instructions.md` whenever the solution or project structure changes.
- Document any new patterns, best practices, or exceptions that Copilot should be aware of.
- Remove outdated guidance and clarify ambiguous instructions as needed.
- Refer to the [GitHub Copilot documentation](https://docs.github.com/en/copilot/customizing-copilot/adding-repository-custom-instructions-for-github-copilot) for details on customizing Copilot for your repository.