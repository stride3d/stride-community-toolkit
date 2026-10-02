open Stride.CommunityToolkit.Bepu
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Skyboxes
open Stride.CommunityToolkit.Rendering.ProceduralModels
open Stride.Core.Mathematics
open Stride.Engine

let game = new Game()

let Start rootScene =
    game.SetupBase3DScene()
    game.AddSkybox() |> ignore
    game.AddProfiler() |> ignore

    let capsule = game.Create3DPrimitive(PrimitiveModelType.Capsule, Bepu3DPhysicsOptions())
    capsule.Transform.Position <- new Vector3(0f, 2.5f, 0f)
    capsule.Scene <- rootScene

[<EntryPoint>]
let main argv =
    game.Run(start = Start)
    0
(*
---example-metadata
slug: capsule-with-rigid-body-fs
title:
  en: Capsule with rigid body in F#
level: Getting Started
category: Shapes
complexity: 1
order: 10
description:
  en: |-
    The first code-only scene written in F#. What the C# version does - base scene, skybox, one
    capsule - plus a profiler, with the differences F# forces you to be explicit about: helpers that return
    a value must be piped to ignore, assignment to a mutable property uses the left arrow, and the game
    loop is started from a real main function marked with EntryPoint.
concepts:
  - Writing a code-only Stride app in F#
  - "Discarding unwanted return values with |> ignore"
  - "Assigning to engine properties with the <- operator"
  - "Starting the loop from an [<EntryPoint>] main function"
  - "Passing the start callback by name: game.Run(start = Start)"
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler, Create3DPrimitive"
tags:
  - 3D
  - F#
  - Bepu
  - Shapes
  - Primitive
  - Capsule
  - Scene Setup
related:
  - E01_3D_BasicScene
  - E07_3D_PartialTorus_FSharp
tocName: Capsule with rigid body
enabled: true
created: 2023-09-30
---
*)