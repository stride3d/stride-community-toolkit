namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides options for creating a primitive entity in a 3D scene.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="PrimitiveEntityOptions"/> and extends it with properties
/// specific to 3D models, such as size, entity name and material.
/// </remarks>
public class Primitive3DEntityOptions : PrimitiveEntityOptions
{
    /// <summary>
    /// Gets or sets the size of the 3D primitive model. If null, default dimensions are used.
    /// </summary>
    /// <remarks>
    /// The <see cref="Size"/> property allows you to specify custom dimensions for the 3D model.
    /// If no size is specified, default dimensions will be applied, based on the type of primitive.
    /// </remarks>
    public Vector3? Size { get; set; }
}