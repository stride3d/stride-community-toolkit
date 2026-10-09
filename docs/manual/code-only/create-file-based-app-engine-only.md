# Create a File-Based App - Engine Only

The [Create a File-Based App](create-file-based-app.md) page runs a toolkit scene from a single `.cs`
file. This is the same idea with no toolkit package: a ground, a cube and a camera against Stride alone,
so you can see what the toolkit's helpers stand for. The [project version](create-project-engine-only.md)
walks through the code and maps each part to the helper it replaces; this page is only what changes
when the app is a single file.

## Steps

1. Create a folder for the app and go into it - one app per folder, for the reason on the
   [file-based app page](create-file-based-app.md#one-app-per-folder).
2. Create `Program.cs` with these directives at the top, then the code from the
   [project version](create-project-engine-only.md#the-code) after them:

   ```csharp
   #:package Stride.Engine@4.4.0
   #:package Stride.AssetCompiler@4.4.0
   #:property PublishAot=false
   ```

   Use the Stride version you are on. `Stride.Engine` is the runtime; `Stride.AssetCompiler` is the
   build step that compiles the engine's shaders into the output, without which the first frame
   fails with `Shader ShaderBase could not be found`. `PublishAot=false` is there because file-based
   apps publish as native AOT by default and Stride is not AOT-compatible.
3. Create `Directory.Build.targets` next to it - the second file, and the reason this page exists:

   ```xml
   <Project>
     <ItemGroup>
       <PackageReference Update="Stride.AssetCompiler" IncludeAssets="build;buildTransitive" />
     </ItemGroup>
   </Project>
   ```
4. Run it:
   ```
   dotnet run Program.cs
   ```

## Why the second file

The asset compiler has to be referenced build-only. As a plain reference its own assemblies are
copied next to the app, and the app fails at start-up with
`Could not load file or assembly 'Stride.NuGetResolver'`. A `.csproj` says build-only with
`IncludeAssets="build;buildTransitive"` on the reference; a `#:package` directive has no way to, so
a `Directory.Build.targets` beside the file says it instead - a file-based app imports one like any
project. The toolkit's file-based apps do not need this because `Stride.CommunityToolkit.Windows`
already passes the asset compiler on build-only; this is the one thing the toolkit does on the
packaging side, and the reason the engine-only version is not quite a single file.

## See it in the repository

[`ProgramEngineOnly.cs`](https://github.com/stride3d/stride-community-toolkit/blob/main/examples/code-only/E01_3D_BasicScene_FileBasedApp/ProgramEngineOnly.cs)
is this app in the toolkit's repository, in the examples folder
`examples/code-only/E01_3D_BasicScene_FileBasedApp`, next to the toolkit versions of the same scene.
Two things differ from the steps above, both because the file lives inside the repository:

- It writes the version as `@$(StrideVersion)`, which the repository's `Directory.Build.props`
  resolves. Outside the repository write the version number.
- The [`Directory.Build.targets`](https://github.com/stride3d/stride-community-toolkit/blob/main/examples/code-only/E01_3D_BasicScene_FileBasedApp/Directory.Build.targets)
  beside it has one more line, an `Import` of the examples folder's own `Directory.Build.targets`.
  MSBuild imports only the nearest file of that name, so a folder that adds one has to pass the
  outer one on. Your own app has no outer file and does not need the line.

That `Directory.Build.targets` is read by all three apps in the folder, but it only changes the
engine-only one: it updates a direct reference to `Stride.AssetCompiler`, and the two toolkit apps
have none. Run the engine-only app with:

```
dotnet run examples/code-only/E01_3D_BasicScene_FileBasedApp/ProgramEngineOnly.cs
```