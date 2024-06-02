using Stride.Core.Mathematics;

namespace Example_CubicleCalamity.Shared;

public static class Constants
{
    public const int BasePointsPerCube = 10;
    public const float Interval = 0.33f;
    public const int MaxLayers = 2;
    public const int Rows = 10;

    public static readonly List<Color> Colours = [Color.Red, Color.Green, Color.Blue, Color.DarkGoldenrod];
    public static readonly Vector3 CubeSize = new(0.5f);
}