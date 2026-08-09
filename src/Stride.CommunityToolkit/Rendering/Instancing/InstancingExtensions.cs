using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Instancing;

/// <summary>
/// Extension methods that wire up the engine features instanced rendering needs.
/// </summary>
public static class InstancingExtensions
{
    /// <summary>
    /// Enables instanced rendering by adding an <see cref="InstancingRenderFeature"/> to the
    /// compositor's <see cref="MeshRenderFeature"/>.
    /// </summary>
    /// <param name="game">The game whose compositor to modify.</param>
    /// <returns><see langword="true"/> if it was added, <see langword="false"/> if it was already there.</returns>
    /// <exception cref="InvalidOperationException">The compositor has no <see cref="MeshRenderFeature"/>.</exception>
    /// <remarks>
    /// Code-only projects need this and nothing warns when it is missing: the compositor built by
    /// <c>GraphicsCompositorHelper.CreateDefault</c> wires up transform, skinning, material,
    /// shadow-caster and lighting, but not instancing. Without it every instanced master renders as a
    /// single model at its own transform and the instances are invisible. Calling it twice is safe.
    /// </remarks>
    public static bool AddInstancingSupport(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var meshRenderFeature = game.SceneSystem.GraphicsCompositor?.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()
            ?? throw new InvalidOperationException("The graphics compositor has no MeshRenderFeature to add instancing to.");

        if (meshRenderFeature.RenderFeatures.OfType<InstancingRenderFeature>().Any()) return false;

        meshRenderFeature.RenderFeatures.Add(new InstancingRenderFeature());

        return true;
    }

    /// <summary>
    /// Registers <see cref="BufferedEntityInstancing"/> instances with the compositor so their GPU
    /// buffers are created and uploaded each frame.
    /// </summary>
    /// <param name="game">The game whose compositor to modify.</param>
    /// <param name="instancings">The buffered instancings to manage.</param>
    /// <returns>The renderer doing the work, so further targets can be added to it later.</returns>
    /// <remarks>
    /// The renderer is inserted before the one that draws the scene, so uploads are recorded ahead of
    /// the draw calls that read them. Calling this repeatedly reuses the existing renderer rather than
    /// adding another. This does not enable instancing itself - call <see cref="AddInstancingSupport"/> too.
    /// </remarks>
    public static InstancingBufferUploadRenderer AddInstancingBufferUpload(this Game game, params BufferedEntityInstancing[] instancings)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(instancings);

        var compositor = game.SceneSystem.GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor.");

        var renderer = FindOrInsertUploadRenderer(compositor);

        renderer.Targets.AddRange(instancings);

        return renderer;
    }

    private static InstancingBufferUploadRenderer FindOrInsertUploadRenderer(GraphicsCompositor compositor)
    {
        if (compositor.Game is SceneRendererCollection collection)
        {
            var existing = collection.Children.OfType<InstancingBufferUploadRenderer>().FirstOrDefault();

            if (existing is not null) return existing;

            var inserted = new InstancingBufferUploadRenderer();

            collection.Children.Insert(0, inserted);

            return inserted;
        }

        var renderer = new InstancingBufferUploadRenderer();
        var wrapper = new SceneRendererCollection();

        wrapper.Children.Add(renderer);

        if (compositor.Game is not null)
        {
            wrapper.Children.Add(compositor.Game);
        }

        compositor.Game = wrapper;

        return renderer;
    }
}