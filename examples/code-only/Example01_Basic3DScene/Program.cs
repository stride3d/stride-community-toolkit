using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();

    var material = game.CreateMaterial(Color.DarkGoldenrod);

    var entity = game.CreatePrimitive(PrimitiveModelType.Capsule, material: material);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
});