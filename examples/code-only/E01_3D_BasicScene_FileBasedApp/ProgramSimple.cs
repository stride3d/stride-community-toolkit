#:package Stride.CommunityToolkit@1.0.0-preview.68
#:package Stride.CommunityToolkit.Bepu@1.0.0-preview.68
#:package Stride.CommunityToolkit.Skyboxes@1.0.0-preview.68
#:property PublishAot=false

using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    var entity = game.CreatePrimitive(Primitive3D.Capsule());
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}