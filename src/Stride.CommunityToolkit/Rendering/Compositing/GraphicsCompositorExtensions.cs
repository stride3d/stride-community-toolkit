using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.UI;

namespace Stride.CommunityToolkit.Rendering.Compositing;

public static class GraphicsCompositorExtensions
{
    private const string UiStageName = "UiStage";
    private const string MainStageName = "Main";
    private const string TestEffectName = "Test";
    private const string UiStageEffectName = "UiStage";

    /// <summary>
    /// Generates a RenderGroupMask that includes all defined RenderGroups except for Group31.
    /// This method dynamically calculates the mask by aggregating all possible RenderGroupMask enum values
    /// and then bitwise negating Group31 from the result.
    /// </summary>
    /// <returns>A RenderGroupMask representing all groups except for Group31.</returns>
    private static RenderGroupMask RenderGroupMaskAllExcludingGroup31() =>
            Enum.GetValues(typeof(RenderGroupMask)).Cast<RenderGroupMask>()
                .Aggregate((mask, next) => mask | next) & ~RenderGroupMask.Group31;

    /// <summary>
    /// Adds a UI render stage and white/clean text effect to the given <see cref="GraphicsCompositor"/>.
    /// This alters the GraphicsCompositor's <see cref="PostProcessingEffects"/>, <see cref="RenderStage"/>, and <see cref="RenderFeature"/>.
    /// </summary>
    /// <param name="graphicsCompositor">The GraphicsCompositor to modify.</param>
    /// <returns>Returns the modified GraphicsCompositor instance, allowing for method chaining.</returns>
    public static GraphicsCompositor AddCleanUIStage(this GraphicsCompositor graphicsCompositor)
    {
        AddPostEffects(graphicsCompositor);
        AddRenderStagesAndFeatures(graphicsCompositor);

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a new scene renderer to the given GraphicsCompositor's game. If the game is already a collection of scene renderers,
    /// the new scene renderer is added to that collection. Otherwise, a new scene renderer collection is created to house both
    /// the existing game and the new scene renderer.
    /// </summary>
    /// <param name="graphicsCompositor">The GraphicsCompositor to which the scene renderer will be added.</param>
    /// <param name="sceneRenderer">The new <see cref="SceneRendererBase"/> instance that will be added to the GraphicsCompositor's game.</param>
    /// <remarks>
    /// This method will either add the scene renderer to an existing SceneRendererCollection or create a new one to house both
    /// the existing game and the new scene renderer. In either case, the GraphicsCompositor's game will end up with the new scene renderer added.
    /// </remarks>
    /// <returns>Returns the modified GraphicsCompositor instance, allowing for method chaining.</returns>
    public static GraphicsCompositor AddSceneRenderer(this GraphicsCompositor graphicsCompositor, SceneRendererBase sceneRenderer)
    {
        if (graphicsCompositor.Game is SceneRendererCollection sceneRendererCollection)
        {
            sceneRendererCollection.Children.Add(sceneRenderer);
        }
        else
        {
            var newSceneRendererCollection = new SceneRendererCollection();

            newSceneRendererCollection.Children.Add(graphicsCompositor.Game);
            newSceneRendererCollection.Children.Add(sceneRenderer);

            graphicsCompositor.Game = newSceneRendererCollection;
        }

        return graphicsCompositor;
    }

    private static void AddPostEffects(GraphicsCompositor graphicsCompositor)
    {
        var forwardRenderer = (ForwardRenderer)graphicsCompositor.SingleView;

        forwardRenderer.PostEffects = new PostProcessingEffects
        {
            DepthOfField = { Enabled = false },
            ColorTransforms = { Transforms = { new ToneMap() } }
        };
    }

    private static void AddRenderStagesAndFeatures(GraphicsCompositor graphicsCompositor)
    {
        var cameraSlot = graphicsCompositor.Cameras[0];
        var uiStage = new RenderStage(UiStageName, MainStageName);

        graphicsCompositor.RenderStages.Add(uiStage);

        graphicsCompositor.RenderFeatures.Add(new UIRenderFeature
        {
            RenderStageSelectors =
                {
                    new SimpleGroupToRenderStageSelector {
                        RenderStage = ((ForwardRenderer)graphicsCompositor.SingleView).TransparentRenderStage,
                        EffectName = TestEffectName,
                        RenderGroup = RenderGroupMaskAllExcludingGroup31()
                    },
                    new SimpleGroupToRenderStageSelector {
                        RenderStage = uiStage,
                        EffectName = UiStageEffectName,
                        RenderGroup = RenderGroupMask.Group31
                    }
                }
        });

        UpdateSceneRendererCollection(graphicsCompositor, cameraSlot, uiStage);
    }

    private static void UpdateSceneRendererCollection(GraphicsCompositor graphicsCompositor, SceneCameraSlot cameraSlot, RenderStage uiStage)
    {
        graphicsCompositor.Game = new SceneRendererCollection {
                new SceneCameraRenderer
                {
                    Child = graphicsCompositor.SingleView,
                    Camera = cameraSlot,
                    RenderMask = RenderGroupMaskAllExcludingGroup31()
                },
                new SceneCameraRenderer
                {
                    Camera = cameraSlot,
                    Child = new SingleStageRenderer { RenderStage = uiStage },
                    RenderMask = RenderGroupMask.Group31
                }
            };
    }
}