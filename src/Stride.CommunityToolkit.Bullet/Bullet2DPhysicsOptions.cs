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
    /// Gets or sets the Bullet physics component to attach. Defaults to a new <see cref="RigidbodyComponent"/>,
    /// except for the ground helper, which defaults to a <see cref="StaticColliderComponent"/>.
    /// <see langword="null"/> attaches no physics.
    /// </summary>
    public PhysicsComponent? PhysicsComponent
    {
        get => _physicsComponent;
        set
        {
            _physicsComponent = value;
            IsPhysicsComponentSet = true;
        }
    }

    /// <summary>Whether <see cref="PhysicsComponent"/> was set, so a helper can tell the default from a choice.</summary>
    internal bool IsPhysicsComponentSet { get; private set; }

    private PhysicsComponent? _physicsComponent = new RigidbodyComponent();

    /// <summary>
    /// Gets or sets the mass of the rigid body, in kilograms, or <see langword="null"/> for Bullet's default of 1.
    /// Applied when <see cref="PhysicsComponent"/> is a <see cref="RigidbodyComponent"/>.
    /// </summary>
    public float? Mass { get; set; }
}