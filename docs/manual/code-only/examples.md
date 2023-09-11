# Code Only Examples

You can either copy and paste the code snippets from the examples below into your own project or run them directly using our console app with an interactive menu.

The console application providing this interactive menu is part of the `Stride.CommunityToolkit.Examples` project.


```plaintext
Stride Community Toolkit Examples

[1] Basic Example - Capsule with rigid body
[2] Basic Example - Give me a cube
[3] Basic Example - Capsule with rigid body and window
[Q] Quit
```

## Basic Example - Capsule with rigid body

This code example demonstrates how to initialize a game, set up a basic 3D scene, create a 3D capsule entity, set its position, and add it to the scene using the Stride Game Engine. The capsule entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method. It's a simple example that provides a starting point for building a game using Stride

[!code-csharp[](../../../examples/code-only/Example01_Basic3DScene/Program.cs)]

- `using var game = new Game();` This line of code creates a new instance of the `Game` class. The `Game` class is the central part of the Stride engine, managing the overall game loop, the scenes, and the updates to the entities. The `using` keyword ensures that the `Dispose()` method is called on the `game` object when it goes out of scope, ensuring that any resources it uses are properly cleaned up
- `game.Run(start: (Scene rootScene) =>` This line initiates the game loop. The `Run` method is responsible for starting the game, and it takes a delegate as a parameter. This delegate is a function that is called once when the game starts. The `rootScene` parameter represents the main scene of your game.
- `game.SetupBase3DScene();` This line sets up a basic 3D scene. It's a helper method provided to quickly set up a scene with a default camera, lighting, and skybox.
- `var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);` Here, a new entity is created in the form of a 3D capsule primitive. The `CreatePrimitive method` is another helper method provided to create basic 3D shapes.
- `entity.Transform.Position = new Vector3(0, 8, 0);` This line sets the position of the created entity in the 3D space. The `Position` property of the `Transform` component determines the location of the entity.
- `entity.Scene = rootScene;` Finally, the entity is added to the `rootScene`. The `Scene` property of an entity determines which scene it belongs to.

## Basic Example - Give me a cube

This example demonstrates the essential steps to create a 3D cube in Stride. Just like the previous example, the cube entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method. The cube is positioned at `(1f, 0.5f, 3f)` in the 3D world space. This example is perfect for those who are new to 3D game development with Stride.

[!code-csharp[](../../../examples/code-only/Example02_GiveMeACube/Program.cs)]

## Basic Example - Capsule with rigid body and window

In this example, we demonstrate how to set up a 3D scene that includes a capsule with a rigid body as well as a simple window displaying a text message.

This example is organized into multiple methods for better readability and maintainability. It is structured as follows:

- `Start(Scene rootScene)` This is the entry point for setting up the scene. It calls other methods to set up the 3D scene, add the capsule, load the font, and add the window.
- `AddCapsule(Scene rootScene)` This method creates a 3D capsule and adds it to the scene at a specific position.
- `LoadFont()` This method loads the font that will be used for the UI window.
- `AddWindow(Scene rootScene)` This method calls `CreateUIEntity()` to create an entity with a UI component, and then adds this entity to the root scene.
- `CreateUIEntity()` This method creates an entity that has a UI component. The UI component includes a canvas as its root element.
- `CreateCanvas()` This method creates a canvas element that will be the root of the UI component.
- `CreateTextBlock(SpriteFont? _font)` This method creates a `TextBlock` element that displays the message "Hello, World". It uses the loaded font and sets other properties like color and size.

This modular approach makes the code easier to understand and maintain. Each method has a clear responsibility.

[!code-csharp[](../../../examples/code-only/Example03_CapsuleAndWindow/Program.cs)]