open Stride.CommunityToolkit.Bepu;
open Stride.CommunityToolkit.Engine;
open Stride.CommunityToolkit.Skyboxes;
open Stride.CommunityToolkit.Rendering.ProceduralModels;
open Stride.Core.Mathematics;
open Stride.Engine;
open Stride.CommunityToolkit.Rendering.Utilities
open Stride.Graphics
open System
open Stride.Rendering
open Stride.Rendering.Materials
open Stride.Rendering.Materials.ComputeColors

let game = new Game()

// Create material for our mesh
let CreateMaterial (game: Game) : Material =
    Material.New(
        game.GraphicsDevice,
        MaterialDescriptor(
            Attributes =
                MaterialAttributes(
                    MicroSurface =
                        MaterialGlossinessMapFeature(GlossinessMap = ComputeFloat(0.9f)),
                    Diffuse =
                        MaterialDiffuseMapFeature(DiffuseMap = ComputeColor(Color4(1.0f, 0.3f, 0.5f, 1.0f))),
                    DiffuseModel = MaterialDiffuseLambertModelFeature(),
                    Specular =
                        MaterialMetalnessMapFeature(MetalnessMap = ComputeFloat(0.0f)),
                    SpecularModel =
                        MaterialSpecularMicrofacetModelFeature(
                            Environment = MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
                        )
                )
        )
    )

// Create mesh entity from mesh builder
let CreateMeshEntity (graphicsDevice:GraphicsDevice) (scene:Scene) (position:Vector3) (build:MeshBuilder -> unit) =
    use meshBuilder = new MeshBuilder()

    build(meshBuilder)

    let model = new Model()
    model.Add(MaterialInstance(Material = CreateMaterial(game)))
    model.Add(Mesh(
        Draw = meshBuilder.ToMeshDraw(graphicsDevice),
        MaterialIndex = 0
    ))

    let entity = new Entity()
    entity.Scene <- scene
    entity.Transform.Position <- position
    entity.Add(ModelComponent(Model = model))
    entity

let BuildPartialTorusMesh (meshBuilder:MeshBuilder) cylRadius tAngle bendRadius nCircsegs nBendsegs =
    //for partial torus up to 360 degrees (tAngle in degrees)
    meshBuilder.WithIndexType(IndexingType.Int16) |> ignore
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList)  |> ignore

    let position = meshBuilder.WithPosition<Vector3>()
    let normal = meshBuilder.WithNormal<Vector3>()

    //vertices
    for j in 0..nBendsegs do
      //Torus angle position Phi starts at 0 in line with Z-axis and increases to Pi/2 at X-axis
      let tPhi = (float j) * tAngle / (float nBendsegs) * Math.PI / 180.0
      let xc = bendRadius * Math.Sin(tPhi)
      let zc = bendRadius * Math.Cos(tPhi)

      for i in 0..(nCircsegs - 1) do
      //circumference angle tTheta
        let tTheta = float i * Math.Tau / float nCircsegs
        let yr = cylRadius * Math.Sin(tTheta)

        let xr = cylRadius * Math.Cos(tTheta) * Math.Sin(tPhi)
        let zr = cylRadius * Math.Cos(tTheta) * Math.Cos(tPhi)

        let tNorm = Vector3(float32 xr, float32 yr, float32 zr)
        let tPos = Vector3(float32 (xc + xr), float32 yr, float32 (zc + zr))

        meshBuilder.AddVertex() |> ignore
        meshBuilder.SetElement(position, tPos)
        meshBuilder.SetElement(normal, tNorm)


    //triangle eles
    for j in 0..(nBendsegs - 1) do
      for i in 0..(nCircsegs - 1) do

        let i_tot = i + j * nCircsegs
        //build triangles
        let i_next = (i + 1) % nCircsegs + j * nCircsegs

        //triangle 1
        meshBuilder.AddIndex(i_tot)
        meshBuilder.AddIndex(i_next + nCircsegs)
        meshBuilder.AddIndex(i_tot + nCircsegs)


        //triangle 2
        meshBuilder.AddIndex(i_tot)
        meshBuilder.AddIndex(i_next)
        meshBuilder.AddIndex(i_next + nCircsegs)

let Start rootScene =
    game.SetupBase3DScene()
    game.AddSkybox() |> ignore
    game.AddProfiler() |> ignore

    // Torus parameters
    let cylRadius = 0.3f
    let tAngle = 270.0f
    let bendRadius = 1.0f
    let nCircsegs = 20
    let nBendsegs = 40

    // Create torus mesh entity
    CreateMeshEntity
        game.GraphicsDevice
        rootScene
        (Vector3(0.0f, 1.0f, 0.0f))
        (fun b -> BuildPartialTorusMesh b (float cylRadius) (float tAngle) (float bendRadius) nCircsegs nBendsegs)
    |> ignore

[<EntryPoint>]
let main argv =
    game.Run(start = Start)
    0