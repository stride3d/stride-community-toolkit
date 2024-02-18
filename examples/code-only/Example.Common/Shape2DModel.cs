using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Example.Common;

public class Shape2DModel
{
    public required Primitive2DModelType Type { get; set; }
    public required Color Color { get; set; }
    public required Vector2 Size { get; set; }
    public Model? Model { get; set; }
}