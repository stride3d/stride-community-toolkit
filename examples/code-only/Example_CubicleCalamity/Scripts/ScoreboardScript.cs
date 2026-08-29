using Example_CubicleCalamity.Gameplay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;

namespace Example_CubicleCalamity.Scripts;

/// <summary>
/// Draws the running total and the combo streak, and animates the total as it changes.
/// </summary>
/// <remarks>
/// The counter eases up to the real total rather than snapping to it. It is a small thing and it is
/// the single cheapest way to make scoring feel like an event: a number that visibly climbs is worth
/// watching, and the same number appearing instantly is just text. The keeper always holds the true
/// total - only the display lags - so nothing downstream has to know this is happening.
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
    /// Set through an object initialiser rather than a constructor, so the class keeps the public
    /// parameterless constructor Stride's STRDIAG010 analyser expects of a component, while
    /// <c>required</c> keeps the compiler enforcing that it is supplied.
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
    /// Assigned explicitly rather than looked up off the entity. Two components of the same type on
    /// one entity can only be told apart by the order they were added, which is a rename or a
    /// reorder away from silently swapping the two labels.
    /// </remarks>
    public EntityTextComponent? ComboText { get; set; }

    /// <summary>
    /// Gets or sets the text showing how much board is left: cubes standing, and how many clearable
    /// groups - moves - remain among them.
    /// </summary>
    /// <remarks>
    /// The moves number is what turns the endgame around: with a handful left, the player knows to
    /// orbit and hunt for them rather than wonder whether the board has quietly run out.
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
    /// The bar is a run of characters whose length follows <see cref="ScoreKeeper.ComboFraction"/> -
    /// no UI framework, no textures, just the same text renderer the rest of the HUD already uses.
    /// Chunky, but it makes the window something the player can race instead of something they
    /// discover when the multiplier vanishes.
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
            // Rate is set from whatever gap remains when the clear lands, so a huge clear counts up
            // faster rather than taking proportionally longer. Every clear feels the same length.
            _displayedScore += Math.Max((target - _displayedScore) / CountUpSeconds, 1f) * deltaTime;

            if (_displayedScore >= target)
            {
                _displayedScore = target;
            }
        }
        else if (_displayedScore > target)
        {
            // The score only ever drops on a restart; snap rather than count a million points down
            _displayedScore = target;
        }

        TotalText.Text = $"{Label}: {(int)_displayedScore:N0}";

        if (_punchRemaining > 0f)
        {
            _punchRemaining = Math.Max(0f, _punchRemaining - deltaTime);

            var punch = _punchRemaining / PunchDuration;

            TotalText.Scale = 1f + (PunchScale - 1f) * punch;
        }
        else
        {
            TotalText.Scale = 1f;
        }
    }

    private void UpdateCombo()
    {
        if (ComboText is null) return;

        if (!Keeper.HasCombo)
        {
            ComboText.IsVisible = false;

            if (ComboBarText is not null)
            {
                ComboBarText.IsVisible = false;
            }

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