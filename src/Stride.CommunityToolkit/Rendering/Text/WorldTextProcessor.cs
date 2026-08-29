using Stride.Engine;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Keeps track of every <see cref="WorldTextComponent"/> in the scene for the world text renderer.
/// </summary>
/// <remarks>
/// Registered automatically through the <c>DefaultEntityComponentProcessor</c> attribute on the
/// component, so nothing needs to add it. Collection through a processor covers the whole entity
/// hierarchy and gives each component's cached measurement a lifetime tied to the component itself.
/// </remarks>
public class WorldTextProcessor : EntityProcessor<WorldTextComponent, WorldTextRenderData>
{
    private readonly List<WorldTextRenderData> _texts = [];

    /// <summary>
    /// Gets every world text currently in the scene, in no particular order.
    /// </summary>
    public IReadOnlyList<WorldTextRenderData> Texts => _texts;

    /// <inheritdoc />
    protected override WorldTextRenderData GenerateComponentData(Entity entity, WorldTextComponent component)
        => new(component);

    /// <inheritdoc />
    protected override bool IsAssociatedDataValid(Entity entity, WorldTextComponent component, WorldTextRenderData associatedData)
        => associatedData.Component == component;

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, WorldTextComponent component, WorldTextRenderData data)
        => _texts.Add(data);

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, WorldTextComponent component, WorldTextRenderData data)
        => _texts.Remove(data);
}