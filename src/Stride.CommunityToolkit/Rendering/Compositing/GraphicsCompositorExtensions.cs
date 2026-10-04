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
            Enum.GetValues<RenderGroupMask>()
                .Aggregate((mask, next) => mask | next) & ~RenderGroupMask.Group31;

    /// <summary>
    /// Adds a UI render stage to the given <see cref="GraphicsCompositor"/> and resets its post effects to
    /// tone mapping only, so UI text and shapes come out clean and white rather than bloomed or blurred.
    /// This alters the GraphicsCompositor's <see cref="PostProcessingEffects"/>, <see cref="RenderStage"/>, and <see cref="RenderFeature"/>.
    /// </summary>
    /// <param name="graphicsCompositor">The GraphicsCompositor to modify.</param>
    /// <returns>Returns the modified GraphicsCompositor instance, allowing for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// "Reset" means what <see cref="GraphicsCompositorHelper.CreateDefault"/> produces: a
    /// <see cref="PostProcessingEffects"/> with every effect disabled except the colour transforms, which
    /// hold a <see cref="ToneMap"/>. A bare <c>new PostProcessingEffects()</c> is not that - it ships with
    /// bloom, ambient occlusion, screen-space reflections, light streaks, lens flare and FXAA
    /// <em>enabled</em>, which is what this method used to install by accident. Enable effects after this
    /// call, not before it.
    /// </para>
    /// <para>
    /// Renderers already hanging off the compositor (see <see cref="AddSceneRenderer"/>) are kept; the UI
    /// stage is drawn last, after them.
    /// </para>
    /// </remarks>
    public static GraphicsCompositor AddCleanUIStage(this GraphicsCompositor graphicsCompositor)
    {
        ResetPostEffects(graphicsCompositor);
        AddRenderStagesAndFeatures(graphicsCompositor);

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a UI render stage to the given <see cref="GraphicsCompositor"/>, leaving its post effects as they are.
    /// This alters the GraphicsCompositor's <see cref="RenderStage"/> and <see cref="RenderFeature"/>.
    /// </summary>
    /// <param name="graphicsCompositor">The GraphicsCompositor to modify.</param>
    /// <returns>Returns the modified GraphicsCompositor instance, allowing for method chaining.</returns>
    /// <remarks>
    /// Renderers already hanging off the compositor (see <see cref="AddSceneRenderer"/>) are kept; the UI
    /// stage is drawn last, after them.
    /// </remarks>
    public static GraphicsCompositor AddUIStage(this GraphicsCompositor graphicsCompositor)
    {
        AddRenderStagesAndFeatures(graphicsCompositor);

        return graphicsCompositor;
    }

    /// <summary>
    /// The compositor's post effects, or <see langword="null"/> when it has none.
    /// </summary>
    /// <param name="graphicsCompositor">The compositor to look in.</param>
    /// <returns>
    /// The <see cref="PostProcessingEffects"/> on the compositor's forward renderer, or <see langword="null"/>
    /// when the single view is not a <see cref="ForwardRenderer"/>, has no post effects, or has post effects
    /// of some other type.
    /// </returns>
    /// <remarks>
    /// Two casts stand between a compositor and its post effects - <see cref="GraphicsCompositor.SingleView"/>
    /// is an <c>ISceneRenderer</c> and <see cref="ForwardRenderer.PostEffects"/> an <c>IPostProcessingEffects</c> -
    /// and every example that wanted to touch bloom at runtime wrote them out by hand. This is that, once.
    /// The 2D compositor from <c>Add2DGraphicsCompositor</c> has no post effects, so it returns
    /// <see langword="null"/>.
    /// </remarks>
    public static PostProcessingEffects? GetPostEffects(this GraphicsCompositor graphicsCompositor)
    {
        ArgumentNullException.ThrowIfNull(graphicsCompositor);

        return graphicsCompositor.SingleView is ForwardRenderer { PostEffects: PostProcessingEffects effects } ? effects : null;
    }

    /// <summary>
    /// Runs <paramref name="configure"/> against the compositor's post effects - the place to switch on
    /// bloom, fog, ambient occlusion and the rest, all of which start disabled.
    /// </summary>
    /// <param name="graphicsCompositor">The compositor to configure.</param>
    /// <param name="configure">Receives the live <see cref="PostProcessingEffects"/>; set <c>Enabled</c> and tune as needed.</param>
    /// <returns>The compositor, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The default compositor ships every effect switched off and only a <see cref="ToneMap"/> in the
    /// colour transforms, so enabling is always explicit: <c>fx.Bloom.Enabled = true</c>. Colour transforms
    /// other than tone mapping - <see cref="Vignetting"/>, <c>FilmGrain</c>, <c>Dither</c> - are not present
    /// at all and must be added: <c>fx.ColorTransforms.Transforms.Add(new Vignetting())</c>.
    /// </para>
    /// <para>
    /// Call this after <see cref="AddCleanUIStage"/>, which resets the post effects to tone mapping only;
    /// anything enabled before it is lost.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="graphicsCompositor"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The compositor has no post effects to configure.</exception>
    /// <example>
    /// <code>
    /// game.AddGraphicsCompositor()
    ///     .AddCleanUIStage()
    ///     .ConfigurePostEffects(fx =>
    ///     {
    ///         fx.Bloom.Enabled = true;
    ///         fx.Fog.Enabled = true;
    ///         fx.ColorTransforms.Transforms.Add(new Vignetting());
    ///     });
    /// </code>
    /// </example>
    public static GraphicsCompositor ConfigurePostEffects(this GraphicsCompositor graphicsCompositor, Action<PostProcessingEffects> configure)
    {
        ArgumentNullException.ThrowIfNull(graphicsCompositor);
        ArgumentNullException.ThrowIfNull(configure);

        var effects = graphicsCompositor.GetPostEffects()
            ?? throw new InvalidOperationException("The graphics compositor has no post effects to configure. AddGraphicsCompositor() creates them; the 2D compositor has none.");

        configure(effects);

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a scene renderer only if one of the same type is not already present.
    /// </summary>
    /// <typeparam name="TRenderer">Type of renderer to ensure.</typeparam>
    /// <param name="graphicsCompositor">The GraphicsCompositor to add to.</param>
    /// <param name="create">Creates the renderer, called only when one is actually needed.</param>
    /// <returns>Returns the GraphicsCompositor instance, allowing for method chaining.</returns>
    /// <remarks>
    /// Meant for renderers that draw everything of a given kind in the scene, where a second instance
    /// would draw everything twice rather than add anything. It lets a helper guarantee the rendering
    /// its own output depends on, without having to know whether the caller already arranged it.
    /// </remarks>
    public static GraphicsCompositor EnsureSceneRenderer<TRenderer>(this GraphicsCompositor graphicsCompositor, Func<TRenderer> create)
        where TRenderer : SceneRendererBase
    {
        ArgumentNullException.ThrowIfNull(graphicsCompositor);
        ArgumentNullException.ThrowIfNull(create);

        if (graphicsCompositor.Game is SceneRendererCollection existing && existing.Children.Any(child => child is TRenderer))
        {
            return graphicsCompositor;
        }

        return graphicsCompositor.AddSceneRenderer(create());
    }

    /// <summary>
    /// Adds a new scene renderer to the specified GraphicsCompositor's game. If the game is already a scene renderer collection,
    /// the new scene renderer is added to that collection. Otherwise, a new scene renderer collection is created to house both
    /// the existing game and the new scene renderer.
    /// </summary>
    /// <param name="graphicsCompositor">The GraphicsCompositor to which the scene renderer will be added.</param>
    /// <param name="sceneRenderer">The new <see cref="SceneRendererBase"/> instance that will be added to the GraphicsCompositor's game.</param>
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
    /// Adds a root render feature to the specified graphics compositor.
    /// </summary>
    /// <param name="graphicsCompositor">The graphics compositor to which the render feature will be added. Cannot be null.</param>
    /// <param name="renderFeature">The root render feature to add. Cannot be null.</param>
    public static void AddRootRenderFeature(this GraphicsCompositor graphicsCompositor, RootRenderFeature renderFeature)
    {
        graphicsCompositor.RenderFeatures.Add(renderFeature);
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

    /// <summary>
    /// Adds particle rendering stages and features to the specified graphics compositor.
    /// </summary>
    /// <remarks>This method configures the graphics compositor to support particle rendering by adding a <see
    /// cref="ParticleEmitterRenderFeature"/>. It requires the presence of both "Opaque" and "Transparent" render stages
    /// in the compositor. The method will throw a <see cref="NullReferenceException"/> if either stage is
    /// missing.</remarks>
    /// <param name="graphicsCompositor">The graphics compositor to which the particle stages and features will be added.</param>
    /// <exception cref="NullReferenceException">Thrown if the "Opaque" or "Transparent" render stage is not found in the graphics compositor.</exception>
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

    /// <summary>
    /// Tone mapping only, the same way <see cref="GraphicsCompositorHelper.CreateDefault"/> builds it.
    /// </summary>
    /// <remarks>
    /// <see cref="PostProcessingEffects.DisableAll"/> is the whole point: the constructor enables bloom,
    /// ambient occlusion, screen-space reflections, light streaks, lens flare and FXAA, and only turning
    /// them off one by one - or all at once - yields the "nothing but tone mapping" that the name promises.
    /// </remarks>
    private static void ResetPostEffects(GraphicsCompositor graphicsCompositor)
    {
        var forwardRenderer = (ForwardRenderer)graphicsCompositor.SingleView;

        var postEffects = new PostProcessingEffects
        {
            ColorTransforms = { Transforms = { new ToneMap() } }
        };

        postEffects.DisableAll();
        postEffects.ColorTransforms.Enabled = true;

        forwardRenderer.PostEffects = postEffects;
    }

    private static void AddRenderStagesAndFeatures(GraphicsCompositor graphicsCompositor)
    {
        var cameraSlot = graphicsCompositor.Cameras[0];
        // A stage without a sort mode draws its objects in the order the engine collected them, and
        // it collects them on several threads: two objects that overlap would swap from frame to frame
        var uiStage = new RenderStage(UiStageName, MainStageName) { SortMode = new BackToFrontSortMode() };

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

    /// <summary>
    /// Rebuilds the compositor's top-level renderer as: the main view (everything but the UI group),
    /// then whatever other renderers were already attached, then the UI stage.
    /// </summary>
    /// <remarks>
    /// The main view is recreated rather than reused because its render mask changes here - it must
    /// stop drawing the UI group that the second renderer now owns. Anything else the caller attached
    /// before this call - a text renderer, a debug renderer - used to be thrown away with the old
    /// collection; it is carried over so that the order in which helpers are called stops mattering.
    /// </remarks>
    private static void UpdateSceneRendererCollection(GraphicsCompositor graphicsCompositor, SceneCameraSlot cameraSlot, RenderStage uiStage)
    {
        var singleView = graphicsCompositor.SingleView;

        var collection = new SceneRendererCollection
        {
            new SceneCameraRenderer
            {
                Child = singleView,
                Camera = cameraSlot,
                RenderMask = RenderGroupMaskAllExcludingGroup31()
            }
        };

        if (graphicsCompositor.Game is SceneRendererCollection existing)
        {
            foreach (var child in existing.Children)
            {
                if (child is SceneCameraRenderer { Child: var viewed } && ReferenceEquals(viewed, singleView))
                    continue; // the old main view; replaced above with the narrower mask

                collection.Children.Add(child);
            }
        }

        collection.Children.Add(new SceneCameraRenderer
        {
            Camera = cameraSlot,
            Child = new SingleStageRenderer { RenderStage = uiStage },
            RenderMask = RenderGroupMask.Group31
        });

        graphicsCompositor.Game = collection;
    }
}