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
public class Bullet2DPhysicsOptions : Primitive2DCreationOptions
{
    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="RigidbodyComponent"/>.
    /// This component allows the 2D primitive to interact with the physics system, enabling movement and collisions.
    /// </summary>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}