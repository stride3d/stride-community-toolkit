using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;

using var game = new Game();

game.Run(start: (Action<Scene>?)((Scene rootScene) =>
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Bepu 2D Physics Example - Stride Community Toolkit";

    game.SetupBase2DScene();
    game.AddProfiler();

    for (int i = 0; i <= 50; i++)
    {
        var box = game.Create2DPrimitive(Primitive2DModelType.Capsule, new()
        {
            Material = game.CreateFlatMaterial(Color.Orange),
            Size = new Vector2(0.5f, 0.8f),
            Depth = 1
        });
        box.Transform.Position = new Vector3(0, 10 + i * 2, 0);
        box.Scene = rootScene;
    }

    var entity = game.Create2DPrimitive(Primitive2DModelType.Circle, new()
    {
        Material = game.CreateFlatMaterial(Color.Red),
    });

    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Add(new DebugRenderComponentScript());
    entity.Add(new CollidableGizmoScript()
    {
        Color = new Color4(0.4f, 0.843f, 0, 0.9f),
        Visible = false
    });

    entity.Scene = rootScene;
}));