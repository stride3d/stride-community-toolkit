# Major Release Workflow

Preparing for a major release, such as upgrading from **.NET 8** to **.NET 9**, involves updating several key areas to ensure a seamless transition. This guide outlines the necessary steps to update the toolkit and associated documentation.

## Steps for a major release

1. **Update Documentation References**:
   - Update `.NET` references on the Home page (`index.md`).
   - Update `.NET` references in `manual\getting-started.md`.
   - Update `.NET` references in `manual\code-only\create-project.md`, `create-project-engine-only.md`, `create-file-based-app.md` and `create-file-based-app-engine-only.md`.
2. **Update Project Files**:
   - Update `TargetFramework` in `Directory.Build.props` and `src\CommonSettings.props`; no `.csproj` sets it.
   - Update the `TargetFramework` in `docs\docfx.json`.
   - Update the version in `docs\docfx.json`, under `build:globalMetadata:_appFooter`.
3. **Update CI/CD Workflows**:
   - Update the `dotnet-version` in:
     - `.github\workflows\dotnet-build-test.yml`.
     - `.github\workflows\gold-images.yml`.
     - `.github\workflows\dotnet-nuget.yml`.
     - `.github\workflows\github-pages.yml`.
     - `.github\workflows\bump-stride-nuget-packages.yml`.
4. **Test Examples**:
   - Test all examples to ensure they function as expected with the new framework version.
    

By following these steps, you ensure the toolkit is fully updated, well-documented, and thoroughly tested for each major release, providing users with a reliable and smooth experience.