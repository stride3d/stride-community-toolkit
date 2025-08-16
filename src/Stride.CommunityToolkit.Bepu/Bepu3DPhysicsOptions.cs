using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Option set for creating a Bepu-physics enabled 3D primitive entity.
/// </summary>
/// <remarks>
/// Inherits geometry / rendering options from <see cref="Primitive3DEntityOptions"/> and adds a configurable Bepu
/// <see cref="CollidableComponent"/>. The default is a dynamic <see cref="BodyComponent"/> with an empty
/// <see cref="CompoundCollider"/>; shape(s) are typically populated later by helper extensions based on the chosen primitive type.
/// </remarks>
public class Bepu3DPhysicsOptions : Primitive3DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bepu collidable component to attach. Defaults to a new dynamic <see cref="BodyComponent"/>
    /// containing an empty <see cref="CompoundCollider"/>.
    /// </summary>
    /// <remarks>
    /// Replace with a <see cref="StaticComponent"/> for immovable geometry or preconfigure collider children before
    /// passing the options instance to a creation helper.
    /// </remarks>
    public CollidableComponent Component { get; set; } = new BodyComponent()
    {
        Collider = new CompoundCollider()
    };
}