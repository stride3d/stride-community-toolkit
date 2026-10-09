using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Keeps track of every <see cref="EntityTextComponent"/> in the scene for the text renderer to draw.
/// </summary>
/// <remarks>
/// <para>
/// Registered automatically through the <c>DefaultEntityComponentProcessor</c> attribute on the
/// component, so nothing needs to add it by hand. It also makes sure an
/// <see cref="EntityTextRenderer"/> is on the scene's compositor, which is what draws the
/// components in Game Studio's scene editor and in a game that never called
/// <c>AddEntityTextRenderer</c>.
/// </para>
/// <para>
/// Components are tracked wherever they sit in the entity hierarchy, including on child entities.
/// </para>
/// </remarks>
public class EntityTextProcessor : EntityProcessor<EntityTextComponent, EntityTextRenderData>
{
    private readonly List<EntityTextRenderData> _texts = [];

    // The compositor the renderer was last ensured on; the editor swaps compositors
    private GraphicsCompositor? _ensuredOn;

    /// <summary>
    /// Gets every text currently in the scene, in no particular order.
    /// </summary>
    public IReadOnlyList<EntityTextRenderData> Texts => _texts;

    /// <inheritdoc />
    public override void Draw(RenderContext context)
    {
        base.Draw(context);

        if (_texts.Count == 0) return;

        SceneRendererRegistration.Ensure(Services, EntityManager, ref _ensuredOn, () => new EntityTextRenderer());
    }

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