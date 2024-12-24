using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var type = PrimitiveModelType.Capsule;

    var entity = game.Create3DPrimitive(type);

    entity.Transform.Position = new Vector3(0, 8, 0);

    var body = entity.Get<BodyComponent>();
    body.Awake = true;

    entity.Scene = rootScene;

    var entity2 = game.Create3DPrimitive(type);

    var body2 = entity2.Get<BodyComponent>();
    body2.Awake = true;

    entity2.Transform.Position = new Vector3(0, 20, 0);

    entity2.Scene = rootScene;

    body.ApplyLinearImpulse(new Vector3(0, 0, 0.001f));
});