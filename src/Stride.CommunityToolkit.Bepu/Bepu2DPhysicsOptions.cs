using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Option set for creating a Bepu-physics enabled 2D-style primitive entity.
/// </summary>
/// <remarks>
/// Extends <see cref="Primitive2DEntityOptions"/> with a Bepu <see cref="CollidableComponent"/>. The default is a dynamic
/// <see cref="Body2DComponent"/> having an empty <see cref="CompoundCollider"/>. Although termed "2D", simulation occurs
/// in 3D space with constraints limiting motion to the XY plane.
/// </remarks>
public class Bepu2DPhysicsOptions : Primitive2DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bepu collidable component to attach. Defaults to a new <see cref="Body2DComponent"/> with an empty
    /// <see cref="CompoundCollider"/>.
    /// </summary>
    public CollidableComponent Component { get; set; } = new Body2DComponent() { Collider = new CompoundCollider() };

    /// <summary>
    /// When true (default), a collider shape matching the primitive type is auto-created and added.
    /// When false, the <see cref="CollidableComponent"/> is attached without shapes; you can add shapes later.
    /// </summary>
    public bool IncludeCollider { get; set; } = true;
}