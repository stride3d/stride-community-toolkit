# Create Project

## Command line and Visual Studio Code

The steps below show how to create a new Stride project using the command line. If you prefer Visual Studio Code, run the same commands from its integrated terminal.

> [!NOTE]
> These instructions target Windows. While code-only projects can be built and run on Linux, the [setup is currently more involved](https://github.com/stride3d/stride/issues/2596). We're working to simplify it and will provide official guidance later.
>
> If you're still on Stride 4.2, use `--version 1.0.0-preview.61` instead of `--prerelease`, which targets Stride 4.3.

1. Prerequisites: Make sure all prerequisites are installed. See [Getting started](../getting-started.md) for details.
2. Create a console app. You can follow the official [Microsoft tutorial](https://learn.microsoft.com/en-gb/dotnet/core/tutorials/with-visual-studio-code) or run:
   ```
   dotnet new console --framework net10.0 --name YourProjectName
   ```
3. Navigate to the project folder:
   ```
   cd YourProjectName
   ``` 
4. Add toolkit package:
   ```
   dotnet add package Stride.CommunityToolkit.Windows --prerelease
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
9. Enjoy Stride: If everything is set up correctly, your project should run.

## Visual Studio 2026 and JetBrains Rider

1. Create a C# Console Application targeting .NET 10.
2. Add the `Stride.CommunityToolkit.Windows` NuGet package (pre-release). This pulls in the required Stride packages.
3. Add the `Stride.CommunityToolkit.Bepu` package (pre-release) to include the Bepu physics integration.
4. Update Program.cs: Paste the example code below.
5. Run the project using your IDE.

## Example code

The example demonstrates basic usage of the Stride Game Engine in a code-only app.

```csharp
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}
```

- `using var game = new Game();` creates a new `Game` instance.
- `game.Run(start: Start);` starts the game and calls `Start` when the game begins.
- `void Start(Scene rootScene)` is invoked on start with the active root scene.
- `game.SetupBase3DScene();` creates a basic 3D scene (camera, lighting, ground).
- `game.Create3DPrimitive(PrimitiveModelType.Capsule)` creates a capsule primitive.
- `entity.Transform.Position = new Vector3(0, 8, 0);` places the capsule 8 units above the ground.
- `entity.Scene = rootScene;` adds the entity to the scene so it is rendered and simulated. This step is crucial because assigning the entity to the scene ensures it is rendered and visible in the game. Without this assignment, the entity would not be part of the scene graph, and therefore, it would not appear in the game. 

`Create3DPrimitive()` creates a capsule with [rigid body physics](https://doc.stride3d.net/latest/en/manual/physics/rigid-bodies.html). Because it starts above the ground, it will fall due to gravity.

> [!TIP]
> Remove entities you no longer need to free resources and avoid unnecessary physics updates.

![image](https://user-images.githubusercontent.com/4528464/180097697-8352e30c-3750-42f1-aef9-ecd6c8e6255e.png)

## Additional examples

Browse more examples in the navigation on the left, grouped by language and complexity.

- [C# Basic examples](examples/basic-examples.md)
- [C# Advanced examples](examples/advance-examples.md)
- [F# Basic examples](examples/basic-examples-fs.md)
- [VB Basic examples](examples/basic-examples-vb.md)

Select any example to see a short description and code snippets.
