using Example13_RootRendererShader.Renderers;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Rendering;

using var game = new Game();

game.Run(start: Start);


void Start(Scene scene)
{
    game.SetupBase();
    AddRenderFeature();
    game.AddProfiler();

    // We must use a component here as it makes sure to add the render processor to the scene.
    // The render prcoessor is responsible for managing render objects for the visibility group.
    // The visibility group is added when a valid render processor "component" is added to the scene.
    var background = new RibbonBackgroundComponent
    {
        Intensity = 0.5f,
        Frequency = 0.5f,
        Amplitude = 0.5f,
        Speed = 0.5f,
        WidthFactor = 0.5f
    };

    // Once this gets added to the scene, the render processor will be added to the scene.
    var entity = new Entity { new RibbonBackgroundComponent() };
    scene.Entities.Add(entity);

    game.Window.Position = new Stride.Core.Mathematics.Int2(50, 50);
}

// This method adds the render feature to the game.
//This ensures that the game knows how to render the RibbonBackgroundComponent.
void AddRenderFeature()
{
    game.SceneSystem.GraphicsCompositor.TryGetRenderStage("Opaque", out var opaqueRenderStage);
    var renderFeature = new RibbonBackgroundRenderFeature()
    {
        RenderStageSelectors =
        {
            new SimpleGroupToRenderStageSelector
            {
                EffectName = "RibbonBackground",
                RenderGroup = RenderGroupMask.All,
                RenderStage = opaqueRenderStage,
            }
        }
    };

    game.AddRootRenderFeature(renderFeature);
}