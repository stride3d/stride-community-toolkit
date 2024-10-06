using Example09_Renderer;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (rootScene) =>
{
    game.SetupBase3DScene();
    game.AddProfiler();

    game.AddSceneRenderer(new MyCustomSceneRenderer());

    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Add(new SpriteBatchRendererScript());

    entity.Scene = rootScene;
});