using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The one place the instancing render feature is added and checked for, shared by the scene setup,
/// the instancing helpers and the run loop's warning.
/// </summary>
internal static class InstancingSupport
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Instancing");

    /// <summary>
    /// Adds an <see cref="InstancingRenderFeature"/> to the compositor's <see cref="MeshRenderFeature"/> once.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when it was added, <see langword="false"/> when it was there already, and
    /// <see langword="null"/> when there is no compositor or no mesh render feature to add it to.
    /// </returns>
    internal static bool? Ensure(GraphicsCompositor? compositor)
    {
        var meshRenderFeature = compositor?.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault();

        if (meshRenderFeature is null) return null;

        if (meshRenderFeature.RenderFeatures.OfType<InstancingRenderFeature>().Any()) return false;

        meshRenderFeature.RenderFeatures.Add(new InstancingRenderFeature());

        return true;
    }

    /// <summary>Whether the compositor can draw instanced meshes.</summary>
    private static bool Has(GraphicsCompositor? compositor)
        => compositor?.RenderFeatures.OfType<MeshRenderFeature>().Any(mesh => mesh.RenderFeatures.OfType<InstancingRenderFeature>().Any()) == true;

    /// <summary>
    /// Logs one warning when the scene holds an <see cref="InstancingComponent"/> and the compositor has no
    /// <see cref="InstancingRenderFeature"/>: the masters would draw as single models and the instances
    /// would be invisible, with nothing else to say so.
    /// </summary>
    internal static void WarnIfMissing(IGame game)
    {
        var sceneSystem = game.GetSceneSystem();

        if (Has(sceneSystem.GraphicsCompositor)) return;

        foreach (var entity in sceneSystem.SceneInstance)
        {
            if (entity.Get<InstancingComponent>() is null) continue;

            Log.Warning($"'{entity.Name}' carries an InstancingComponent but the graphics compositor has no InstancingRenderFeature, so its instances are not drawn. Set Instancing on the scene setup (UseInstancing()) or call AddInstancingSupport().");

            return;
        }
    }
}