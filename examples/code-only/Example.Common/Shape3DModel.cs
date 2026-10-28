using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Example.Common;

/// <summary>A 3D shape to make: its primitive, colour and size, and the model once one has been built for it.</summary>
public class Shape3DModel
{
    public required Primitive3D Type { get; set; }
    public required Color Color { get; set; }
    public required Vector3 Size { get; set; }
    public Model? Model { get; set; }
}