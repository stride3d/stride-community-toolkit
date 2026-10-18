---
generated: true
slug: capsule-with-rigid-body-fs
---

# Capsule with rigid body in F#

The first code-only scene written in F#. What the C# version does - base scene, skybox, one
capsule - plus a profiler, with the differences F# forces you to be explicit about: helpers that return
a value must be piped to ignore, assignment to a mutable property uses the left arrow, and the game
loop is started from a real main function marked with EntryPoint.

The `Program.fs` file shows how to:

- Writing a code-only Stride app in F#
- Discarding unwanted return values with |> ignore
- Assigning to engine properties with the <- operator
- Starting the loop from an [<EntryPoint>] main function
- Passing the start callback by name: game.Run(start = Start)
- Using helpers: SceneSetup3D, UseBepu, UseSkybox, AddProfiler, CreatePrimitive

![Capsule with rigid body in F#](media/capsule-with-rigid-body-fs.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E01_3D_BasicScene_FSharp).

[!code-fsharp[](../../../../examples/code-only/E01_3D_BasicScene_FSharp/Program.fs?start=1&end=20)]