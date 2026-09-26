using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Effects.Picking;

/// <summary>Adds GPU picking to a game.</summary>
public static class GpuPickerExtensions
{
    /// <summary>The effect meshes are drawn with in the picking stage: the forward effect's vertex side, the id writer's pixel side.</summary>
    internal const string EffectName = "GpuPickingEffect.GpuPickingIds";

    /// <summary>
    /// Adds a picking pass over the main camera, so a screen point can be turned into the entity,
    /// mesh, instance and surface point under it without any collider. Call once, after the
    /// compositor exists; ask the returned picker every frame you need an answer.
    /// </summary>
    /// <param name="game">The game whose compositor draws the pass.</param>
    /// <param name="pickable">Which render groups the pass draws. Everything by default.</param>
    /// <returns>The picker. Dispose it to take the pass out again.</returns>
    /// <remarks>
    /// Behind the call: a "Picking" render stage with a float target and a depth buffer, a
    /// <see cref="SimpleGroupToRenderStageSelector"/> on the compositor's mesh feature routing meshes
    /// into it under the picking effect, a <see cref="GpuPickingRenderFeature"/> writing each mesh's
    /// id, and a <see cref="GpuPickingSceneRenderer"/> over the main camera slot appended after the
    /// main view, drawing and reading back only on frames with a request.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The game has no compositor, no mesh render feature, or no camera slot.</exception>
    public static GpuPicker AddGpuPicker(this Game game, RenderGroupMask pickable = RenderGroupMask.All)
    {
        ArgumentNullException.ThrowIfNull(game);

        var compositor = game.SceneSystem.GraphicsCompositor
            ?? throw new InvalidOperationException("The game has no graphics compositor; call AddGraphicsCompositor or SetupBase3D first.");
        var meshFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault()
            ?? throw new InvalidOperationException("The compositor has no MeshRenderFeature; there are no meshes to pick.");
        var slot = compositor.Cameras.FirstOrDefault()
            ?? throw new InvalidOperationException("The compositor has no camera slot; the picker needs the main camera.");

        var stage = new RenderStage("Picking", "Picking")
        {
            Output = new RenderOutputDescription(PixelFormat.R32G32B32A32_Float, PixelFormat.D32_Float) { ScissorTestEnable = true },
        };

        compositor.RenderStages.Add(stage);

        var feature = new GpuPickingRenderFeature();

        meshFeature.RenderFeatures.Add(feature);

        var selector = new SimpleGroupToRenderStageSelector { EffectName = EffectName, RenderStage = stage, RenderGroup = pickable };

        meshFeature.RenderStageSelectors.Add(selector);

        var renderer = new GpuPickingSceneRenderer
        {
            Stage = stage,
            Feature = feature,
            Camera = slot,
            FollowSource = () => game.Input.MousePosition,
        };

        compositor.AddSceneRenderer(renderer);

        return new GpuPicker(compositor, meshFeature, stage, feature, selector, renderer);
    }
}