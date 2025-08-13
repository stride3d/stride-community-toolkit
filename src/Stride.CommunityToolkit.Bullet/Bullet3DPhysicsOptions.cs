using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Represents configuration options for the Bullet physics system.
/// </summary>
/// <remarks>This class provides options for configuring the physics behavior of entities in the Bullet physics
/// system. It allows customization of the physics component assigned to an entity, enabling the use of default or
/// custom components.</remarks>
public class Bullet3DPhysicsOptions : Primitive3DEntityOptions
{
    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="RigidbodyComponent"/>.
    /// </summary>
    /// <remarks>
    /// By default, a <see cref="RigidbodyComponent"/> is assigned to the entity to handle physics simulations,
    /// but you can override this with a custom physics component if needed.
    /// </remarks>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}