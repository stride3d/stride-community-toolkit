# New Library Project

### New NuGet Library

1. Create a project in the `src` folder, make sure to follow the naming convention `Stride.CommunityToolkit.<LibraryName>`. Check the [existing libraries](https://github.com/stride3d/stride-community-toolkit/tree/main/src) to see the folder structure pattern.
1. Update '.csproj' file with the correct package information. Check existing library project files and add missing properties.  
1. Update `includes/libraries.md` with a name and description of the new library. This will show up on:
    - The home page
    - Getings Started page
1. Update `docs/docfx.json` with the new `.csproj` location, so the [API](../../api/index.md) documentation is generated for the new library.

> [!TIP]
> Please feel free to reach out to the maintainers for help with the process, suggest better ways to do things, or ask for help with the code.