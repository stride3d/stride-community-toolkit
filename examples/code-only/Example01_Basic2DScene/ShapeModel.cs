using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Example01_Basic2DScene;

public class ShapeModel
{
    public required ShapeType Type { get; set; }
    public required Color Color { get; set; }
    public required Vector3 Size { get; set; }
    public Model? Model { get; set; }
}