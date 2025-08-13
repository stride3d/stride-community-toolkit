namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Option set for creating a 3D primitive entity (cube, sphere, capsule, plane, etc.).
/// </summary>
/// <remarks>
/// Extends <see cref="PrimitiveEntityOptions"/> with a size override. If <see cref="Size"/> is not provided,
/// the primitive factory chooses shape‑specific defaults (e.g. unit cube, radius 0.5 sphere).
/// </remarks>
public class Primitive3DEntityOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the desired size/dimensions for the 3D primitive model. When <c>null</c>, shape‑specific
    /// default dimensions are applied by the creation helper.
    /// </summary>
    /// <remarks>
    /// Interpretation depends on primitive type (e.g. box uses all components, sphere may derive radius
    /// from the largest component, capsules may map Y to height, etc.). Creation helpers document specifics.
    /// </remarks>
    public Vector3? Size { get; set; }
}