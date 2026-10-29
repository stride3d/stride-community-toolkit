namespace CubeCollapse.Gameplay;

/// <summary>
/// What one clear was worth, and how that total was arrived at.
/// </summary>
/// <param name="CubeCount">How many cubes were cleared.</param>
/// <param name="Base">Points for the cubes themselves.</param>
/// <param name="Bonus">Extra points for clearing them together.</param>
/// <param name="Multiplier">Combo multiplier applied to the sum of the two.</param>
/// <param name="Total">The points actually awarded.</param>
/// <param name="Tier">How big the clear was.</param>
/// <param name="ComboStep">Which step of the combo this clear was, starting at zero.</param>
public readonly record struct ScoreResult(
    int CubeCount,
    int Base,
    int Bonus,
    float Multiplier,
    int Total,
    ScoreTier Tier,
    int ComboStep)
{
    /// <summary>
    /// Gets a human-readable breakdown of the total.
    /// </summary>
    /// <remarks>
    /// Built from the same fields as the total, so the two cannot disagree.
    /// </remarks>
    public string Breakdown => Multiplier > 1f
        ? $"({Base} + {Bonus}) x {Multiplier:0.#} = {Total}"
        : $"{Base} + {Bonus} = {Total}";
}