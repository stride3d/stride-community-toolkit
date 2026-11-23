using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Background;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Sprites;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The 2D graphics compositor: a forward renderer with no post effects, so flat colours stay as written, and the
/// mesh, sprite and background features over the opaque and transparent stages.
/// </summary>
/// <remarks>
/// Post effects are added afterwards, through <see cref="GraphicsCompositorExtensions.AddCleanUIStage"/> with
/// <c>toneMap: false</c>, which is what <see cref="SceneSetup2D"/> does when its <see cref="SceneSetup.PostEffects"/> is set.
/// </remarks>
public static class GraphicsCompositorHelper2D
{
    private const string ModelEffectName = "StrideForwardShadingEffect";

    /// <summary>
    /// Creates the 2D <see cref="GraphicsCompositor"/>, with one camera slot and no post effects.
    /// </summary>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="msaa">Multisample anti-aliasing level for the forward renderer. Defaults to <see cref="MultisampleCount.None"/>.
    /// Thin geometry - lines drawn as narrow quads, small sprites - flickers as it moves without it, because a
    /// shape narrower than a pixel is only drawn when it happens to cover the pixel's centre. The level is clamped
    /// to what the graphics device supports.</param>
    /// <returns>A <see cref="GraphicsCompositor"/> with the render stages and features for opaque and transparent objects.</returns>
    public static GraphicsCompositor CreateDefault(Color4? clearColor = null, MultisampleCount msaa = MultisampleCount.None)
    {
        var opaqueRenderStage = new RenderStage(RenderStageNames.Opaque, "Main") { SortMode = new StateChangeSortMode() };
        var transparentRenderStage = new RenderStage(RenderStageNames.Transparent, "Main") { SortMode = new BackToFrontSortMode() };

        var singleView = new ForwardRenderer
        {
            Clear = { Color = clearColor ?? Color.CornflowerBlue },
            OpaqueRenderStage = opaqueRenderStage,
            TransparentRenderStage = transparentRenderStage,
            MSAALevel = msaa,
        };

        var forwardLighting = new ForwardLightingRenderFeature
        {
            LightRenderers =
            {
                new LightAmbientRenderer(),
                new LightDirectionalGroupRenderer(),
                new LightPointGroupRenderer(),
                new LightSpotGroupRenderer(),
            },
        };

        var cameraSlot = new SceneCameraSlot();

        return new GraphicsCompositor
        {
            Cameras = { cameraSlot },
            RenderStages = { opaqueRenderStage, transparentRenderStage },
            RenderFeatures =
            {
                new MeshRenderFeature
                {
                    RenderFeatures =
                    {
                        new TransformRenderFeature(),
                        new MaterialRenderFeature(),
                        forwardLighting
                    },
                    RenderStageSelectors =
                    {
                        new MeshTransparentRenderStageSelector
                        {
                            EffectName = ModelEffectName,
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage,
                        },
                    },
                    PipelineProcessors =
                    {
                        new MeshPipelineProcessor { TransparentRenderStage = transparentRenderStage },
                    },
                },
                new SpriteRenderFeature
                {
                    RenderStageSelectors =
                    {
                        new SpriteTransparentRenderStageSelector
                        {
                            EffectName = "Test",
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage,
                        },
                    },
                },
                new BackgroundRenderFeature
                {
                    RenderStageSelectors =
                    {
                        new SimpleGroupToRenderStageSelector
                        {
                            RenderStage = opaqueRenderStage,
                            EffectName = "Test",
                        },
                    },
                },
            },
            Game = new SceneCameraRenderer()
            {
                Child = singleView,
                Camera = cameraSlot,
            },
            Editor = singleView,
            SingleView = singleView,
        };
    }
}