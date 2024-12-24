using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Bullet;

/// <summary>
/// Provides options for creating a 2D primitive entity, such as a square or rectangle.
/// Inherits common entity creation options from <see cref="PrimitiveCreationOptions"/>
/// and adds specific settings for 2D primitive models.
/// </summary>
public class Primitive2DCreationOptions : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the 2D primitive model.
    /// If null, default size values will be used. The <see cref="Vector2"/> represents width (X) and height (Y) dimensions.
    /// </summary>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the depth of the 2D primitive. Defaults to 0.04f.
    /// The depth adds a third dimension (Z-axis) to the 2D object, making it slightly thicker than a flat object.
    /// This is useful for the physics engine, which may be optimized for 3D physics calculations.
    /// Even when handling 2D objects, the physics system often operates in 3D space with constraints applied to specific axes.
    /// </summary>
    public float Depth { get; set; } = 0.04f;

    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="RigidbodyComponent"/>.
    /// This component allows the 2D primitive to interact with the physics system, enabling movement and collisions.
    /// </summary>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}