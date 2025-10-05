using Example01_Basic3DScene_SyncScript;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube);
    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);
    entity.Add(new RotationComponentScript());
    entity.Scene = scene;

    var entityCone = game.Create3DPrimitive(PrimitiveModelType.Cone, new() { Size = new(0.5f, 5, 0) });
    entityCone.Transform.Position = new Vector3(0, 6, 0);
    entityCone.Scene = scene;
}