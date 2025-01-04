open Stride.CommunityToolkit.Bepu;
open Stride.CommunityToolkit.Engine;
open Stride.CommunityToolkit.Skyboxes;
open Stride.CommunityToolkit.Rendering.ProceduralModels;
open Stride.Core.Mathematics;
open Stride.Engine;

let game = new Game()

let Start rootScene =
    game.SetupBase3DScene()
    game.AddSkybox() |> ignore
    game.AddProfiler() |> ignore

    let firstBox = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    firstBox.Transform.Position <- new Vector3(0f, 2.5f, 0f)
    firstBox.Scene <- rootScene

[<EntryPoint>]
let main argv =
    game.Run(start = Start)
    0