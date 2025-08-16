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
    /// Gets or sets the Bullet physics component to attach. Defaults to a new <see cref="RigidbodyComponent"/>.
    /// </summary>
    /// <remarks>
    /// Replace with an alternative <see cref="PhysicsComponent"/> (e.g. static or kinematic) to change simulation behavior
    /// before the entity is created/added to a scene.
    /// </remarks>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}