using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Option set for creating a Bullet-physics enabled 2D-style primitive entity.
/// </summary>
/// <remarks>
/// Extends <see cref="Primitive2DEntityOptions"/> with a configurable Bullet <see cref="PhysicsComponent"/> (default
/// <see cref="RigidbodyComponent"/>). Even for 2D gameplay the underlying Bullet simulation is 3D; axis locking or
/// constraints at a higher level typically enforce planar motion.
/// </remarks>
public class Bullet2DPhysicsOptions : Primitive2DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bullet physics component to attach. Defaults to a new <see cref="RigidbodyComponent"/>.
    /// </summary>
    /// <remarks>
    /// Swap with a configured rigid body (e.g. static, kinematic) or a custom subclass prior to entity creation
    /// to tailor collision / motion behavior.
    /// </remarks>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();

    /// <summary>
    /// When true (default), a collider shape matching the primitive type is auto-created and added.
    /// When false, the <see cref="PhysicsComponent"/> is attached without shapes; you can add shapes later.
    /// </summary>
    public bool IncludeCollider { get; set; } = true;
}