# Capsule with rigid body in F#

This code example demonstrates how to initialize a game, set up a basic 3D scene, create a 3D capsule entity, set its position, and add it to the scene using the Stride Game Engine. The capsule entity comes automatically equipped with a rigid body and a collider, thanks to the `CreatePrimitive()` method. It's a simple example that provides a starting point for building a game using Stride

*ToDo: Add a screenshot*

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example05_FSharp_Basic3DScene).

[!code-fsharp[](../../../../examples/code-only/Example05_FSharp_Basic3DScene/Program.fs)]

- `let game = new Game()` Creates a new instance of the `Game` class, serving as the central part of the Stride engine for managing game loop, scenes, and entities.
- `let Start rootScene =` Defines a function named `Start` that takes a `Scene` object named `rootScene` as an argument.
- `game.SetupBase3DScene()` Sets up a basic 3D scene with a default camera, lighting, and skybox.
- `game.AddProfiler() |> ignore` Adds a profiler to the game and discards the unneeded return value.
- `let firstBox = game.CreatePrimitive(PrimitiveModelType.Capsule);` Creates a new 3D capsule primitive entity.
- `firstBox.Transform.Position <- new Vector3(0f, 2.5f, 0f)` Sets the 3D position of the created entity.
- `firstBox.Scene <- rootScene` Adds the entity to the `rootScene`.
- `[<EntryPoint>]` Specifies that the following `main` function is the entry point of the application.
- `let main argv =` Defines the main function, which will be the entry point for the application.
- `game.Run(start = Start)` Initiates the game loop by passing the `Start` function as the `start` delegate.
- `0` Indicates a successful program execution.