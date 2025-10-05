using Example02_GiveMeACube;
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
    game.SetupBase3DScene();
    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube);

    entity.Transform.Position = new Vector3(0f, 0.5f, 0);
    entity.Scene = rootScene;

    var orbitingEntity = game.Create3DPrimitive(PrimitiveModelType.Cube, new() { IncludeCollider = false });

    orbitingEntity.Transform.Position = new Vector3(0, 0.5f, 0);
    orbitingEntity.Add(new RotationComponentScript());
    orbitingEntity.Scene = rootScene;
}