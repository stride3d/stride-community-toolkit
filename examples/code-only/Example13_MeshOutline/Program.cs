using Example13_MeshOutline;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddRootRenderFeature(new MeshOutlineRenderFeature()
    {
        RenderGroupMask = RenderGroupMask.Group5,
        ScaleAdjust = 0.03f
    });

    var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, options: new()
    {
        RenderGroup = RenderGroup.Group5,
    });

    entity.Transform.Position = new Vector3(1f, 0.5f, 3f);

    entity.Add(new MeshOutlineComponent()
    {
        Enabled = true,
        Color = Color.Cyan,
        Intensity = 100f
    });

    entity.Scene = rootScene;
}