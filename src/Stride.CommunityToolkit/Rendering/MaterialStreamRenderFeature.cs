using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// The mesh feature's half of a <see cref="MaterialStreamView"/>: sets the forward effect's
/// <see cref="MaterialKeys.PixelStageSurfaceFilter"/> permutation on every effect drawn this frame,
/// so each mesh is shaded by the filter's shader instead of its lighting. A port of the render
/// feature behind Game Studio's view modes.
/// </summary>
/// <remarks>
/// A permutation parameter is part of the effect's identity: setting one recompiles the effect, and
/// no longer setting it recompiles the plain one, so a view switch costs a compile and a frame or two
/// of the fallback effect, the same as the editor's toolbar. With <see cref="Filter"/> null the feature
/// does nothing.
/// </remarks>
public sealed class MaterialStreamRenderFeature : SubRenderFeature
{
    private StaticObjectPropertyKey<RenderEffect> _renderEffectKey;

    /// <summary>The shader mixed into every effect, or <see langword="null"/> for lit shading.</summary>
    public ShaderSource? Filter { get; set; }

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
    }

    /// <inheritdoc/>
    public override void PrepareEffectPermutations(RenderDrawContext context)
    {
        if (Filter is null) return;

        var renderEffects = RootRenderFeature.RenderData.GetData(_renderEffectKey);
        var slotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

        foreach (var renderObject in RootRenderFeature.RenderObjects)
        {
            var staticObjectNode = renderObject.StaticObjectNode;

            for (var i = 0; i < slotCount; i++)
            {
                var renderEffect = renderEffects[staticObjectNode * slotCount + i];

                // An effect not drawn this frame is not validated this frame
                if (renderEffect is null || !renderEffect.IsUsedDuringThisFrame(RenderSystem)) continue;

                renderEffect.EffectValidator.ValidateParameter(MaterialKeys.PixelStageSurfaceFilter, Filter);
            }
        }
    }
}