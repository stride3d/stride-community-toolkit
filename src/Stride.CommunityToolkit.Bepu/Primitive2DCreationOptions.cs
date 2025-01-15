using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu;

/// <summary>
/// Provides options for creating a 2D primitive entity, such as a square or rectangle.
/// Inherits common entity creation options from <see cref="PrimitiveCreationOptions"/>
/// and adds specific settings for 2D primitive models.
/// </summary>
public class Primitive2DCreationOptions : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the primitive model. If null, default dimensions are used.
    /// </summary>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the depth of the 2D primitive. Defaults to 1.
    /// The depth adds a third dimension (Z-axis) to the 2D object, making it slightly thicker than a flat object.
    /// This is useful for the physics engine, which may be optimized for 3D physics calculations.
    /// Even when handling 2D objects, the physics system often operates in 3D space with constraints applied to specific axes.
    /// </summary>
    public float Depth { get; set; } = 1;

    /// <summary>
    /// Gets or sets the physics component to be added to the entity.
    /// </summary>
    public CollidableComponent Component { get; set; } = new Body2DComponent() { Collider = new CompoundCollider() };
}