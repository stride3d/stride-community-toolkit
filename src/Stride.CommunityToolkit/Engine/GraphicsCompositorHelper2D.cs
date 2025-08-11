using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Background;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Materials;
using Stride.Rendering.Sprites;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides helper methods for creating and configuring 2D graphics compositors.
/// </summary>
/// <remarks>This class includes methods to create default configurations for graphics compositors, allowing
/// customization of rendering options such as post-processing effects, model effects, and camera settings.</remarks>
public static class GraphicsCompositorHelper2D
{
    /// <summary>
    /// Creates a default <see cref="GraphicsCompositor"/> configured with specified rendering options.
    /// </summary>
    /// <param name="enablePostEffects">If <see langword="true"/>, post-processing effects such as tone mapping are enabled; otherwise, they are
    /// disabled.</param>
    /// <param name="modelEffectName">The name of the effect used for rendering models. Defaults to "StrideForwardShadingEffect".</param>
    /// <param name="camera">An optional <see cref="CameraComponent"/> to be associated with the compositor. If provided, it will be assigned
    /// to a camera slot.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="groupMask">Specifies the render group mask to be used for rendering. Defaults to <see cref="RenderGroupMask.All"/>.</param>
    /// <returns>A <see cref="GraphicsCompositor"/> instance configured with the specified options, including render stages and
    /// features for opaque and transparent objects.</returns>
    public static GraphicsCompositor CreateDefault(bool enablePostEffects = false, string modelEffectName = "StrideForwardShadingEffect", CameraComponent? camera = null, Color4? clearColor = null, RenderGroupMask groupMask = RenderGroupMask.All)
    {
        var opaqueRenderStage = new RenderStage("Opaque", "Main") { SortMode = new StateChangeSortMode() };
        var transparentRenderStage = new RenderStage("Transparent", "Main") { SortMode = new BackToFrontSortMode() };

        var postProcessingEffects = enablePostEffects
            ? new PostProcessingEffects
            {
                ColorTransforms = { Transforms = { new ToneMap() } },
            }
            : null;

        if (postProcessingEffects != null)
        {
            postProcessingEffects.DisableAll();
            postProcessingEffects.ColorTransforms.Enabled = true;
        }

        var singleView = new ForwardRenderer
        {
            Clear = { Color = clearColor ?? Color.CornflowerBlue },
            OpaqueRenderStage = opaqueRenderStage,
            TransparentRenderStage = transparentRenderStage,
            PostEffects = postProcessingEffects,
        };

        var cameraSlot = new SceneCameraSlot();

        if (camera != null)
        {
            camera.Slot = cameraSlot.ToSlotId();
        }

        return new GraphicsCompositor
        {
            Cameras = { cameraSlot },
            RenderStages = { opaqueRenderStage, transparentRenderStage },
            RenderFeatures = {
                new MeshRenderFeature
                {
                    RenderFeatures =
                    {
                        new TransformRenderFeature(),
                        new MaterialRenderFeature(),
                    },
                    RenderStageSelectors =
                    {
                        new MeshTransparentRenderStageSelector
                        {
                            EffectName = modelEffectName,
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage,
                            RenderGroup = groupMask,
                        },
                    }
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
                            RenderGroup = groupMask,
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
                            RenderGroup = groupMask,
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