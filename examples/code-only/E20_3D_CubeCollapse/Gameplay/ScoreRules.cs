using CubeCollapse.Shared;

namespace CubeCollapse.Gameplay;

/// <summary>
/// What a clear is worth. Pure arithmetic, with no dependency on the scene.
/// </summary>
/// <remarks>
/// Kept apart from everything that draws or removes anything, so the rules can be read on their own
/// and tested without a running game.
/// </remarks>
public static class ScoreRules
{
    /// <summary>
    /// Multiplier for each successive clear inside the combo window. The last entry repeats once the
    /// combo runs past the end of the table.
    /// </summary>
    private static readonly float[] _comboMultipliers = [1f, 1.5f, 2f, 3f, 5f];

    /// <summary>
    /// Scores a clear.
    /// </summary>
    /// <param name="cubeCount">How many cubes are being cleared.</param>
    /// <param name="comboStep">Which step of the combo this is, starting at zero for the first clear.</param>
    /// <returns>The points awarded and the parts they were made of.</returns>
    /// <remarks>
    /// The bonus is <c>n * (n - 1)</c> rather than <c>n * n</c>, so it is zero for a single cube by
    /// construction.
    /// </remarks>
    public static ScoreResult Score(int cubeCount, int comboStep)
    {
        if (cubeCount <= 0)
        {
            return new ScoreResult(0, 0, 0, 1f, 0, ScoreTier.Good, comboStep);
        }

        var baseScore = cubeCount * GameSettings.BasePointsPerCube;
        var bonus = cubeCount * (cubeCount - 1) * GameSettings.BasePointsPerCube;
        var multiplier = GetMultiplier(comboStep);
        var total = (int)MathF.Round((baseScore + bonus) * multiplier);

        return new ScoreResult(cubeCount, baseScore, bonus, multiplier, total, GetTier(cubeCount), comboStep);
    }

    /// <summary>
    /// Returns the combo multiplier for a given step.
    /// </summary>
    /// <param name="comboStep">Which step of the combo, starting at zero.</param>
    /// <returns>The multiplier applied to the clear.</returns>
    public static float GetMultiplier(int comboStep)
        => _comboMultipliers[Math.Clamp(comboStep, 0, _comboMultipliers.Length - 1)];

    /// <summary>
    /// Returns how big a clear counts as.
    /// </summary>
    /// <param name="cubeCount">How many cubes were cleared.</param>
    /// <returns>The tier, used for the wording and colour of the feedback.</returns>
    public static ScoreTier GetTier(int cubeCount) => cubeCount switch
    {
        >= 30 => ScoreTier.Calamity,
        >= 18 => ScoreTier.Huge,
        >= 10 => ScoreTier.Great,
        >= 5 => ScoreTier.Nice,
        _ => ScoreTier.Good,
    };

    /// <summary>
    /// Returns the word shown on the score popup for a tier.
    /// </summary>
    /// <param name="tier">The tier of the clear.</param>
    /// <returns>A short label, or an empty string for the smallest clears.</returns>
    public static string GetTierLabel(ScoreTier tier) => tier switch
    {
        ScoreTier.Calamity => "CALAMITY!",
        ScoreTier.Huge => "HUGE!",
        ScoreTier.Great => "GREAT!",
        ScoreTier.Nice => "NICE!",
        _ => string.Empty,
    };
}