using Stride.CommunityToolkit.Rendering.DebugShapes;
using Stride.Particles.Rendering;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.UI;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// Provides extension methods for the <see cref="GraphicsCompositor"/> class to enhance its functionality.
/// These methods allow for the addition of UI stages, scene renderers, and debug render features,
/// as well as utility methods for working with render stages.
/// </summary>
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
    /// Adds an <see cref="EntityDebugSceneRenderer"/> to the <see cref="GraphicsCompositor"/> for rendering entity debug information.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to which the entity debug renderer will be added.</param>
    /// <param name="options">Optional settings to customize the appearance of the debug renderer. If not provided, default options will be used.</param>
    /// <returns>The modified <see cref="GraphicsCompositor"/> instance with the entity debug renderer added.</returns>
    /// <remarks>
    /// This method adds a custom <see cref="EntityDebugSceneRenderer"/> to the graphics compositor, allowing the display of debug information
    /// such as entity names and positions in a 3D scene. The renderer can be customized using the <paramref name="options"/> parameter,
    /// which allows the user to define font size, color, and other settings.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to add an entity debug renderer with default settings:
    /// <code>
    /// graphicsCompositor.EntityDebugSceneRenderer();
    /// </code>
    /// You can also specify custom options:
    /// <code>
    /// var options = new EntityDebugRendererOptions { FontSize = 16, FontColor = Color.Red };
    /// graphicsCompositor.EntityDebugSceneRenderer(options);
    /// </code>
    /// </example>
    public static GraphicsCompositor AddEntityDebugRenderer(this GraphicsCompositor graphicsCompositor, EntityDebugSceneRendererOptions? options = null)
    {
        graphicsCompositor.AddSceneRenderer(new EntityDebugSceneRenderer(options));

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

    /// <summary>
    /// Adds an immediate debug render feature to the specified <see cref="GraphicsCompositor"/>.
    /// This method ensures the debug render feature is added only once and links it with both the
    /// "Opaque" and "Transparent" render stages.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> to modify.</param>
    /// <exception cref="NullReferenceException">
    /// Thrown when the "Opaque" or "Transparent" render stages are not found in the <paramref name="graphicsCompositor"/>.
    /// </exception>
    public static void AddImmediateDebugRenderFeature(this GraphicsCompositor graphicsCompositor)
    {
        var debugRenderFeatures = graphicsCompositor.RenderFeatures.OfType<ImmediateDebugRenderFeature>();

        if (!graphicsCompositor.TryGetRenderStage("Opaque", out var opaqueRenderStage))
        {
            throw new NullReferenceException("Opaque RenderStage not found");
        }

        if (!graphicsCompositor.TryGetRenderStage("Transparent", out var transparentRenderStage))
        {
            throw new NullReferenceException("Transparent RenderStage not found");
        }

        if (!debugRenderFeatures.Any())
        {
            var newDebugRenderFeature = new ImmediateDebugRenderFeature()
            {
                RenderStageSelectors = {
                        new ImmediateDebugRenderStageSelector
                        {
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage
                        }
                    }
            };

            graphicsCompositor.RenderFeatures.Add(newDebugRenderFeature);
        }
    }

    /// <summary>
    /// Attempts to retrieve a render stage from the specified <see cref="GraphicsCompositor"/> based on the provided effect name.
    /// </summary>
    /// <param name="graphicsCompositor">The <see cref="GraphicsCompositor"/> containing the render stages.</param>
    /// <param name="effectName">The name of the render stage to search for.</param>
    /// <param name="renderStage">
    /// When this method returns, contains the <see cref="RenderStage"/> if the render stage was found; otherwise, <c>null</c>.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the render stage is found; otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetRenderStage(this GraphicsCompositor graphicsCompositor, string effectName, out RenderStage? renderStage)
    {
        renderStage = null;

        var renderSystem = graphicsCompositor.RenderSystem;

        for (int i = 0; i < renderSystem.RenderStages.Count; ++i)
        {
            var stage = renderSystem.RenderStages[i];

            if (stage.Name == effectName)
            {
                renderStage = stage;

                return true;
            }
        }

        return false;
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

    public static void AddParticleStagesAndFeatures(this GraphicsCompositor graphicsCompositor)
    {
        if (!graphicsCompositor.TryGetRenderStage("Opaque", out var opaqueRenderStage))
        {
            throw new NullReferenceException("Opaque RenderStage not found");
        }

        if (!graphicsCompositor.TryGetRenderStage("Transparent", out var transparentRenderStage))
        {
            throw new NullReferenceException("Transparent RenderStage not found");
        }

        graphicsCompositor.RenderFeatures.Add(new ParticleEmitterRenderFeature()
        {
            Name = "ParticleEmitterRenderFeature",
            RenderStageSelectors =
            {
                new ParticleEmitterTransparentRenderStageSelector()
                {
                    EffectName = "ParticleEmitterTransparent",
                    RenderGroup = RenderGroupMaskAllExcludingGroup31(),
                    OpaqueRenderStage = opaqueRenderStage,
                    TransparentRenderStage = transparentRenderStage
                }
            }
        });
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