using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Bepu.Engine;
public class Primitive2DCreationOptionsWithBepu : PrimitiveCreationOptions
{
    /// <summary>
    /// Gets or sets the size of the primitive model. If null, default dimensions are used.
    /// </summary>
    public Vector2? Size { get; set; }

    public float Depth { get; set; } = 1;

    /// <summary>
    /// Gets or sets the physics component to be added to the entity.
    /// </summary>
    public ContainerComponent Component { get; set; } = new Body2DComponent() { Collider = new CompoundCollider() };
}
