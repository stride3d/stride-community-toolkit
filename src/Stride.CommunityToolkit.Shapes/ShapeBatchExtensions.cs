using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// Wires <see cref="ShapeBatch"/> rendering into a game's graphics compositor.
/// </summary>
public static class ShapeBatchExtensions
{
    /// <summary>
    /// Registers the shape renderer and returns the batch to submit shapes to.
    /// </summary>
    /// <param name="game">The game whose compositor to modify.</param>
    /// <param name="depthTest">
    /// Whether scene geometry can occlude these shapes. Leave <c>false</c> for gizmos, 2D scenes and
    /// anything that should stay visible through walls; pass <c>true</c> for decals and ground
    /// markers that belong in the scene.
    /// </param>
    /// <param name="fill">
    /// A fill source for a textured batch - see <see cref="ShapeBatch.FillSource"/> - or <c>null</c>
    /// for plain fills. <see cref="ShapeBatch.FillWith"/> installs one later for the common case.
    /// </param>
    /// <param name="afterPostEffects">
    /// Draw the batch after the post-processing chain, in the compositor's UI stage, where the text
    /// renderers draw: a colour comes out as given. In the scene, a shape goes through the tone
    /// mapper with everything else, and under auto exposure a white shape lands at whatever grey
    /// the frame's brightness leaves it - about half, next to text that is truly white. That is
    /// right for a decal on the ground and wrong for a HUD. Nothing is depth-tested there; the
    /// compositor must have the toolkit's UI stage (<c>AddUIStage</c> or <c>AddCleanUIStage</c>,
    /// which <c>SetupBase3D</c> and <c>AddGraphicsCompositor</c> both do), else the batch falls
    /// back to the scene's transparent stage.
    /// </param>
    /// <returns>The batch: submit shapes to it every frame from your update logic.</returns>
    /// <remarks>
    /// Shapes render in the compositor's "Transparent" stage, alpha-blended in submission order,
    /// before UI and debug text - or, with <paramref name="afterPostEffects"/>, in the UI stage
    /// after the post effects. Call after the graphics compositor exists (from the Start callback).
    /// Calling this more than once adds another independent batch, which is how you get depth-tested
    /// and overlay shapes in the same scene; the first batch registers as the service that
    /// <see cref="ShapeComponent"/> draws through. A scene that never calls this still draws its
    /// components: the processor registers a depth-tested batch of its own the first time it needs one.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The compositor has no "Transparent" render stage.</exception>
    public static ShapeBatch AddShapeBatch(this Game game, bool depthTest = false, IComputeColor? fill = null, bool afterPostEffects = false)
    {
        ArgumentNullException.ThrowIfNull(game);

        var compositor = game.SceneSystem.GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor.");

        var sceneInstance = game.SceneSystem.SceneInstance
            ?? throw new InvalidOperationException("The game has no scene instance yet; add the batch from the Start callback or later.");

        var batch = new ShapeBatch { DepthTest = depthTest, FillSource = fill };

        // Group 31 is the toolkit's UI group: the main view leaves it out and a second camera renderer
        // draws it, in the UI stage, once the post effects have run
        if (afterPostEffects) batch.RenderGroup = RenderGroup.Group31;
        var displayScale = DisplayScale.GetOrCreate(game);

        // Screen shapes and Corner() work in the display's scaled pixels, so the window is asked each
        // time rather than remembered: it resizes, and the scale follows the monitor it is on
        batch.ScreenSizeSource = () =>
        {
            var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;

            return new Vector2(backBuffer.Width, backBuffer.Height) / (batch.AutoScale ? displayScale.Value : 1f);
        };

        // Expose the first batch to ShapeProcessor and anything else that wants to draw
        if (game.Services.GetService<ShapeBatch>() is null)
        {
            game.Services.AddService(batch);
        }

        Register(sceneInstance, compositor.RenderSystem, batch);

        return batch;
    }

    /// <summary>
    /// Puts a batch into rendering for a scene: the render feature into the render system, if it is
    /// not there yet, and the batch into the visibility group that pairs the scene with that render
    /// system. What <see cref="AddShapeBatch"/> does for a game, and what <see cref="ShapeProcessor"/>
    /// does for itself where nothing called <see cref="AddShapeBatch"/> - Game Studio's scene editor
    /// among them.
    /// </summary>
    /// <exception cref="InvalidOperationException">The render system has no "Transparent" render stage.</exception>
    internal static void Register(SceneInstance sceneInstance, RenderSystem renderSystem, ShapeBatch batch)
    {
        RenderStage? transparentStage = null;
        RenderStage? uiStage = null;

        foreach (var stage in renderSystem.RenderStages)
        {
            if (stage.Name == "Transparent") transparentStage = stage;
            else if (stage.Name == "UiStage") uiStage = stage;
        }

        if (transparentStage is null)
            throw new InvalidOperationException("The graphics compositor has no Transparent render stage.");

        if (!renderSystem.RenderFeatures.OfType<ShapeBatchFeature>().Any())
        {
            var feature = new ShapeBatchFeature();

            // Every batch to the transparent stage, except the UI group, which goes to the UI stage
            // after the post effects when the compositor has one. Without a UI stage the group draws
            // with everything else, so a batch asked to draw after the post effects still draws
            feature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                RenderStage = transparentStage,
                RenderGroup = uiStage is null ? RenderGroupMask.All : RenderGroupMask.All & ~RenderGroupMask.Group31,
            });

            if (uiStage is not null)
            {
                feature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
                {
                    RenderStage = uiStage,
                    RenderGroup = RenderGroupMask.Group31,
                });
            }

            renderSystem.RenderFeatures.Add(feature);
        }

        VisibilityGroupFor(sceneInstance, renderSystem).RenderObjects.Add(batch);
    }

    /// <summary>
    /// Takes a batch out of rendering. Its shapes stop drawing from the next frame; the batch itself
    /// can be added again later.
    /// </summary>
    /// <param name="game">The game the batch was added to.</param>
    /// <param name="batch">The batch <see cref="AddShapeBatch"/> returned.</param>
    public static void RemoveShapeBatch(this Game game, ShapeBatch batch)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(batch);

        if (game.SceneSystem.GraphicsCompositor is not { } compositor) return;

        foreach (var visibilityGroup in game.SceneSystem.SceneInstance.VisibilityGroups)
        {
            if (visibilityGroup.RenderSystem == compositor.RenderSystem)
            {
                visibilityGroup.RenderObjects.Remove(batch);
            }
        }

        batch.Reset();
    }

    // The visibility group that pairs the scene with the compositor's render system, which is
    // what a render object is registered with. The compositor makes it on its first draw, after
    // the usual Start callback has run; making it here first, the same way, means the batch is
    // registered before the first frame instead of one frame later, and there is no one-shot
    // system polling for it. The compositor finds it by render system and adopts it.
    private static VisibilityGroup VisibilityGroupFor(SceneInstance sceneInstance, RenderSystem renderSystem)
    {
        foreach (var visibilityGroup in sceneInstance.VisibilityGroups)
        {
            if (visibilityGroup.RenderSystem == renderSystem)
            {
                return visibilityGroup;
            }
        }

        var created = new VisibilityGroup(renderSystem);

        sceneInstance.VisibilityGroups.Add(created);

        return created;
    }
}