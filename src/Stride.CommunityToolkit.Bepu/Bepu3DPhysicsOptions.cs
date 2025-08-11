using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Represents configuration options for the Bepu physics system.
/// </summary>
/// <remarks>This class provides options for configuring the physics behavior of entities in the Bepu physics
/// system. It allows customization of the physics component assigned to an entity, enabling the use of default or
/// custom components.</remarks>
public class Bepu3DPhysicsOptions : Primitive3DCreationOptions
{
    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="BodyComponent"/>.
    /// </summary>
    /// <remarks>
    /// By default, a <see cref="BodyComponent"/> is assigned to the entity to handle physics simulations,
    /// but you can override this with a custom physics component if needed.
    /// </remarks>
    public CollidableComponent Component { get; set; } = new BodyComponent()
    {
        Collider = new CompoundCollider()
    };
}