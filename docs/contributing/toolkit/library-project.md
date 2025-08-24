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
     - `.github/workflows/dotnet.yml` (`PROJECTS`)
     - `.github/workflows/dotnet-nuget.yml` (`env` and the `# Stride.CommunityToolkit.<LibraryName>` section)
6. Optional: add example projects
   - If adding examples, follow the existing folder structure in `examples`.
7. Optional: add guidance content
   - If you plan to include guides or tutorials:
     - Add new pages to `docs/manual`.
     - Update the `toc.yml` to link the new content.

> [!TIP]
> Reach out to maintainers anytime, process improvements, clarifications, or code reviews, we're happy to help!
