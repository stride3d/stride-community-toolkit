using Stride.Engine;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>Adds Game Studio's material view modes to a game.</summary>
public static class MaterialStreamViewExtensions
{
    /// <summary>
    /// Adds a material stream view: set its <see cref="MaterialStreamView.Stream"/> and every mesh
    /// draws that stream as its colour instead of the lit result, the way the editor's toolbar view
    /// modes do. Call once, after the compositor exists.
    /// </summary>
    /// <param name="game">The game whose compositor draws the meshes.</param>
    /// <returns>The view, showing lit shading until a stream is set. Dispose it to take the feature out again.</returns>
    /// <remarks>
    /// Behind the call: a <see cref="MaterialStreamRenderFeature"/> on the compositor's
    /// <see cref="MeshRenderFeature"/>, which sets the forward effect's surface-filter permutation on
    /// every effect drawn while a stream is chosen. Nothing is drawn differently until then.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The game has no compositor or no mesh render feature.</exception>
    public static MaterialStreamView AddMaterialStreamView(this Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var compositor = game.SceneSystem.GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor; call AddGraphicsCompositor or SetupBase3D first.");
        var meshFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()
            ?? throw new InvalidOperationException("The compositor has no MeshRenderFeature; there are no meshes to view.");

        var feature = new MaterialStreamRenderFeature();

        meshFeature.RenderFeatures.Add(feature);

        return new MaterialStreamView(meshFeature, feature);
    }
}