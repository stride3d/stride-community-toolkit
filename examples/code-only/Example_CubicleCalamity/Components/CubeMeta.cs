using Stride.Core.Mathematics;
using Stride.Engine;

namespace CubicleCalamity.Components;

public class CubeComponent : EntityComponent
{
    public Color Color { get; set; }

    public CubeComponent(Color color) => Color = color;
}