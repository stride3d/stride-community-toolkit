using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Option set for creating a Bullet-physics enabled 3D primitive entity.
/// </summary>
/// <remarks>
/// Inherits geometry / rendering related settings from <see cref="Primitive3DEntityOptions"/> and adds a
/// configurable Bullet <see cref="PhysicsComponent"/> (default <see cref="RigidbodyComponent"/>) used to
/// participate in the Bullet simulation. Override <see cref="PhysicsComponent"/> to supply a custom component
/// (e.g. kinematic rigid body, character controller base, or a preconfigured collider shape).
/// </remarks>
public class Bullet3DPhysicsOptions : Primitive3DEntityOptions
{
    /// <summary>
    /// Gets or sets the Bullet physics component to attach. Defaults to a new <see cref="RigidbodyComponent"/>,
    /// except for the ground helpers, which default to a <see cref="StaticColliderComponent"/>.
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
    /// When true (default), a collider shape matching the primitive type is auto-created and added.
    /// When false, the <see cref="PhysicsComponent"/> is attached without shapes; you can add shapes later.
    /// </summary>
    public bool IncludeCollider { get; set; } = true;
}