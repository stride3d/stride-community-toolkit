using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Effects.Picking;

/// <summary>
/// The mesh feature's half of GPU picking: gives every mesh drawn in the picking stage the id its
/// pixels write, and remembers which <see cref="ModelComponent"/> each id stands for so a pixel
/// read back later can be turned into an entity again.
/// </summary>
/// <remarks>
/// A port of the render feature Game Studio's viewport selects entities with. It does its work
/// only on frames the picker has a request for: <see cref="Active"/> is set by the picker's
/// renderer during collection, before the render system extracts.
/// </remarks>
public sealed class GpuPickingRenderFeature : SubRenderFeature
{
    private readonly Dictionary<int, ModelComponent> _components = [];
    private Dictionary<RenderModel, RenderInstancing>? _instancing;
    private ObjectPropertyKey<Vector4> _idsKey;
    private ConstantBufferOffsetReference _pickingId;

    /// <summary>Whether a pick is pending this frame. Off, the feature costs nothing.</summary>
    internal bool Active { private get; set; }

    /// <summary>The component an id read back from the picking target stands for, if it was drawn in the last picking pass.</summary>
    /// <param name="id">The id, as written by the shader.</param>
    /// <returns>The component, or <see langword="null"/> for the cleared background or a component gone since.</returns>
    public ModelComponent? Find(int id) => _components.TryGetValue(id, out var component) ? component : null;

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        _idsKey = RootRenderFeature.RenderData.CreateObjectKey<Vector4>();
        _pickingId = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(GpuPickingShaderKeys.PickingId.Name);
    }

    /// <inheritdoc/>
    public override void Extract()
    {
        if (!Active) return;

        var ids = RootRenderFeature.RenderData.GetData(_idsKey);

        _components.Clear();
        _instancing = Context.VisibilityGroup is { } group && group.Tags.TryGetValue(InstancingRenderFeature.ModelToInstancingMap, out var map) ? map : null;

        foreach (var reference in RootRenderFeature.ObjectNodeReferences)
        {
            var node = RootRenderFeature.GetObjectNode(reference);

            if (node.RenderObject is not RenderMesh { Source: ModelComponent component } renderMesh) continue;

            // The runtime id is an integer assigned to an object once, never reused while it lives
            var id = RuntimeIdHelper.ToRuntimeId(component);

            _components[id] = component;
            ids[reference] = new Vector4(id, MeshIndex(component, renderMesh.Mesh), renderMesh.Mesh.MaterialIndex, 0f);
        }
    }

    /// <inheritdoc/>
    public override void Prepare(RenderDrawContext context)
    {
        if (!Active) return;

        var ids = RootRenderFeature.RenderData.GetData(_idsKey);

        foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
        {
            // Only the nodes drawn with the picking effect have the slot; every other stage's skip
            var perDrawLayout = renderNode.RenderEffect?.Reflection?.PerDrawLayout;

            if (perDrawLayout is null) continue;

            var offset = perDrawLayout.GetConstantBufferOffset(_pickingId);

            if (offset == -1) continue;

            Marshal.StructureToPtr(ids[renderNode.RenderObject.ObjectNode], renderNode.Resources.ConstantBuffer.Data + offset, false);

            if (_instancing is { } instancing && renderNode.RenderObject is RenderMesh { InstanceCount: > 0, RenderModel: { } model } && instancing.TryGetValue(model, out var renderInstancing))
            {
                BindInstancing(renderNode, renderInstancing);
            }
        }
    }

    /// <summary>
    /// Binds the instance buffers to the picking effect by name. The engine's instancing feature
    /// binds them by position, assuming the world buffer comes before its inverse in the per-draw
    /// descriptors, which holds for the forward effect but not for the picking effect, whose pixel
    /// stage uses neither: there the compiler lists them the other way round, and every instance
    /// would be drawn through its inverse matrix.
    /// </summary>
    private static void BindInstancing(in RenderNode renderNode, RenderInstancing instancing)
    {
        var entries = renderNode.RenderEffect.Effect?.Bytecode.Reflection.ResourceGroups.FirstOrDefault(group => group.Name == "PerDraw")?.Entries;

        if (entries is null) return;

        var slot = 0;

        foreach (var entry in entries)
        {
            if (entry.KeyInfo.KeyName == TransformationInstancingKeys.InstanceWorld.Name)
            {
                renderNode.Resources.DescriptorSet.SetShaderResourceView(slot, instancing.InstanceWorldBuffer);
            }
            else if (entry.KeyInfo.KeyName == TransformationInstancingKeys.InstanceWorldInverse.Name)
            {
                renderNode.Resources.DescriptorSet.SetShaderResourceView(slot, instancing.InstanceWorldInverseBuffer);
            }

            slot += entry.SlotCount;
        }
    }

    private static int MeshIndex(ModelComponent component, Mesh mesh)
    {
        var meshes = component.Model.Meshes;

        for (var i = 0; i < meshes.Count; i++)
        {
            if (meshes[i] == mesh) return i;
        }

        return 0;
    }
}