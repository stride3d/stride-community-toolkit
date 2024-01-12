using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Example_2D_Playground;

public class Shape2DModel
{
    public required Primitive2DModelType Type { get; set; }
    public required Color Color { get; set; }
    public required Vector3 Size { get; set; }
    public Model? Model { get; set; }
}