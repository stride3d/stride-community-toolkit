using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example13_MeshOutline;

/// <summary>
/// Marks an entity as outlined. Add it to any entity that has a <see cref="ModelComponent"/> and
/// <see cref="MeshOutlineRenderFeature"/> will draw an outline around that entity's mesh.
/// </summary>
/// <remarks>
/// Set <see cref="ActivableEntityComponent.Enabled"/> to <c>false</c> to hide the outline without
/// removing the component.
/// </remarks>
public class MeshOutlineComponent : ActivableEntityComponent
{
    /// <summary>
    /// Colour of the outline.
    /// </summary>
    public Color4 Color { get; set; } = Color4.White;

    /// <summary>
    /// Brightness multiplier applied to <see cref="Color"/>. Values above 1 push the outline into
    /// HDR range, which makes it glow once the bloom post effect is applied.
    /// </summary>
    public float Intensity { get; set; } = 1f;
}