namespace CubeCollapse.Gameplay;

/// <summary>
/// How big a clear was, used to pick the wording, size and colour of the score popup.
/// </summary>
public enum ScoreTier
{
    /// <summary>The smallest clear the rules allow.</summary>
    Good,

    /// <summary>A clear worth noticing.</summary>
    Nice,

    /// <summary>A large clear.</summary>
    Great,

    /// <summary>A very large clear.</summary>
    Huge,

    /// <summary>The kind the game is named after.</summary>
    Calamity
}