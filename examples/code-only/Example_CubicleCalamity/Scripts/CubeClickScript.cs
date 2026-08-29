using Example_CubicleCalamity.Gameplay;
using Example_CubicleCalamity.Setup;
using Example_CubicleCalamity.Shared;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;

namespace Example_CubicleCalamity.Scripts;

/// <summary>
/// Turns a mouse click into a cleared group.
/// </summary>
/// <remarks>
/// This script does input and nothing else: raycast, ask <see cref="MatchFinder"/> what is connected,
/// hand the count to <see cref="ScoreKeeper"/>, tell <see cref="CubeGrid"/> to collapse, and spawn the
/// feedback. The rules themselves live in <c>Gameplay/</c> where they can be read and tested on their
/// own - this used to be one class doing input, matching, scoring, sound, popups and the running
/// total at once.
/// </remarks>
public class CubeClickScript : AsyncScript
{
    private readonly Random _drift = new(1);
    private Material? _letterMaterial;
    private Material? _digitMaterial;
    private Material? _menuMaterial;

    /// <summary>
    /// Gets the logical grid, which is the source of truth for what is where.
    /// </summary>
    /// <remarks>
    /// Set through an object initialiser rather than a constructor. Stride's STRDIAG010 analyser wants
    /// every component to have a public parameterless constructor for deserialisation, which rules out
    /// constructor injection - but <c>required</c> keeps the compiler enforcing that this is supplied,
    /// so nothing is lost by moving it.
    /// </remarks>
    public required CubeGrid Grid { get; init; }

    /// <summary>
    /// Gets the running total and combo streak.
    /// </summary>
    public required ScoreKeeper Keeper { get; init; }

    /// <summary>
    /// Gets the game's sound effects. Named Sounds rather than Audio, which would hide ScriptComponent.Audio.
    /// </summary>
    public required GameAudio Sounds { get; init; }

    /// <summary>
    /// Gets or sets the scoreboard, so a clear can make the total jump.
    /// </summary>
    public ScoreboardScript? Scoreboard { get; set; }

    /// <summary>
    /// Gets or sets the banner revealed when no clearable group is left.
    /// </summary>
    public EntityTextComponent? GameOverText { get; set; }

    /// <summary>
    /// Gets whether the board has run out of moves.
    /// </summary>
    public bool IsGameOver { get; private set; }

    /// <summary>
    /// Gets the current level, which knows where the platform's centre is - the game-over letters
    /// spawn relative to it.
    /// </summary>
    public required LevelState Levels { get; init; }

    /// <summary>
    /// Gets or sets what a restart actually does - rebuilding the board is the game controller's
    /// job, so this script only detects the request.
    /// </summary>
    public Action? RestartRequested { get; set; }

    /// <summary>
    /// Gets or sets what advancing to the next level does. Like <see cref="RestartRequested"/>,
    /// this script only detects the key.
    /// </summary>
    public Action? NextLevelRequested { get; set; }

    /// <inheritdoc />
    public override async Task Execute()
    {
        var camera = Entity.Scene.GetCamera();

        if (camera is null) return;

        while (Game.IsRunning)
        {
            if (!IsGameOver && Input.HasMouse && IsClicking())
            {
                TryClear(camera);
            }

            // The menu keys live only behind game over, so they cannot fire mid-game. Q also moves
            // the camera down (the controller owns Q/E), which does not matter for the frame in
            // which the game exits.
            if (IsGameOver)
            {
                if (Input.IsKeyPressed(Keys.R)) RestartRequested?.Invoke();
                if (Input.IsKeyPressed(Keys.N)) NextLevelRequested?.Invoke();
                if (Input.IsKeyPressed(Keys.Q)) ((Game)Game).Exit();
            }

            await Script.NextFrame();
        }
    }

    /// <summary>
    /// Whether the player is asking to clear a cube this frame.
    /// </summary>
    /// <remarks>
    /// Holding shift turns the click into a repeat, for taking a board apart quickly. Note that shift
    /// is also the camera controller's speed modifier, so holding it moves the camera faster too.
    /// </remarks>
    private bool IsClicking()
        => Input.IsKeyDown(Keys.LeftShift) && Input.IsMouseButtonDown(MouseButton.Left)
        || Input.IsMouseButtonPressed(MouseButton.Left);

    private void TryClear(CameraComponent camera)
    {
        if (!camera.RaycastMouse(this, 100, out var hitInfo)) return;

        var cube = hitInfo.Collidable.Entity;

        if (cube.Name != EntityNames.Cube) return;

        ClearFrom(cube);
    }

    /// <summary>
    /// Clears the group connected to a cube, scores it, and collapses the board - everything a click
    /// does once the raycast has decided what was clicked.
    /// </summary>
    /// <param name="cube">The cube the clear starts from.</param>
    private void ClearFrom(Entity cube)
    {
        var group = MatchFinder.FindGroup(Grid, cube);

        if (!MatchFinder.IsClearable(group.Count))
        {
            // A lone cube is a miss, not a mistake: a dull note, no popup, and the combo is left
            // alone so a stray click does not cost a streak the player earned
            Sounds.PlayRejected();

            return;
        }

        var result = Keeper.RegisterClear(group.Count);

        Sounds.PlayClear(group.Count);

        if (result.ComboStep > 0)
        {
            Sounds.PlayComboStep(result.ComboStep);
        }

        Log.Info($"Cleared {group.Count} {cube.Get<Components.CubeComponent>()?.Color} at {cube.Transform.Position}: {result.Breakdown}");

        AddScorePopup(cube.Transform.Position, result);

        // The grid collapses immediately, so the next click already matches the finished layout while
        // the cubes are still visibly falling into it. Nothing here moves a body: gravity does that,
        // and the two cannot disagree once they settle because SlidingCubeComponent pins each cube to
        // its own column, so the only place a cube can fall to is the slot the grid just gave it.
        Grid.RemoveAndCollapse(group);

        foreach (var cleared in group)
        {
            cleared.Remove();
        }

        Scoreboard?.Punch();

        CheckForGameOver();
    }

    /// <summary>
    /// Ends the game once no group of the minimum size is left anywhere on the board.
    /// </summary>
    /// <remarks>
    /// Checked after a clear rather than every frame, because the board can only become unplayable as
    /// a result of one. Cubes with no matching neighbour can never be removed once groups of two are
    /// required, so every game ends with some left standing - the point of this is to say so, instead
    /// of leaving the player clicking at a board that has quietly stopped responding.
    /// </remarks>
    private void CheckForGameOver()
    {
        if (IsGameOver || MatchFinder.HasClearableGroup(Grid)) return;

        IsGameOver = true;

        Log.Info($"No moves left. {Grid.Count} cubes stranded, final score {Keeper.TotalScore:N0}.");

        SpawnGameOverLetters();

        if (GameOverText is null) return;

        GameOverText.Text = $"{Keeper.TotalScore:N0} points\n{Grid.Count} cubes stranded";
        GameOverText.IsVisible = true;
    }

    /// <summary>
    /// Drops "GAME OVER" and the final score onto the board as solid physics letters.
    /// </summary>
    /// <remarks>
    /// The words spawn at increasing heights, so they land in sequence - GAME, then OVER, then the
    /// score in 3D digits raining down last. Everything tumbles off whatever is left of the platform,
    /// which is the whole fun of ending a physics game with physics.
    /// </remarks>
    private void SpawnGameOverLetters()
    {
        var scene = Entity.Scene;
        var game = (Game)Game;

        // Cached across restarts: materials are GPU resources, and a fresh set per game over would
        // leak three of them every time the player presses R
        var letterMaterial = _letterMaterial ??= game.CreateMaterial(Color.Gold, specular: 0.1f, microSurface: 0.4f);
        var digitMaterial = _digitMaterial ??= game.CreateMaterial(Color.White, specular: 0.1f, microSurface: 0.4f);
        var menuMaterial = _menuMaterial ??= game.CreateMaterial(new Color(170, 220, 255), specular: 0.1f, microSurface: 0.4f);

        // The player can be anywhere on the orbit when the board dies, so the words spawn turned
        // toward wherever the camera is right now. Facing is decided once, at spawn - after that the
        // letters are ordinary rigid bodies and tumble however they land.
        var yaw = 0f;
        var towardCamera = Vector3.UnitZ;
        var cameraEntity = scene.GetCamera()?.Entity;

        if (cameraEntity is not null)
        {
            var direction = cameraEntity.Transform.Position - Levels.Current.PlatformCentre;

            direction.Y = 0;

            if (direction.LengthSquared() > MathUtil.ZeroTolerance)
            {
                direction.Normalize();
                towardCamera = direction;
                yaw = MathF.Atan2(direction.X, direction.Z);
            }
        }

        // The two words are staggered along the view direction, so the nearer one never hides the
        // farther one whichever side the camera watches from
        FallingLetters.SpawnWord(game, scene, "GAME", new Vector3(0, 7f, 0) + towardCamera * 0.6f, letterMaterial, yaw, seed: 1);
        FallingLetters.SpawnWord(game, scene, "OVER", new Vector3(0, 9.5f, 0) - towardCamera * 0.6f, letterMaterial, yaw, seed: 2);
        FallingLetters.SpawnWord(game, scene, Keeper.TotalScore.ToString(), new Vector3(0, 12f, 0), digitMaterial, yaw, seed: 3);

        // The menu is static 3D lettering that keeps facing the camera - unlike the words above it
        // never falls, because a menu the player has to chase defeats its purpose
        FallingLetters.SpawnMenuLine(game, scene, "N - NEXT LEVEL", new Vector3(0, 5.8f, 0), menuMaterial);
        FallingLetters.SpawnMenuLine(game, scene, "R - RESTART", new Vector3(0, 5.0f, 0), menuMaterial);
        FallingLetters.SpawnMenuLine(game, scene, "Q - QUIT", new Vector3(0, 4.2f, 0), menuMaterial);
    }

    /// <summary>
    /// Clears the game-over state for a fresh board: hides the banner, removes the 3D menu, and
    /// accepts clicks again. The board itself is rebuilt by whoever invoked the restart.
    /// </summary>
    public void ResetForRestart()
    {
        IsGameOver = false;

        if (GameOverText is not null)
        {
            GameOverText.IsVisible = false;
        }

        foreach (var entity in Entity.Scene.Entities.Where(e => e.Name == EntityNames.GameOverMenu).ToList())
        {
            FallingLetters.ReleaseAndRemove(entity);
        }
    }

    private void AddScorePopup(Vector3 position, ScoreResult result)
    {
        var tierLabel = ScoreRules.GetTierLabel(result.Tier);
        var text = string.IsNullOrEmpty(tierLabel)
            ? $"{result.Total:N0}"
            : $"{tierLabel}\n{result.Total:N0}";

        if (result.Multiplier > 1f)
        {
            text += $"  x{result.Multiplier:0.#}";
        }

        var entity = new Entity(EntityNames.ScorePopup, position)
        {
            new EntityTextComponent()
            {
                Text = text,
                FontSize = GetFontSize(result.Tier),
                TextColor = GetTierColour(result.Tier),
                Anchor = TextAnchor.MiddleCenter,
                Alignment = Stride.Graphics.TextAlignment.Center,
                EnableShadow = true,
                LayerDepth = 1f,
            },
            new ScorePopupScript { HorizontalDrift = (float)(_drift.NextDouble() - 0.5) * 0.8f }
        };

        entity.Scene = SceneSystem.SceneInstance.RootScene;
    }

    private static float GetFontSize(ScoreTier tier) => tier switch
    {
        ScoreTier.Calamity => 34,
        ScoreTier.Huge => 30,
        ScoreTier.Great => 26,
        ScoreTier.Nice => 22,
        _ => 18,
    };

    /// <summary>
    /// Colour for each tier, warming as the clear gets bigger.
    /// </summary>
    private static Color GetTierColour(ScoreTier tier) => tier switch
    {
        ScoreTier.Calamity => new Color(255, 80, 80),
        ScoreTier.Huge => new Color(255, 150, 60),
        ScoreTier.Great => new Color(255, 210, 70),
        ScoreTier.Nice => new Color(180, 255, 130),
        _ => Color.White,
    };
}