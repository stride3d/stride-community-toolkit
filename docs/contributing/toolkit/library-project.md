# Creating a New Library Project

## Steps to create a new NuGet library

1. Create the project
   - Add a new project under `src`, following the naming convention `Stride.CommunityToolkit.<LibraryName>`.
   - Refer to the [existing libraries](https://github.com/stride3d/stride-community-toolkit/tree/main/src) for folder structure consistency.
2. Configure the project file
   - Update the `.csproj` with correct package metadata.
   - Review existing library projects to ensure all necessary properties (e.g., `Title`, `Description`, `PackageTags`) are included.
3. Update documentation
   - Add the new library's name and description to `docs/includes/libraries.md`. This displays the library on:
     - The home page
     - The Getting Started page
4. Generate API documentation
   - Update `docs/docfx.json` to include the new `.csproj` so the [API documentation](../../api/index.md) is generated for the library.
5. Update CI/CD workflows
   - Add the project to:
     - `.github/workflows/dotnet-build-test.yml` (`PROJECTS`) - otherwise pull requests never build it
     - `.github/workflows/dotnet-nuget.yml` (`PACK_PROJECTS`) - otherwise it is never published
6. Update the local package feed
   - Add the project to the `projects` array in [`build/pack-local.cs`](building.md), which mirrors
     `PACK_PROJECTS`. Without it the library is missing from the local feed and cannot be tested the
     way a real consumer uses it.
7. If the library ships `EntityComponent` types
   - Follow [Making components work in Game Studio](game-studio-components.md). A component works in
     code-only projects long before it works in the editor, so this is easy to skip and hard to
     notice: the three requirements each fail silently, and differently.
8. Optional: add example projects
   - If adding examples, follow the existing folder structure in `examples`.
9. Optional: add guidance content
   - If you plan to include guides or tutorials:
     - Add new pages to `docs/manual`.
     - Update the `toc.yml` to link the new content.

> [!IMPORTANT]
> Seven places have to know about a new library: the solution, both workflows, `pack-local.cs`,
> `docfx.json`, `libraries.md`, and - if it ships components - its own `Module.cs`. Every one of them
> fails quietly when forgotten, so work down this list rather than trusting a clean build.

> [!TIP]
> Reach out to maintainers anytime, process improvements, clarifications, or code reviews, we're happy to help!
