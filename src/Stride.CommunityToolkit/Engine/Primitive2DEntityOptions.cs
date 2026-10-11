namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Options for creating a 2D primitive entity: the name, material, render group and position of the entity, and the
/// depth a physics package gives its collider. The shape and its dimensions are the
/// <see cref="Rendering.ProceduralModels.Primitive2D"/> passed beside the options.
/// </summary>
public class Primitive2DEntityOptions : PrimitiveEntityOptionsBase
{
    /// <summary>
    /// The thickness along Z that a physics package gives the collider of the flat shape. Defaults to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// A 2D shape still lives in a 3D simulation. Some thickness keeps its collisions stable; the body's motion is
    /// kept in the plane by the physics package's 2D body, not by the shape.
    /// </remarks>
    public float Depth { get; set; } = 1;
}