using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

var type = PrimitiveModelType.Sphere;
Entity? entity1 = null;
Entity? entity2 = null;

using var game = new Game();


game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    entity1 = game.Create3DPrimitive(type);
    entity1.Transform.Position = new Vector3(0, 8, 0);

    var body = entity1.Get<BodyComponent>();
    body.Spec
    body.Awake = true;

    entity1.Scene = rootScene;

    entity2 = game.Create3DPrimitive(type);
    entity2.Transform.Position = new Vector3(0, 20, 0);

    var body2 = entity2.Get<BodyComponent>();
    body2.Awake = true;

    entity2.Scene = rootScene;

    //body.ApplyLinearImpulse(new Vector3(0, 0, 0.001f));
}, update: Update);

void Update(Scene scene, GameTime time)
{
    //var body1 = entity1?.Get<BodyComponent>();

    //if (body1 != null)
    //{
    //    body1.Awake = true;
    //}

    //var body2 = entity2?.Get<BodyComponent>();

    //if (body2 != null)
    //{
    //    body2.Awake = true;
    //}
}