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
    public static void AddCleanUIStage(this GraphicsCompositor graphicsCompositor)
    {
        AddPostEffects(graphicsCompositor);
        AddRenderStagesAndFeatures(graphicsCompositor);
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