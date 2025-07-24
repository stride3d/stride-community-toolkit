using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;

namespace Stride.CommunityToolkit.Engine;

public static class GraphicsCompositorHelper2D
{
    public static GraphicsCompositor CreateDefault(Color4? clearColor = null)
    {
        var opaqueStage = new RenderStage("Opaque", "Main") { SortMode = new StateChangeSortMode() };
        var transparentStage = new RenderStage("Transparent", "Main") { SortMode = new BackToFrontSortMode() };

        var spriteFeature = new SpriteRenderFeature
        {
            RenderStageSelectors = { new SpriteTransparentRenderStageSelector
            {
                EffectName = "Test",
                OpaqueRenderStage = opaqueStage,
                TransparentRenderStage = transparentStage,
                RenderGroup = RenderGroupMask.All
            } }
        };

        var forwardRenderer = new ForwardRenderer
        {
            Clear = { Color = clearColor ?? Color.CornflowerBlue },
            OpaqueRenderStage = opaqueStage,
            TransparentRenderStage = transparentStage
        };

        var cameraSlot = new SceneCameraSlot();

        return new GraphicsCompositor
        {
            Cameras = { cameraSlot },
            RenderStages = { opaqueStage, transparentStage },
            RenderFeatures = { spriteFeature },
            Game = new SceneCameraRenderer { Child = forwardRenderer, Camera = cameraSlot }
        };
    }
}