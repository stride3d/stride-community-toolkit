using Stride.CommunityToolkit.Rendering.ProceduralModels;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Option set for creating a 2D primitive entity using the toolkit's code-only helpers.
/// </summary>
/// <remarks>
/// Inherits common properties from <see cref="PrimitiveEntityOptions"/> and adds sizing + thickness information
/// suitable for 2D style content that still lives in a 3D world / physics space.
/// </remarks>
public class Primitive2DEntityOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the logical size values for the generated 2D primitive.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, a shape-appropriate default is chosen by the creation helper.
    /// The meaning of <see cref="Size"/> components depends on the primitive type being created:
    /// <para /> - For rectangular shapes: X = width, Y = height.
    /// <para /> - For cylindrical shapes: X = radius, Y = length.
    /// <para /> - For regular polygons: X = radius, Y = number of sides.
    /// </remarks>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets custom polygon vertices in the XY plane.
    /// </summary>
    /// <remarks>
    /// Used only with <see cref="Primitive2DModelType.Polygon"/>. When provided, vertices take precedence over <see cref="Size"/>.
    /// The polygon must contain at least 3 unique vertices and should be ordered around a convex outline.
    /// </remarks>
    public Vector2[]? Vertices { get; set; }

    /// <summary>
    /// Gets or sets the depth (thickness) assigned to the generated 2D primitive. Defaults to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// Even "2D" primitives often exist in a 3D simulation. A small Z thickness can improve collision stability
    /// or simplify shared 3D physics pipelines; constraints can still lock motion/rotation axes to emulate 2D.
    /// </remarks>
    public float Depth { get; set; } = 1;
}