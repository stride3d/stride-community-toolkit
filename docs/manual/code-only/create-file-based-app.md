# Create a File-Based App

A .NET 10 **file-based app** is a single `.cs` file with no `.csproj`: the packages it needs are declared
at the top of the file with `#:` directives, and `dotnet run Program.cs` builds and runs it. For a Stride
code-only project that is the shortest possible route from an empty folder to a window with a scene in
it, and it is a natural fit for experiments, teaching snippets and bug reproductions.

The regular [Create Project](create-project.md) steps still apply when you want a `.csproj` - an IDE
solution, several files, content that needs copying to the output. `dotnet project convert` turns a
file-based app into exactly that project when it outgrows one file, so nothing is lost by starting here.

> [!NOTE]
> Requires the .NET 10 SDK. Visual Studio does not build file-based apps; run them from the command
> line or the Visual Studio Code terminal. Everything else in [Getting Started](../getting-started.md)
> applies.
>
> C# only. File-based apps are a C# compiler feature - `dotnet run Program.fs` does not recognise the file, and an F# script under `dotnet fsi` cannot run Stride because the asset compiler only runs as part of a project build. For F# and Visual Basic create a project as in [Create Project](create-project.md); the [F# examples](examples/basic-examples-fs.md) and [VB examples](examples/basic-examples-vb.md) show the code.

## Steps

1. Create a folder for the app and go into it. One app per folder - the reason is below.
   ```
   mkdir MyStrideApp
   cd MyStrideApp
   ```
2. Create `Program.cs` with the directives and the scene:

   [!code-csharp[](../../../examples/code-only/E01_3D_BasicScene_FileBasedApp/ProgramSimple.cs)]

   Use the toolkit version that matches your Stride - see the table in [Getting Started](../getting-started.md).
3. Run it:
   ```
   dotnet run Program.cs
   ```
   The first run restores the packages and compiles the engine's shaders, so it takes a while; later
   runs are quick.

The scene is the same one the [Create Project](create-project.md#example-code) page walks through line
by line.

## What the directives do

| Directive | Meaning |
|---|---|
| `#:package Name@Version` | A NuGet package reference. `Stride.CommunityToolkit.Windows` brings in Stride itself and the asset compiler; `Bepu` adds physics; `Skyboxes` adds `AddSkybox()`. Use `Stride.CommunityToolkit.Linux` instead of `Windows` on Linux. |
| `#:property Name=Value` | An MSBuild property, as if written in a `.csproj`. `PublishAot=false` is there because file-based apps publish as native AOT by default and Stride is not AOT-compatible; without it every build carries the trimming and AOT analyser warnings. |
| `#:project ../path/To.csproj` | A project reference. The toolkit's own [file-based example](examples/file-based-app.md) uses this to build against the toolkit source instead of a package. |

## One app per folder

Stride's asset compiler collects shaders and assets from the app's directory downwards. A file-based app
sitting in a folder with other Stride code sees all of it as its own - the toolkit's examples folder,
for instance, then fails on two examples that ship shaders with the same name. Give each app its own
folder and it never comes up.

## Where the build output goes

There is no `obj/` or `bin/` next to the file: the SDK keeps them in its own temporary build cache,
which is what keeps the folder to one file. Stride 4.4 handles that layout (its asset compiler used to
build a malformed path from it in early 4.4 previews; that is fixed in 4.4.0-beta5). If you ever need
the output next to the file - to inspect the compiled `data/` folder, say - two directives put it there:

```csharp
#:property BaseIntermediateOutputPath=obj\
#:property OutputPath=bin\
```

## Growing into a project

```
dotnet project convert Program.cs
```

writes a `.csproj` next to the file with the same packages and properties, and removes the `#:` lines.
From there it is a normal code-only project: open it in an IDE, add files, add it to a solution.

## See it in the toolkit

`examples/code-only/E01_3D_BasicScene_FileBasedApp/Program.cs` is this scene as a file-based app,
built against the toolkit source with `#:project` references; the [example page](examples/file-based-app.md)
describes it. Because it has no `.csproj` it is not part of the solution and is run from the command line.

`ProgramEngineOnly.cs` in the same folder is the scene with no toolkit package at all - see
[Create a File-Based App - Engine Only](create-file-based-app-engine-only.md).