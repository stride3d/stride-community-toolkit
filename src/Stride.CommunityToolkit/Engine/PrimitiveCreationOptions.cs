using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

public abstract class PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the name of the entity.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the material to be applied to the primitive model.
    /// </summary>
    public Material? Material { get; set; }

    /// <summary>
    /// Determines whether to include a collider component in the entity. Defaults to true.
    /// </summary>
    public bool IncludeCollider { get; set; } = true;

    /// <summary>
    /// Gets or sets the render group for the entity. Defaults to RenderGroup.Group0.
    /// </summary>
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;
}