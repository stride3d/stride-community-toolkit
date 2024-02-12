using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.CreatePrimitive(PrimitiveModelType.Cube);

    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);

    entity.Scene = rootScene;
}