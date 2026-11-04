using Stride.Core.Mathematics;

namespace CubeCollapse.Shared;

/// <summary>
/// Every number that shapes the board or the pace of play, in one place so they are easy to tune.
/// </summary>
public static class GameSettings
{
    /// <summary>
    /// The largest board: cubes along each side of a layer at the size cap. The board actually
    /// played comes from <c>LevelRules</c> and grows level by level up to this.
    /// </summary>
    public const int Rows = 10;

    /// <summary>
    /// The most layers any board grows to. Also the height <c>CubeGrid</c> scans when collapsing a
    /// column, which is why it is a hard cap rather than a default.
    /// </summary>
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
    /// At 2.5 a thinking pause broke the streak, so blind spam-clicking held x5 and beat deliberate
    /// play about 1.7M to 0.7M (both styles simulated over five boards with the real rules). At 7 both
    /// keep the streak, the quadratic group bonus decides, and deliberate play leads 3.6M to 1.7M.
    /// </remarks>
    public const float ComboWindowSeconds = 7f;

    /// <summary>
    /// The colours the game starts with. A cube matches its neighbour when both carry the same one,
    /// so fewer colours means larger groups and an easier board. The player can switch to any set in
    /// <see cref="ColourPalettes"/> at runtime from the in-game key menu.
    /// </summary>
    public static readonly IReadOnlyList<Color> Colours = ColourPalettes.Classic.Colours;

    /// <summary>
    /// How many of a palette's colours the board plays with, two to four. Fewer colours means larger
    /// groups and an easier board; "--colours 3" on the command line overrides it for one run.
    /// </summary>
    public const int ColourCount = 4;

    /// <summary>Width, height and depth of one cube. The grid spacing follows from it.</summary>
    public static readonly Vector3 CubeSize = new(0.5f);
}