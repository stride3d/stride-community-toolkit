# Capsule with rigid body

This code example demonstrates how to initialize a game, set up a basic 3D scene, create a 3D capsule entity, set its position, and add it to the scene using the Stride Game Engine. The capsule entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method. It's a simple example that provides a starting point for building a game using Stride

*ToDo: Add a screenshot*

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example01_Basic3DScene).

[!code-csharp[](../../../../examples/code-only/Example01_Basic3DScene/Program.cs)]

- `using var game = new Game();` This line of code creates a new instance of the `Game` class. The `Game` class is the central part of the Stride engine, managing the overall game loop, the scenes, and the updates to the entities. The `using` keyword ensures that the `Dispose()` method is called on the `game` object when it goes out of scope, ensuring that any resources it uses are properly cleaned up
- `game.Run(start: (Scene rootScene) =>` This line initiates the game loop. The `Run` method is responsible for starting the game, and it takes a delegate as a parameter. This delegate is a function that is called once when the game starts. The `rootScene` parameter represents the main scene of your game.
- `game.SetupBase3DScene();` This line sets up a basic 3D scene. It's a helper method provided to quickly set up a scene with a default camera, lighting, and skybox.
- `var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);` Here, a new entity is created in the form of a 3D capsule primitive. The `CreatePrimitive method` is another helper method provided to create basic 3D shapes.
- `entity.Transform.Position = new Vector3(0, 8, 0);` This line sets the position of the created entity in the 3D space. The `Position` property of the `Transform` component determines the location of the entity.
- `entity.Scene = rootScene;` Finally, the entity is added to the `rootScene`. The `Scene` property of an entity determines which scene it belongs to.