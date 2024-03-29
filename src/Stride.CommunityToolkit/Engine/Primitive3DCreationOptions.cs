using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides options for creating a primitive entity in a 3D scene.
/// </summary>
public class Primitive3DCreationOptions : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the primitive model. If null, default dimensions are used.
    /// </summary>
    public Vector3? Size { get; set; }

    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of RigidbodyComponent.
    /// </summary>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}