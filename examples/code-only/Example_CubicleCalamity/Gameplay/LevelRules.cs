using Example_CubicleCalamity.Shared;
using Stride.Core.Mathematics;

namespace Example_CubicleCalamity.Gameplay;

/// <summary>
/// One level's board: its number and its dimensions, with the measurements that follow from them.
/// </summary>
/// <param name="Number">The level, counting from 1.</param>
/// <param name="Rows">Cubes along each horizontal side.</param>
/// <param name="Layers">How many layers the platform grows to.</param>
public readonly record struct LevelDefinition(int Number, int Rows, int Layers)
{
    /// <summary>
    /// Offset applied to the X and Z of every cube so the platform centres on the ground's origin
    /// rather than growing out of one corner, whatever <see cref="Rows"/> is.
    /// </summary>
    /// <remarks>
    /// Cube centres sit at <c>GridOrigin + i * CubeSize</c> for <c>i</c> in <c>[0, Rows)</c>, so the
    /// footprint spans <c>(Rows - 1) * CubeSize</c> and pulling it back by half of that puts its
    /// middle on zero.
    /// </remarks>
    public float GridOrigin => -(Rows - 1) * GameSettings.CubeSize.X * 0.5f;

    /// <summary>
    /// Middle of the finished platform, which is what the camera orbits and the game-over letters
    /// face away from.
    /// </summary>
    public Vector3 PlatformCentre => new(0, Layers * GameSettings.CubeSize.Y * 0.5f, 0);
}

/// <summary>
/// How the board grows from level to level. Pure arithmetic, testable without a running game.
/// </summary>
public static class LevelRules
{
    /// <summary>Cubes along each side of the first level's board.</summary>
    public const int StartingSize = 5;

    /// <summary>
    /// Returns the board for a level: 5x5x5 at level one, one cube larger per side each level,
    /// capped at the full board once it gets there.
    /// </summary>
    /// <param name="number">The level, counting from 1. Values below 1 are treated as 1.</param>
    /// <returns>The level's board definition.</returns>
    /// <remarks>
    /// The cap matters twice over: <see cref="GameSettings.Rows"/> is the size the physics solver
    /// settings were tuned against, and <see cref="CubeGrid"/> scans columns up to
    /// <see cref="GameSettings.MaxLayers"/> when it collapses them. Levels past the cap keep the
    /// full board and rely on the score carrying over to stay worth playing.
    /// </remarks>
    public static LevelDefinition ForNumber(int number)
    {
        var level = Math.Max(1, number);
        var size = Math.Min(StartingSize + level - 1, GameSettings.Rows);

        return new LevelDefinition(level, size, Math.Min(size, GameSettings.MaxLayers));
    }
}

/// <summary>
/// The one shared answer to "which board are we on?" - everything that depends on the current
/// level's dimensions reads it from here.
/// </summary>
/// <remarks>
/// A plain mutable holder rather than a value passed around, because the level changes at runtime
/// and the spawner, the click script and the scoreboard must all agree at the moment it does.
/// </remarks>
public sealed class LevelState
{
    /// <summary>Gets or sets the level currently being played.</summary>
    public LevelDefinition Current { get; set; } = LevelRules.ForNumber(1);
}