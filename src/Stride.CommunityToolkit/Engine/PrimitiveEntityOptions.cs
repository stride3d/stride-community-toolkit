using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides options for creating a primitive entity, such as a cube, sphere, or other 3D object.
/// These options allow customization of the entity's name, material, collider inclusion, and render group.
/// </summary>
public abstract class PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the name of the entity.
    /// This can be useful for identifying the entity within the scene or debugging purposes.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the material to be applied to the primitive model.
    /// The material defines the appearance of the primitive, including its color and texture.
    /// </summary>
    public Material? Material { get; set; }

    /// <summary>
    /// Gets or sets the render group for the entity. Defaults to <see cref="RenderGroup.Group0"/>.
    /// Render groups allow different entities to be rendered in separate stages or layers,
    /// which can be useful for organizing complex scenes or applying different rendering effects.
    /// </summary>
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;
}