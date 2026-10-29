using CubeCollapse.Shared;

namespace CubeCollapse.Gameplay;

/// <summary>
/// The running total and the combo streak.
/// </summary>
/// <remarks>
/// Deliberately knows nothing about entities, input or drawing: it is handed a cube count and returns
/// what that was worth. The click script feeds it, the scoreboard reads it, and neither has to know
/// how the other works.
/// </remarks>
public class ScoreKeeper
{
    private float _comboRemaining;

    /// <summary>Gets the running total.</summary>
    public int TotalScore { get; private set; }

    /// <summary>
    /// Gets which step of the combo the next clear would be, starting at zero.
    /// </summary>
    public int ComboStep { get; private set; }

    /// <summary>
    /// Gets how much of the combo window is left, from 1 just after a clear down to 0 when it lapses.
    /// </summary>
    /// <remarks>
    /// Exposed as a fraction rather than as seconds so the display does not have to know the window
    /// length to draw a bar or fade a colour.
    /// </remarks>
    public float ComboFraction => GameSettings.ComboWindowSeconds <= 0f
        ? 0f
        : Math.Clamp(_comboRemaining / GameSettings.ComboWindowSeconds, 0f, 1f);

    /// <summary>
    /// Gets whether a combo is currently running, meaning the next clear is worth more.
    /// </summary>
    public bool HasCombo => ComboStep > 0 && _comboRemaining > 0f;

    /// <summary>
    /// Puts the keeper back to a fresh game: zero score, no combo.
    /// </summary>
    public void Reset()
    {
        TotalScore = 0;
        ComboStep = 0;
        _comboRemaining = 0f;
    }

    /// <summary>
    /// Scores a clear, extends the combo, and adds the result to the total.
    /// </summary>
    /// <param name="cubeCount">How many cubes were cleared.</param>
    /// <returns>What the clear was worth.</returns>
    public ScoreResult RegisterClear(int cubeCount)
    {
        var result = ScoreRules.Score(cubeCount, ComboStep);

        TotalScore += result.Total;

        ComboStep++;
        _comboRemaining = GameSettings.ComboWindowSeconds;

        return result;
    }

    /// <summary>
    /// Advances the combo window.
    /// </summary>
    /// <param name="deltaTime">Seconds since the last update.</param>
    /// <remarks>
    /// The streak lapses on time rather than on a miss, so hesitating costs the multiplier but a
    /// misjudged click does not.
    /// </remarks>
    public void Update(float deltaTime)
    {
        if (_comboRemaining <= 0f) return;

        _comboRemaining -= deltaTime;

        if (_comboRemaining > 0f) return;

        _comboRemaining = 0f;
        ComboStep = 0;
    }
}