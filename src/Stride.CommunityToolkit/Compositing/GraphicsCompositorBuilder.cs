using Stride.Particles.Rendering;
using Stride.Rendering;
using Stride.Rendering.Background;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Shadows;
using Stride.Rendering.Sprites;
using Stride.Rendering.UI;

namespace Stride.CommunityToolkit.Compositing;

// Taken from Stride.Rendering.Compositing GraphicsCompositorHelper
public static class GraphicsCompositorBuilder
{
    public static GraphicsCompositor Create(Color4? clearColor = null)
    {
        var groupMask = RenderGroupMask.All;
        var opaqueRenderStage = new RenderStage("Opaque", "Main") { SortMode = new StateChangeSortMode() };
        var transparentRenderStage = new RenderStage("Transparent", "Main") { SortMode = new BackToFrontSortMode() };
        var shadowCasterRenderStage = new RenderStage("ShadowMapCaster", "ShadowMapCaster") { SortMode = new FrontToBackSortMode() };
        var shadowCasterCubeMapRenderStage = new RenderStage("ShadowMapCasterCubeMap", "ShadowMapCasterCubeMap") { SortMode = new FrontToBackSortMode() };
        var shadowCasterParaboloidRenderStage = new RenderStage("ShadowMapCasterParaboloid", "ShadowMapCasterParaboloid") { SortMode = new FrontToBackSortMode() };
        var uiStage = new RenderStage("UiStage", "Main");

        var postProcessingEffects = new PostProcessingEffects
        {
            //Outline = { Enabled = false },
            //Fog = { Enabled = false },
            //AmbientOcclusion = { Enabled = false },
            //LocalReflections = { Enabled = false },
            DepthOfField = { Enabled = false },
            //BrightFilter = { Enabled = true },
            //Bloom = {  Enabled = true },
            //LightStreak = {  Attenuation = 0.7f },
            ColorTransforms = { Transforms = { new ToneMap() } },
        };

        //postProcessingEffects.DisableAll();
        //postProcessingEffects.ColorTransforms.Enabled = true;

        var singleView = new ForwardRenderer
        {
            Clear = { Color = clearColor ?? Color.CornflowerBlue },
            OpaqueRenderStage = opaqueRenderStage,
            TransparentRenderStage = transparentRenderStage,
            ShadowMapRenderStages = { shadowCasterRenderStage, shadowCasterParaboloidRenderStage, shadowCasterCubeMapRenderStage },
            PostEffects = postProcessingEffects
        };

        var cameraSlot = new SceneCameraSlot { Name = "Main" };

        return new()
        {
            Cameras = { cameraSlot },
            RenderStages =
                {
                    opaqueRenderStage,
                    transparentRenderStage,
                    shadowCasterRenderStage,
                    shadowCasterParaboloidRenderStage,
                    shadowCasterCubeMapRenderStage,
                    uiStage
                },
            RenderFeatures =
                {
                    new MeshRenderFeature
                    {
                        RenderFeatures =
                        {
                            new TransformRenderFeature(),
                            new SkinningRenderFeature(),
                            new MaterialRenderFeature(),
                            new ShadowCasterRenderFeature(),
                            new ForwardLightingRenderFeature
                            {
                                LightRenderers =
                                {
                                    new LightAmbientRenderer(),
                                    new LightSkyboxRenderer(),
                                    new LightDirectionalGroupRenderer(),
                                    new LightPointGroupRenderer(),
                                    new LightSpotGroupRenderer(),
                                    new LightClusteredPointSpotGroupRenderer(),
                                    new LightProbeRenderer()
                                },
                                ShadowMapRenderer = new ShadowMapRenderer
                                {
                                    Renderers =
                                    {
                                        new LightDirectionalShadowMapRenderer { ShadowCasterRenderStage = shadowCasterRenderStage },
                                        new LightSpotShadowMapRenderer { ShadowCasterRenderStage = shadowCasterRenderStage },
                                        new LightPointShadowMapRendererParaboloid { ShadowCasterRenderStage = shadowCasterParaboloidRenderStage },
                                        new LightPointShadowMapRendererCubeMap { ShadowCasterRenderStage = shadowCasterCubeMapRenderStage }
                                    }
                                }
                            },
                            new InstancingRenderFeature()
                        },
                        RenderStageSelectors =
                        {
                            new MeshTransparentRenderStageSelector
                            {
                                EffectName = "StrideForwardShadingEffect",
                                OpaqueRenderStage = opaqueRenderStage,
                                TransparentRenderStage = transparentRenderStage,
                                RenderGroup = groupMask
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = "StrideForwardShadingEffect.ShadowMapCaster",
                                ShadowMapRenderStage = shadowCasterRenderStage,
                                RenderGroup = groupMask
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = "StrideForwardShadingEffect.ShadowMapCasterParaboloid",
                                ShadowMapRenderStage = shadowCasterParaboloidRenderStage,
                                RenderGroup = groupMask
                            },
                            new ShadowMapRenderStageSelector
                            {
                                EffectName = "StrideForwardShadingEffect.ShadowMapCasterCubeMap",
                                ShadowMapRenderStage = shadowCasterCubeMapRenderStage,
                                RenderGroup = groupMask
                            }
                        },
                        PipelineProcessors =
                        {
                            new MeshPipelineProcessor { TransparentRenderStage = transparentRenderStage },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterRenderStage },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterParaboloidRenderStage, DepthClipping = true },
                            new ShadowMeshPipelineProcessor { ShadowMapRenderStage = shadowCasterCubeMapRenderStage, DepthClipping = true }
                        }
                    },
                    new ParticleEmitterRenderFeature
                    {
                        RenderStageSelectors =
                        {
                            new ParticleEmitterTransparentRenderStageSelector
                            {
                                OpaqueRenderStage = opaqueRenderStage, TransparentRenderStage = transparentRenderStage
                            }
                        }
                    },
                    new UIRenderFeature
                    {
                        RenderStageSelectors =
                        {
                            new SimpleGroupToRenderStageSelector {
                                RenderStage = transparentRenderStage,
                                EffectName = "Test",
                                RenderGroup = RenderGroupMaskAllExcludingGroup31() },
                            new SimpleGroupToRenderStageSelector {
                                RenderStage = uiStage,
                                EffectName = "UiStage",
                                RenderGroup = RenderGroupMask.Group31 }
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
            Game = new SceneRendererCollection {
                new SceneCameraRenderer {
                    Child = singleView,
                    Camera = cameraSlot,
                    RenderMask = RenderGroupMaskAllExcludingGroup31() },
                new SceneCameraRenderer
                {
                    Camera = cameraSlot,
                    Child = new SingleStageRenderer { RenderStage = uiStage },
                    RenderMask = RenderGroupMask.Group31
                }
            },
            Editor = singleView,
            SingleView = singleView
        };
    }

    private static RenderGroupMask RenderGroupMaskAllExcludingGroup31() => RenderGroupMask.Group0 | RenderGroupMask.Group1 | RenderGroupMask.Group2 | RenderGroupMask.Group3 | RenderGroupMask.Group4 | RenderGroupMask.Group5 | RenderGroupMask.Group6 | RenderGroupMask.Group7 | RenderGroupMask.Group8 | RenderGroupMask.Group9 | RenderGroupMask.Group10 | RenderGroupMask.Group11 | RenderGroupMask.Group12 | RenderGroupMask.Group13 | RenderGroupMask.Group14 | RenderGroupMask.Group15 | RenderGroupMask.Group16 | RenderGroupMask.Group17 | RenderGroupMask.Group18 | RenderGroupMask.Group19 | RenderGroupMask.Group20 | RenderGroupMask.Group21 | RenderGroupMask.Group22 | RenderGroupMask.Group23 | RenderGroupMask.Group24 | RenderGroupMask.Group25 | RenderGroupMask.Group26 | RenderGroupMask.Group27 | RenderGroupMask.Group28 | RenderGroupMask.Group29 | RenderGroupMask.Group30;
}