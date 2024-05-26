# Capsule with rigid body in F#

[!INCLUDE [capsule-with-rigid-body](../../../includes/manual/examples/capsule-with-rigid-body.md)]

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example01_Basic3DScene_FSharp).

[!code-fsharp[](../../../../examples/code-only/Example01_Basic3DScene_FSharp/Program.fs)]

- `let game = new Game()` Creates a new instance of the `Game` class, serving as the central part of the Stride engine for managing game loop, scenes, and entities.
- `let Start rootScene =` Defines a function named `Start` that takes a `Scene` object named `rootScene` as an argument.
- `game.SetupBase3DScene()` Sets up a basic 3D scene with a default camera, lighting.
- `game.AddSkybox()` Adds a skybox to the scene, providing a background image for the 3D environment.
- `game.AddProfiler() |> ignore` Adds a profiler to the game and discards the unneeded return value.
- `let firstBox = game.Create3DPrimitive(PrimitiveModelType.Capsule);` Creates a new 3D capsule primitive entity.
- `firstBox.Transform.Position <- new Vector3(0f, 2.5f, 0f)` Sets the 3D position of the created entity.
- `firstBox.Scene <- rootScene` Adds the entity to the `rootScene`.
- `[<EntryPoint>]` Specifies that the following `main` function is the entry point of the application.
- `let main argv =` Defines the main function, which will be the entry point for the application.
- `game.Run(start = Start)` Initiates the game loop by passing the `Start` function as the `start` delegate.
- `0` Indicates a successful program execution.