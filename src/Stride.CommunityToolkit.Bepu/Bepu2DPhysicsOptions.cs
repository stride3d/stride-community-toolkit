using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

public class Bepu2DPhysicsOptions : Primitive2DEntityOptions
{
    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="Body2DComponent"/>.
    /// This component allows the 2D primitive to interact with the physics system, enabling movement and collisions.
    /// </summary>
    public CollidableComponent Component { get; set; } = new Body2DComponent() { Collider = new CompoundCollider() };
}