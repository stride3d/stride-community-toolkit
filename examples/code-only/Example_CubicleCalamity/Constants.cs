using Stride.Core.Mathematics;

namespace CubicleCalamity;

public static class Constants
{
    public const int BasePointsPerCube = 10;
    public const float Interval = 0.33f;
    public const int MaxLayers = 10;
    public const int Rows = 10;
    public static readonly List<Color> Colours = new() { Color.Red, Color.Green, Color.Blue };
    public static readonly Vector3 CubeSize = new(0.5f);
}