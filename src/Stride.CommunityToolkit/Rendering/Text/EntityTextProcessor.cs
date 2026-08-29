using Stride.Engine;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Keeps track of every <see cref="EntityTextComponent"/> in the scene for the text renderer to draw.
/// </summary>
/// <remarks>
/// <para>
/// Registered automatically through the <c>DefaultEntityComponentProcessor</c> attribute on the
/// component, so nothing needs to add it by hand.
/// </para>
/// <para>
/// It replaces the renderer walking the scene's entity list each frame, which had two consequences
/// worth naming because both looked like the renderer being broken. Only top-level entities were
/// ever visited, so a label added as a child of another entity - the obvious way to attach a label to
/// a thing - never drew at all. And the cached text measurements were keyed on entities that the
/// renderer had no way of knowing had been removed, so every short-lived label leaked one entry.
/// Collection through a processor fixes both: the engine reports components arriving and leaving
/// wherever they sit in the hierarchy.
/// </para>
/// </remarks>
public class EntityTextProcessor : EntityProcessor<EntityTextComponent, EntityTextRenderData>
{
    private readonly List<EntityTextRenderData> _texts = [];

    /// <summary>
    /// Gets every text currently in the scene, in no particular order.
    /// </summary>
    public IReadOnlyList<EntityTextRenderData> Texts => _texts;

    /// <inheritdoc />
    protected override EntityTextRenderData GenerateComponentData(Entity entity, EntityTextComponent component)
        => new(component);

    /// <inheritdoc />
    protected override bool IsAssociatedDataValid(Entity entity, EntityTextComponent component, EntityTextRenderData associatedData)
        => associatedData.Component == component;

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, EntityTextComponent component, EntityTextRenderData data)
        => _texts.Add(data);

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, EntityTextComponent component, EntityTextRenderData data)
        => _texts.Remove(data);
}