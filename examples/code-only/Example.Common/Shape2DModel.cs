using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Example.Common;

/// <summary>A 2D shape to make: its primitive and colour, and the model once one has been built for it.</summary>
public class Shape2DModel
{
    public required Primitive2D Type { get; set; }
    public required Color Color { get; set; }
    public Model? Model { get; set; }
}