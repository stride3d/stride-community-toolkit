namespace Stride.GameDefaults;

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
                    shadowCasterCubeMapRenderStage
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
                            new SimpleGroupToRenderStageSelector { RenderStage = transparentRenderStage, EffectName = "Test" }
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
            Game = new SceneCameraRenderer { Child = singleView, Camera = cameraSlot },
            Editor = singleView,
            SingleView = singleView
        };
    }
}

