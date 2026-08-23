using Stride.Core.Mathematics;

namespace Example_CubicleCalamity.Shared;

/// <summary>
/// Every number that shapes the board or the pace of play, in one place so they can be tried out
/// without hunting through the game code for the one that matters.
/// </summary>
public static class GameSettings
{
    /// <summary>Cubes along each side of a layer, so a layer holds <c>Rows * Rows</c> cubes.</summary>
    public const int Rows = 10;

    /// <summary>How many layers the platform grows to.</summary>
    public const int MaxLayers = 10;

    /// <summary>Seconds between one layer spawning and the next.</summary>
    public const float Interval = 0.33f;

    /// <summary>Points a single cube is worth before any group bonus.</summary>
    public const int BasePointsPerCube = 10;

    /// <summary>Fewest connected cubes that may be cleared. A lone cube is not a match.</summary>
    public const int MinimumGroupSize = 2;

    /// <summary>
    /// Seconds after a clear during which the next one still counts as part of the same combo.
    /// </summary>
    /// <remarks>
    /// This was 2.5, which quietly inverted the game's incentive: a thinking pause is longer than
    /// that, so deliberate play ran at x1 while blind spam-clicking held a permanent x5 and beat it
    /// roughly 1.7M to 0.7M (measured by simulating both styles over five boards with the real
    /// rules). At 7 seconds both keep the streak, the quadratic group bonus decides the winner, and
    /// the same simulation puts deliberate play ahead 3.6M to 1.7M. The combo now punishes
    /// stalling, not thinking.
    /// </remarks>
    public const float ComboWindowSeconds = 7f;

    /// <summary>
    /// The colours a cube can take. A cube matches its neighbour when both carry the same one, so
    /// fewer colours here means larger groups and an easier board.
    /// </summary>
    public static readonly List<Color> Colours = [Color.Red, Color.Green, Color.Blue, Color.DarkGoldenrod];

    /// <summary>Width, height and depth of one cube. The grid spacing follows from it.</summary>
    public static readonly Vector3 CubeSize = new(0.5f);

    /// <summary>
    /// Offset applied to the X and Z of every cube so the platform is centred on the ground's origin
    /// rather than growing out of one corner, whatever <see cref="Rows"/> is set to.
    /// </summary>
    /// <remarks>
    /// Cube centres sit at <c>GridOrigin + i * CubeSize</c> for <c>i</c> in <c>[0, Rows)</c>, so the
    /// footprint spans <c>(Rows - 1) * CubeSize</c> and pulling it back by half of that puts its
    /// middle on zero. At 10 rows of 0.5 that is -2.25, at 5 rows it is -1.
    /// </remarks>
    public static readonly float GridOrigin = -(Rows - 1) * CubeSize.X * 0.5f;

    /// <summary>
    /// Middle of the finished platform, which is what the camera orbits and looks at.
    /// </summary>
    public static readonly Vector3 PlatformCentre = new(0, MaxLayers * CubeSize.Y * 0.5f, 0);
}