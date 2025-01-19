# Creating a New Library Project

## Steps to Create a New NuGet Library

1. **Create the Project**:
   - Add a new project in the `src` folder, following the naming convention: `Stride.CommunityToolkit.<LibraryName>`. 
   - Refer to the [existing libraries](https://github.com/stride3d/stride-community-toolkit/tree/main/src) to ensure consistency with the folder/subfolder structure.
2. **Configure the Project File**:
   - Update the `.csproj` file with the correct package metadata.
   - Review existing library project files to ensure all necessary properties (e.g., `Title`, `Description`, `Import`) are included.
3. **Update Documentation**:
   - Add the new library's name and description to `includes/libraries.md`. This will display the library on:
     - The home page.
     - The Getting Started page.
4. **Generate API Documentation**:
   - Update `docs/docfx.json` to include the new `.csproj` location, ensuring that the [API documentation](../../api/index.md) is generated for the new library.
5. **Update CI/CD Workflows**:
   - Add the new project to:
     - `.github\workflows\dotnet.yml`.
     - `.github\workflows\dotnet-nuget.yml`.
6. **Optional: Add Example Projects**:
   - If adding example projects, follow the existing folder structure pattern in the `examples` directory.
7. **Optional: Add Guidance Content**:
   - If you plan to include guidance or tutorials for the new library:
     - Add new pages to the `docs/manual` folder.
     - Update the `toc.yml` file to include links to the new content.

> [!TIP]
> Don't hesitate to reach out to the maintainers for assistance. Whether it's improving the process, code, clarifying steps, or helping with code, we're here to help!