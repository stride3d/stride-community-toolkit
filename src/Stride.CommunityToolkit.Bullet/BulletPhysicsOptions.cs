using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// The options of a primitive entity with Bullet physics, 2D or 3D: the entity options plus the physics
/// component and the mass.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="PhysicsComponent"/> left <see langword="null"/>, <see cref="PrimitiveEntityOptions.Body"/> decides what
/// is attached: a <see cref="RigidbodyComponent"/>, the same body kinematic, or a <see cref="StaticColliderComponent"/>;
/// <see cref="BodyMode.None"/> attaches nothing. The component gets the collider shape that fits the primitive, and a
/// 2D shape's hull is as deep as the shape says.
/// </para>
/// <para>
/// A supplied <see cref="PhysicsComponent"/> wins over the mode and is used as it is. One that already holds a collider
/// shape keeps it: the fitted shape is added to an empty component only. A supplied component with
/// <see cref="BodyMode.None"/> is refused, since the two contradict each other.
/// </para>
/// <para>
/// Options without a component can serve any number of entities, each getting a fresh component. Options with a
/// supplied component serve one entity: a component belongs to one entity, and the engine refuses it on a second.
/// </para>
/// </remarks>
public class BulletPhysicsOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the Bullet physics component attached to the entity, or <see langword="null"/> to have one made
    /// from <see cref="PrimitiveEntityOptions.Body"/>.
    /// </summary>
    public PhysicsComponent? PhysicsComponent { get; set; }

    /// <summary>
    /// Gets or sets the mass of the rigid body, in kilograms, or <see langword="null"/> for Bullet's default of 1.
    /// </summary>
    /// <remarks>
    /// Bullet keeps mass on the rigid body. A static collider has no mass; a value given with
    /// <see cref="BodyMode.Static"/> is ignored with a line in the log.
    /// </remarks>
    public float? Mass { get; set; }
}