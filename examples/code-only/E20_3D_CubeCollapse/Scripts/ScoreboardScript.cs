using CubeCollapse.Gameplay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;

namespace CubeCollapse.Scripts;

/// <summary>
/// Draws the running total and the combo streak, and animates the total as it changes.
/// </summary>
/// <remarks>
/// The counter eases up to the real total rather than snapping to it, so scoring reads as an event.
/// Only the display lags; the keeper always holds the true total.
/// </remarks>
public class ScoreboardScript : SyncScript
{
    private const string Label = "Total Score";
    private const float CountUpSeconds = 0.4f;
    private const float PunchDuration = 0.2f;
    private const float PunchScale = 1.25f;

    /// <summary>How many bar segments a full combo window shows.</summary>
    private const int ComboBarSegments = 20;

    private float _displayedScore;
    private float _punchRemaining;
    private int _countedAtCubes = -1;

    /// <summary>
    /// Gets the score keeper this reads from.
    /// </summary>
    /// <remarks>
    /// Set through an object initialiser rather than a constructor: a component needs a public
    /// parameterless constructor (STRDIAG010), and <c>required</c> still makes the compiler enforce it.
    /// </remarks>
    public required ScoreKeeper Keeper { get; init; }

    /// <summary>
    /// Gets or sets the text showing the running total.
    /// </summary>
    public EntityTextComponent? TotalText { get; set; }

    /// <summary>
    /// Gets or sets the text showing the combo multiplier.
    /// </summary>
    /// <remarks>
    /// Assigned explicitly rather than looked up off the entity: components of the same type on one
    /// entity differ only by the order they were added, so a reorder would silently swap the labels.
    /// </remarks>
    public EntityTextComponent? ComboText { get; set; }

    /// <summary>
    /// Gets or sets the text showing how much board is left: cubes standing, and how many clearable
    /// groups - moves - remain among them.
    /// </summary>
    /// <remarks>
    /// With a handful of moves left, the player knows to orbit and hunt for them.
    /// </remarks>
    public EntityTextComponent? RemainingText { get; set; }

    /// <summary>
    /// Gets or sets the grid the remaining count is read from.
    /// </summary>
    public CubeGrid? Grid { get; set; }

    /// <summary>
    /// Gets or sets the current level, shown ahead of the remaining count.
    /// </summary>
    public LevelState? Levels { get; set; }

    /// <summary>
    /// Gets or sets the draining bar under the combo, showing how much of the window is left.
    /// </summary>
    /// <remarks>
    /// A run of characters whose length follows <see cref="ScoreKeeper.ComboFraction"/>, drawn by the
    /// same text renderer as the rest of the HUD, so the player can race the window.
    /// </remarks>
    public EntityTextComponent? ComboBarText { get; set; }

    /// <inheritdoc />
    public override void Start() => _displayedScore = Keeper.TotalScore;

    /// <inheritdoc />
    public override void Update()
    {
        var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        Keeper.Update(deltaTime);

        UpdateTotal(deltaTime);
        UpdateCombo();
        UpdateRemaining();
    }

    private void UpdateRemaining()
    {
        if (RemainingText is null || Grid is null) return;

        // Counting moves walks the whole board, so only recount when the board has changed - and
        // every change to it (spawning, clearing, collapsing) moves the cube count
        if (Grid.Count == _countedAtCubes) return;

        _countedAtCubes = Grid.Count;

        var moves = MatchFinder.CountClearableGroups(Grid);
        var level = Levels is null ? string.Empty : $"Level {Levels.Current.Number}  -  ";

        RemainingText.Text = $"{level}{Grid.Count} cubes, {moves} {(moves == 1 ? "move" : "moves")}";
    }

    private void UpdateTotal(float deltaTime)
    {
        if (TotalText is null) return;

        var target = Keeper.TotalScore;

        if (_displayedScore < target)
        {
            // The rate is the remaining gap over CountUpSeconds, recomputed each frame, so a huge
            // clear eases in about as quickly as a small one
            _displayedScore += Math.Max((target - _displayedScore) / CountUpSeconds, 1f) * deltaTime;

            if (_displayedScore >= target) _displayedScore = target;
        }
        else if (_displayedScore > target)
        {
            // The score only ever drops on a restart; snap rather than count a million points down
            _displayedScore = target;
        }

        TotalText.Text = $"{Label}: {(int)_displayedScore:N0}";

        // A spent punch is zero, which is scale 1
        _punchRemaining = Math.Max(0f, _punchRemaining - deltaTime);

        TotalText.Scale = 1f + (PunchScale - 1f) * (_punchRemaining / PunchDuration);
    }

    private void UpdateCombo()
    {
        if (ComboText is null) return;

        if (!Keeper.HasCombo)
        {
            ComboText.IsVisible = false;

            if (ComboBarText is not null) ComboBarText.IsVisible = false;

            return;
        }

        var multiplier = ScoreRules.GetMultiplier(Keeper.ComboStep);

        ComboText.IsVisible = true;
        ComboText.Text = $"COMBO x{multiplier:0.#}";

        // Fades as the window runs out, so the streak is visibly expiring rather than just vanishing
        ComboText.Opacity = Math.Clamp(Keeper.ComboFraction * 1.5f, 0f, 1f);

        if (ComboBarText is null) return;

        // Ceiling, not rounding: the bar only reaches zero segments at the moment the combo lapses
        var segments = (int)MathF.Ceiling(Keeper.ComboFraction * ComboBarSegments);

        ComboBarText.IsVisible = true;
        ComboBarText.Text = new string('=', segments);
    }

    /// <summary>
    /// Makes the total jump, called when a clear has just been scored.
    /// </summary>
    public void Punch() => _punchRemaining = PunchDuration;
}