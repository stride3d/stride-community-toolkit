namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Option set for creating a 3D primitive entity (cube, sphere, capsule, plane, etc.).
/// </summary>
/// <remarks>
/// Extends <see cref="PrimitiveEntityOptions"/> with a size override. If <see cref="Size"/> is not provided,
/// the primitive factory chooses shape‑specific defaults (e.g., unit cube, radius 0.5 sphere).
/// </remarks>
public class Primitive3DEntityOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the desired size/dimensions for the 3D primitive model. When <c>null</c>, the creation helper
    ///  applies shape‑specific default dimensions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>How this value is read depends on the primitive, and the conventions differ.</strong> Box-like
    /// shapes take full extents, while round shapes take a <em>radius</em> — that is, a half extent. Passing a
    /// diameter to a sphere therefore produces a model twice the intended size.
    /// </para>
    /// <list type="table">
    ///   <listheader><term>Primitive</term><description> Interpretation</description></listheader>
    ///   <item><term>Cube, RectangularPrism, TriangularPrism</term><description> Full extent along each axis.</description></item>
    ///   <item><term>Plane, InfinitePlane</term><description> <c>X</c> and <c>Z</c> are the full extents.</description></item>
    ///   <item><term>Sphere</term><description> <c>X</c> is the <em>radius</em>.</description></item>
    ///   <item><term>Capsule</term><description> <c>X</c> is the radius, <c>Y</c> the length of the cylindrical section.</description></item>
    ///   <item><term>Cylinder</term><description> <c>X</c> is the radius, <c>Z</c> the height.</description></item>
    ///   <item><term>Cone</term><description> <c>X</c> is the radius, <c>Y</c> the height.</description></item>
    ///   <item><term>Torus</term><description> <c>X</c> is the major radius, <c>Y</c> the thickness.</description></item>
    ///   <item><term>Teapot</term><description> <c>X</c> is a uniform size.</description></item>
    /// </list>
    /// <para>
    /// The generated model and the generated physics collider read this value the same way, so a primitive
    /// created with a collider always matches its mesh. They can only disagree if a collider is supplied by
    /// hand, in which case it must follow the same convention as the row above.
    /// </para>
    /// </remarks>
    /// <example>
    /// A sphere one unit across, and a cube one unit across:
    /// <code>
    /// game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { Size = new Vector3(0.5f) }); // radius
    /// game.Create3DPrimitive(PrimitiveModelType.Cube, new() { Size = new Vector3(1f) });     // full extent
    /// </code>
    /// </example>
    public Vector3? Size { get; set; }
}