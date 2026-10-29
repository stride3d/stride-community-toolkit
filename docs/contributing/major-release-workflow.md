# Major Release Workflow

A major release moves the toolkit to a new .NET version, such as **.NET 10** to **.NET 11**. It touches the documentation, the project settings and the CI workflows.

## Steps for a major release

1. **Update Documentation References**:
   - Update `.NET` references on the Home page (`index.md`).
   - Update `.NET` references in `manual\getting-started.md`.
   - Update `.NET` references in `manual\code-only\create-project.md`, `create-project-engine-only.md`, `create-file-based-app.md` and `create-file-based-app-engine-only.md`.
   - Update the `bin/Debug/net10.0` output paths in `contributing\toolkit\building.md` and `.github\copilot-instructions.md`.
2. **Update Project Files**:
   - Update `TargetFramework` in `Directory.Build.props` and `src\CommonSettings.props`.
   - Update the projects that set their own `TargetFramework`: `E04_GumUI`, `E01_3D_BasicScene_FSharp`, `E01_3D_BasicScene_VisualBasic` and `E07_3D_PartialTorus_FSharp`.
   - Update the `TargetFramework` in `docs\docfx.json`.
   - Update the version in `docs\docfx.json`, under `build:globalMetadata:_appFooter`.
3. **Update CI/CD Workflows**:
   - Update the `dotnet-version` in:
     - `.github\workflows\dotnet-build-test.yml`.
     - `.github\workflows\gold-images.yml`.
     - `.github\workflows\dotnet-nuget.yml`.
     - `.github\workflows\github-pages.yml`.
4. **Test Examples**:
   - Build the full solution, `Stride.CommunityToolkit.slnx`, which compiles the examples, and run them on the new framework version.