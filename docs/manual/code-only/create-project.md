# Create Project

## Visual Studio Code Instructions

1. **Create a Console App:** Follow the [Microsoft tutorial](https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio-code?pivots=dotnet-6-0) to create a new console application.
   ```
   dotnet new console --framework net6.0
   ```
1. **Add NuGet Package:** Execute the following command to add the necessary NuGet package.
   ```
   dotnet add package Stride.CommunityToolkit --prerelease
   ```
1. **Update Program.cs:** Paste the example code (provided below) into your `Program.cs` file.
1. **Run the Project:** Execute the following command.
   ```
   dotnet run
   ```
1. **Enjoy Stride:** If everything is set up correctly, you should now be able to run and enjoy your Stride project.

<!-- If you experience any issue (timestamping certificate) adding this package, try again (weird, isn't it?)-->

## Visual Studio 2022 and Rider Instructions
 
1. **Create a C# Console Application:** Open Visual Studio 2022 or Rider and create a new C# Console Application targeting .NET 6.
1. **Add NuGet Package:** Search for and add the **Stride.CommunityToolkit** NuGet package, ensuring you opt for the pre-release version.
   - This package will install all needed Stride NuGet packages
1. **Update Program.cs:** Paste the example code (provided below) into your `Program.cs` file.
1. **Run the Project:** Build and run your project using the IDE's run functionality.
1. **Enjoy Stride:** If everything is set up correctly, you should now be able to run and enjoy your Stride project.


## Example Code

The provided C# code example is designed to showcase the basic usage of the Stride Game Engine.


```csharp
using Stride.CommunityToolkit.Extensions;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}
```

1. `using var game = new Game();` creates a new instance of the `Game` class.
1. The `game.Run(start: Start);` line starts the game, and it specifies that the `Start` method should be called when the game begins.
1. `void Start(Scene rootScene)` is the method that is called when the game starts. It takes in a `Scene` object, which represents the game scene that is currently being played.
1. Inside the `Start` method, `game.SetupBase3DScene();` sets up a basic 3D scene.
1. `var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);` creates a new primitive entity of type `Capsule`, and assigns it to the `entity` variable.
1. `entity.Transform.Position = new Vector3(0, 8, 0);` sets the position of the entity in the 3D space. The position is set to (0, 8, 0), which means the capsule is placed 8 units above the ground.
1. `entity.Scene = rootScene;` adds the entity to the root scene of the game.


The `CreatePrimitive()` method creates a Capsule with [rigid body physics](https://doc.stride3d.net/latest/en/manual/physics/rigid-bodies.html). Because the capsule is placed 8 units above the ground, it will fall due to gravity. Note that it's important to remove the capsule from memory once it's no longer visible in the scene, to free up resources and ensure the CPU isn't unnecessarily calculating physics for it

![image](https://user-images.githubusercontent.com/4528464/180097697-8352e30c-3750-42f1-aef9-ecd6c8e6255e.png)