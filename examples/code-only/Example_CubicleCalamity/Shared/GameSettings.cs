using Stride.Core.Mathematics;

namespace Example_CubicleCalamity.Shared;

/// <summary>
/// Every number that shapes the board or the pace of play, in one place so they can be tried out
/// without hunting through the game code for the one that matters.
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
    /// This was 2.5, which quietly inverted the game's incentive: a thinking pause is longer than
    /// that, so deliberate play ran at x1 while blind spam-clicking held a permanent x5 and beat it
    /// roughly 1.7M to 0.7M (measured by simulating both styles over five boards with the real
    /// rules). At 7 seconds both keep the streak, the quadratic group bonus decides the winner, and
    /// the same simulation puts deliberate play ahead 3.6M to 1.7M. The combo now punishes
    /// stalling, not thinking.
    /// </remarks>
    public const float ComboWindowSeconds = 7f;

    /// <summary>
    /// The colours the game starts with. A cube matches its neighbour when both carry the same one,
    /// so fewer colours means larger groups and an easier board. The player can switch to any set in
    /// <see cref="ColourPalettes"/> at runtime from the in-game dropdown.
    /// </summary>
    public static readonly IReadOnlyList<Color> Colours = ColourPalettes.Classic.Colours;

    /// <summary>Width, height and depth of one cube. The grid spacing follows from it.</summary>
    public static readonly Vector3 CubeSize = new(0.5f);
}