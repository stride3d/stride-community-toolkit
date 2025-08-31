using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Example.Common;

/// <summary>
/// Represents a 2D shape model with a specified type, color, size, and an optional associated model.
/// </summary>
/// <remarks>This class is used to define the properties of a 2D shape, including its type, color, and size. An
/// optional model can also be associated with the shape for additional context or rendering purposes.</remarks>
public class Shape2DModel
{
    public required Primitive2DModelType Type { get; set; }
    public required Color Color { get; set; }
    public required Vector2 Size { get; set; }
    public Model? Model { get; set; }
}