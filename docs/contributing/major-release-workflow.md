# Major Release Workflow

Preparing for a major release, such as upgrading from **.NET 8** to **.NET 9**, involves updating several key areas to ensure a seamless transition. This guide outlines the necessary steps to update the toolkit and associated documentation.

## Steps for Major Release:

1. **Update Documentation References**:
   - Update `.NET` references on the Home page (`index.md`).
   - Update `.NET` references in `manual\gettings-started.md`.
   - Update `.NET` references in `manual\code-only\create-project.md`.
2. **Update Project Files**:
   - Update the `TargetFramework` in all `.csproj` files.
   - Update the `TargetFramework` in `docs\docfx.json`.
   - Update the version in `docs\docfx.json`, under `build:globalMetadata:_appFooter`.
3. **Update CI/CD Workflows**:
   - Update the `dotnet-version` in:
     - `.github\workflows\dotnet.yml`.
     - `.github\workflows\dotnet-nuget.yml`.
4. **Test Examples**:
   - Test all examples to ensure they function as expected with the new framework version.
    

By following these steps, you ensure the toolkit is fully updated, well-documented, and thoroughly tested for each major release, providing users with a reliable and smooth experience.