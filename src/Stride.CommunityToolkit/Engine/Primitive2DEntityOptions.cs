namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Option set for creating a 2D primitive entity (quad, rectangle, sprite-like plane) using the toolkit's code‑only helpers.
/// </summary>
/// <remarks>
/// Inherits common properties from <see cref="PrimitiveEntityOptions"/> and adds sizing + thickness information
/// suitable for 2D style content that still lives in a 3D world / physics space.
/// </remarks>
public class Primitive2DEntityOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the logical width/height for the generated 2D primitive. When <c>null</c> a shape‑appropriate
    /// default is chosen by the creation helper (e.g. unit quad).
    /// </summary>
    /// <remarks>X = width, Y = height.</remarks>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the depth (thickness) assigned to the generated 2D primitive. Defaults to <c>1</c>.
    /// </summary>
    /// <remarks>
    /// Even "2D" primitives often exist in a 3D simulation. A small Z thickness can improve collision stability
    /// or simplify shared 3D physics pipelines; constraints can still lock motion/rotation axes to emulate 2D.
    /// </remarks>
    public float Depth { get; set; } = 1;
}