using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3D();
    game.Add3DCameraController();
    //game.Add3DGround();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;

    var leftWall = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = new Vector3(1, 50, 1),
        Material = game.CreateMaterial(Color.LightGray)
    });
    leftWall.Transform.Position = new Vector3(-25, 0, 0);
    leftWall.Scene = rootScene;
}