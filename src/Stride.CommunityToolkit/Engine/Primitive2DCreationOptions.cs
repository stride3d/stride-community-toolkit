using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Engine;

public class Primitive2DCreationOptions : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the primitive model. If null, default dimensions are used.
    /// </summary>
    public Vector2? Size { get; set; }

    public float Depth { get; set; } = 0.04f;

    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of RigidbodyComponent.
    /// </summary>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}