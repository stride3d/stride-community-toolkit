using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Base (non-physics) option set used when creating primitive entities (2D or 3D).
/// </summary>
/// <remarks>
/// This abstraction intentionally contains only rendering / identification related settings.
/// Physics specific configuration is layered in derived option types such as
/// <c>Bepu2DPhysicsOptions</c>, <c>Bepu3DPhysicsOptions</c>, <c>Bullet2DPhysicsOptions</c>, and <c>Bullet3DPhysicsOptions</c>.
/// Additional shared options can be added here as they become engine‑agnostic.
/// </remarks>
public abstract class PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the (optional) human friendly name for the entity to aid scene inspection or debugging.
    /// If <c>null</c>, a name may be assigned by higher level factory helpers or remain empty.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the material to apply to the generated primitive model (if it creates a visual component).
    /// When <c>null</c>, a default toolkit / engine material is typically assigned by the creation helper.
    /// </summary>
    public Material? Material { get; set; }

    /// <summary>
    /// Gets or sets the render group for the entity. Defaults to <see cref="RenderGroup.Group0"/>.
    /// Use render groups to route subsets of entities through specific rendering passes or layers
    /// (e.g. separating world geometry, UI routed meshes, debug overlays, etc.).
    /// </summary>
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;
}