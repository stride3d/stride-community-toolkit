# Creating a New Library Project

## Steps to create a new NuGet library

1. Create the project
   - Add a new project under `src`, following the naming convention `Stride.CommunityToolkit.<LibraryName>`.
   - Follow the folder structure of the [existing libraries](https://github.com/stride3d/stride-community-toolkit/tree/main/src).
   - Add the project to `Stride.CommunityToolkit.slnx`.
2. Configure the project file
   - Set `Title` and `Description` in the `.csproj`, ending the description with `$(StrideVersionNote)` as the other libraries do.
   - Import `..\CommonSettings.props`, which supplies the rest of the package metadata: version, licence, authors, tags, icon, readme and SourceLink.
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
   - A library not yet in `PACK_PROJECTS` goes on `DocPaths.PackagesNotOnNuGet` in the examples
     metadata generator, so the pages of its examples say they run from a clone of the repository.
6. Update the local package feed
   - Add the project to the `projects` array in [`build/pack-local.cs`](building.md), which mirrors
     `PACK_PROJECTS`. Without it the library is missing from the local feed and cannot be tested the
     way a real consumer uses it.
7. If the library ships `EntityComponent` types
   - Follow [Making components work in Game Studio](game-studio-components.md). A component works in
     code-only projects without any of it, and each missing requirement fails silently in the editor.
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
> Reach out to the maintainers any time for process improvements, clarifications or code reviews.