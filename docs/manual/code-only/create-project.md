# Create Project

## Command line and Visual Studio Code

The steps below show how to create a new Stride project using the command line. If you prefer Visual Studio Code, run the same commands from its integrated terminal. For the shortest possible start - one `.cs` file, no project file at all - see [Create a File-Based App](create-file-based-app.md).

> [!NOTE]
> Match the toolkit version to your Stride version - the table is in [Getting Started](../getting-started.md). `--prerelease` takes the newest preview, so on an older Stride pin it, for example `--version 1.0.0-preview.62` for Stride 4.3.

# [Windows](#tab/windows)

1. Prerequisites: Make sure all prerequisites are installed. See [Getting started](../getting-started.md) for details.
2. Create a console app. You can follow the official [Microsoft tutorial](https://learn.microsoft.com/en-gb/dotnet/core/tutorials/with-visual-studio-code) or run:
   ```
   dotnet new console --framework net10.0 --name YourProjectName
   ```
3. Navigate to the project folder:
   ```
   cd YourProjectName
   ```
4. Add the toolkit package. It brings in the Stride packages and the asset compiler build step:
   ```
   dotnet add package Stride.CommunityToolkit --prerelease
   ```
5. Add the Bepu physics package:
   ```
   dotnet add package Stride.CommunityToolkit.Bepu --prerelease
   ```
6. Update Program.cs: Paste the [example code](#example-code) below into your Program.cs file.
7. Build (optional): `dotnet run` performs an implicit build, but you can build explicitly:
   ```
   dotnet build
   ```
8. Run the project:
   ```
   dotnet run
   ```

# [Linux](#tab/linux)

1. Prerequisites: the .NET 10 SDK. Microsoft's build (`dotnet-install.sh` or the Microsoft package feed) needs nothing more; a distribution's own `dotnet` package works too, with the Linux package of step 6.
2. Create a console app:
   ```
   dotnet new console --framework net10.0 --name YourProjectName
   ```
3. Navigate to the project folder:
   ```
   cd YourProjectName
   ```
4. Tell the build it targets Linux. Add this line to the `<PropertyGroup>` of `YourProjectName.csproj`; Stride reads the platform from it and copies the Linux native libraries next to your app:
   ```xml
   <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
   ```
5. Add the toolkit package. It brings in the Stride packages and the asset compiler build step:
   ```
   dotnet add package Stride.CommunityToolkit --prerelease
   ```
6. Only with a distribution's `dotnet` package, add the Linux package. The distribution's build reports a runtime identifier the asset compiler does not map, so without this package no native library is found; the package copies them into place. The distribution's older Roslyn also skips Stride's source generators without an error, which Microsoft's build does not.
   ```
   dotnet add package Stride.CommunityToolkit.Linux --prerelease
   ```
7. Add the Bepu physics package:
   ```
   dotnet add package Stride.CommunityToolkit.Bepu --prerelease
   ```
8. Update Program.cs: Paste the [example code](#example-code) below into your Program.cs file.
9. Build. On Stride 4.4 the first build of a project fails inside the asset compiler with NuGet signature errors, because the compiler inherits the SDK's signature verification without the SDK's trust store (reported upstream). Until that is fixed, turn the verification off for the build:
   ```
   DOTNET_NUGET_SIGNATURE_VERIFICATION=false dotnet build
   ```
   Later builds in the same project no longer need the variable.
10. Run the project:
   ```
   dotnet run
   ```

> [!TIP]
> To build for Linux from a Windows machine instead, as Game Studio does, replace the `RuntimeIdentifier` line with `<StridePlatform>Linux</StridePlatform>`, build on Windows, copy the output to the Linux machine and start it with `dotnet YourProjectName.dll`.

---

## Visual Studio 2026 and JetBrains Rider

1. Create a C# Console Application targeting .NET 10.
2. Add the `Stride.CommunityToolkit` NuGet package (pre-release). This pulls in the required Stride packages and the asset compiler.
3. Add the `Stride.CommunityToolkit.Bepu` package (pre-release) to include the Bepu physics integration.
4. Update Program.cs: Paste the example code below.
5. Run the project using your IDE.

## Example code

```csharp
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu(), start: Start);

void Start(Scene rootScene)
{
    var entity = game.CreatePrimitive(Primitive3D.Capsule());

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}
```

- `using var game = new Game();` creates a new `Game` instance.
- `game.Run(new SceneSetup3D().UseBepu(), start: Start);` sets up a basic 3D scene (camera, lighting, sky, ground) with Bepu physics, starts the game and calls `Start` when the scene is ready. Every piece of the setup is a property; see [Scene Setup](scene-setup.md).
- `void Start(Scene rootScene)` is invoked on start with the active root scene.
- `game.CreatePrimitive(Primitive3D.Capsule())` creates a capsule primitive. `Primitive3D.Capsule(radius, length)` sizes it.
- `entity.Transform.Position = new Vector3(0, 8, 0);` places the capsule 8 units above the ground.
- `entity.Scene = rootScene;` adds the entity to the scene so it is rendered and simulated. Without it the entity does not appear.

With `UseBepu()` on the setup, `CreatePrimitive()` gives the capsule a [rigid body](https://doc.stride3d.net/latest/en/manual/physics/rigid-bodies.html), so it falls to the ground.

[Create Project - Engine Only](create-project-engine-only.md) builds a ground, a cube and a camera against the engine alone, with no toolkit package, and maps each part to the helper it replaces.

> [!TIP]
> Remove entities you no longer need to free resources and avoid unnecessary physics updates.

![image](https://user-images.githubusercontent.com/4528464/180097697-8352e30c-3750-42f1-aef9-ecd6c8e6255e.png)

## Additional examples

Browse more examples in the navigation on the left, grouped by language and complexity.

- [C# Basic examples](examples/basic-examples.md)
- [C# Advanced examples](examples/advance-examples.md)
- [F# Basic examples](examples/basic-examples-fs.md)
- [VB Basic examples](examples/basic-examples-vb.md)