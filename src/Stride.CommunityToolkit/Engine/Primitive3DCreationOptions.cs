using Stride.Engine;
using Stride.Physics;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides options for creating a primitive entity in a 3D scene.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="PrimitiveCreationOptions"/> and extends it with properties
/// specific to 3D models, such as size and physics components.
/// </remarks>
public class Primitive3DCreationOptions : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the 3D primitive model. If null, default dimensions are used.
    /// </summary>
    /// <remarks>
    /// The <see cref="Size"/> property allows you to specify custom dimensions for the 3D model.
    /// If no size is specified, default dimensions will be applied, based on the type of primitive.
    /// </remarks>
    public Vector3? Size { get; set; }

    /// <summary>
    /// Gets or sets the physics component to be added to the entity. Defaults to a new instance of <see cref="RigidbodyComponent"/>.
    /// </summary>
    /// <remarks>
    /// By default, a <see cref="RigidbodyComponent"/> is assigned to the entity to handle physics simulations,
    /// but you can override this with a custom physics component if needed.
    /// </remarks>
    public PhysicsComponent? PhysicsComponent { get; set; } = new RigidbodyComponent();
}