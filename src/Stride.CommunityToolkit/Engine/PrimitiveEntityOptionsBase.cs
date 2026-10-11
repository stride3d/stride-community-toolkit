using Stride.Rendering;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides common, non-physics options used when creating primitive entities.
/// </summary>
/// <remarks>
/// <para>This base type contains rendering, identification, and positioning options shared by 2D and 3D primitive creation helpers.</para>
/// <para>Physics-specific options are defined by derived types such as <c>Bepu2DPhysicsOptions</c>, <c>Bepu3DPhysicsOptions</c>, <c>Bullet2DPhysicsOptions</c>, and <c>Bullet3DPhysicsOptions</c>.</para>
/// </remarks>
public abstract class PrimitiveEntityOptionsBase
{
    /// <summary>
    /// Gets or sets the optional name assigned to the created entity.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the creation helper may assign a name or leave the entity unnamed.
    /// </remarks>
    public string? EntityName { get; set; }

    /// <summary>
    /// Gets or sets the material to apply to the generated primitive model.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the generated model uses the material behavior defined by the creation helper or the engine.
    /// </remarks>
    public Material? Material { get; set; }

    /// <summary>
    /// Gets or sets the render group assigned to the generated model component.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="RenderGroup.Group0"/>. Render groups can be used to route entities through different rendering passes or layers.
    /// </remarks>
    public RenderGroup RenderGroup { get; set; } = RenderGroup.Group0;

    /// <summary>
    /// Gets or sets the initial world position applied to the entity's transform.
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, the entity keeps the default transform position, <see cref="Vector3.Zero"/>.
    /// </remarks>
    public Vector3? Position { get; set; }

    /// <summary>
    /// Whether the physics engine chosen by the scene setup attaches a body to the entity. Defaults to
    /// <see langword="true"/>; <see langword="false"/> gives a mesh with no physics, whatever the setup chose.
    /// </summary>
    /// <remarks>
    /// Without a setup, or with <see cref="PhysicsSetupBase.None"/>, nothing is attached either way. The physics packages'
    /// entity extensions (<c>AddBepu3DPhysics</c>, <c>AddBullet3DPhysics</c>) attach physics explicitly whatever this says.
    /// </remarks>
    public bool IncludePhysics { get; set; } = true;
}