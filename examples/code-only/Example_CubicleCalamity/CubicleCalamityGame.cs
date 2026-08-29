using Example_CubicleCalamity.Components;
using Example_CubicleCalamity.Gameplay;
using Example_CubicleCalamity.Scripts;
using Example_CubicleCalamity.Setup;
using Example_CubicleCalamity.Shared;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

namespace Example_CubicleCalamity;

/// <summary>
/// Runs Cubicle Calamity: builds the scene once, then grows the platform a layer at a time until it
/// is complete and hands it over to the physics.
/// </summary>
/// <remarks>
/// This is the piece a reader should start from. It owns the order things happen in and delegates the
/// detail: <see cref="CubeSpawner"/> builds cubes, <see cref="MaterialFactory"/> builds the look, and
/// the scripts under <c>Scripts/</c> handle everything that reacts to the player.
/// </remarks>
/// <param name="game">The running game.</param>
public class CubicleCalamityGame(Game game)
{
    /// <summary>Seed for cube colours, so the same board comes up every run while tuning.</summary>
    private const int Seed = 1;

    private readonly Vector3 _referenceCubePosition = new(-4, 1, -4);

    private readonly CubeGrid _grid = new();
    private readonly ScoreKeeper _keeper = new();
    private readonly LevelState _levels = new();

    // The seam persistence slots into: swap for a JsonProgressStore (see its docs) and a new launch
    // resumes at the level the last game over reached. The rest of the game only sees the interface.
    private readonly IProgressStore _progressStore = new FreshProgressStore();
    private GameProgress _progress = new();

    // One full material set per palette, all built at startup: materials are GPU resources, and a
    // palette switch should be a repaint, not an allocation
    private CubeMaterialSet[] _materialSets = [];
    private int _paletteIndex;
    private DebugTextDropdown? _paletteDropdown;

    private CubeMaterialSet? _materials;
    private CubeSpawner? _spawner;
    private GameAudio? _audio;
    private ScoreboardScript? _scoreboard;
    private CubeClickScript? _clickScript;
    private HoverHighlightScript? _hover;
    private CameraRotationScript? _cameraRotation;
    private BepuSimulation? _simulation;
    private Scene? _scene;

    private double _elapsedTime;
    private int _layer = 1;
    private bool _platformComplete;

    /// <summary>
    /// Builds the scene. Called once, before the first frame.
    /// </summary>
    /// <param name="scene">The root scene to populate.</param>
    public void Start(Scene scene)
    {
        _scene = scene;

        // With the FreshProgressStore this is always level 1; with a JSON store it is wherever the
        // last session got to
        _progress = _progressStore.Load();
        _levels.Current = LevelRules.ForNumber(_progress.Level);

        game.Window.AllowUserResizing = true;
        game.AddGraphicsCompositor().AddCleanUIStage();

        AddCamera();

        game.AddSceneRenderer(new EntityTextRenderer());
        // No shadows: the cubes are mostly emissive, so a cast shadow would darken a face that is
        // otherwise showing its true colour, which is the one thing this board cannot afford
        game.AddDirectionalLight(enableShadows: false, intensity: 3f);
        game.Add3DGround();
        game.AddProfiler();

        // Every palette's materials, built once up front - a palette switch is then a repaint,
        // never an allocation
        _materialSets = [.. ColourPalettes.All.Select(palette => MaterialFactory.CreateCubeMaterialSet(game, palette.Colours))];
        _materials = _materialSets[_paletteIndex];

        _spawner = new CubeSpawner(game, scene, _grid, _levels, Seed);
        _spawner.UsePalette(ColourPalettes.All[_paletteIndex].Colours, _materials.Normal);

        _audio = new GameAudio(game);

        AddOrientationGizmo();
        AddPaletteDropdown();

        // The toolkit's studio rig: key, fill and rim. The cubes supply most of their own colour, so
        // this is here for edge definition and to model the game-over letters.
        game.AddStudioLighting();

        _spawner.SpawnLayer(0);

        AddScoreboard();
        AddGameManager();

        var camera = scene.GetCamera();

        _cameraRotation = new CameraRotationScript { RotationCentre = _levels.Current.PlatformCentre };
        camera?.Entity.Add(_cameraRotation);

        _simulation = camera?.Entity.GetSimulation();

        ConfigureSolverForLockedStacks();
    }

    /// <summary>
    /// Grows the platform, then releases it. Called once per frame.
    /// </summary>
    /// <param name="scene">The root scene.</param>
    /// <param name="time">Timing for the current frame.</param>
    public void Update(Scene scene, GameTime time)
    {
        _paletteDropdown?.Update(game.Input);

        _elapsedTime += time.Elapsed.TotalSeconds;

        var layers = _levels.Current.Layers;

        if (_elapsedTime >= GameSettings.Interval && _layer <= layers - 1)
        {
            _elapsedTime = 0;

            _spawner?.SpawnLayer(_layer);

            _layer++;
        }

        if (!_platformComplete && _layer == layers)
        {
            _platformComplete = true;

            ReleaseCubesToPhysics(scene);
        }
    }

    /// <summary>
    /// Turns every cube from kinematic to dynamic, so the finished platform starts obeying gravity.
    /// </summary>
    /// <remarks>
    /// Cubes are spawned kinematic so each layer hangs in place while the ones above it are still
    /// being built. Letting them fall as they spawn would scatter the platform before it exists.
    /// </remarks>
    /// <param name="scene">The scene holding the cubes.</param>
    private static void ReleaseCubesToPhysics(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Name != EntityNames.Cube) continue;

            var body = entity.Get<SlidingCubeComponent>();

            if (body is null) continue;

            body.Kinematic = false;

            // Going dynamic re-applies the shape inertia, which undoes the rotation lock.
            // SimulationUpdate would catch it on the next step anyway; doing it here closes the
            // one step of freedom in between.
            body.ApplyRotationLock();
        }
    }

    /// <summary>
    /// Raises the solver's substep count, which is what stops a rotation-locked stack from jittering.
    /// </summary>
    /// <remarks>
    /// Locking rotation makes the four contact points on each cube face linearly dependent - they all
    /// control the same single linear degree of freedom - and a single-substep solve cannot converge
    /// them. The stack never settles, so it never sleeps, and the residual impulses read as boiling.
    /// <para>
    /// Measured on a headless 10x10x10 replica of this scene using Stride's defaults: at one substep
    /// all 1000 bodies stay awake with an RMS vertical velocity of 0.166; at two substeps every one
    /// sleeps at 0.00001, and a 15 second run takes 119 ms rather than 1308 ms - sleeping saves far
    /// more than the extra substep costs. Contact spring settings and MaximumRecoveryVelocity made
    /// no useful difference, and an unlocked stack settles fine at one substep, which is what
    /// identifies the rotation lock as the thing being paid for here.
    /// </para>
    /// <para>
    /// Stride's SoftStart temporarily multiplies this by <see cref="BepuSimulation.SoftStartSubstepFactor"/>
    /// and divides it back afterwards, so setting it here round-trips correctly.
    /// </para>
    /// </remarks>
    private void ConfigureSolverForLockedStacks()
    {
        if (_simulation is null) return;

        _simulation.Simulation.Solver.SubstepCount = 2;
    }

    /// <summary>
    /// Adds the camera, aimed at the middle of the platform.
    /// </summary>
    /// <remarks>
    /// Two ordering details matter here.
    /// <para>
    /// <see cref="TransformExtensions.LookAt(Stride.Engine.TransformComponent, Vector3, Vector3, float)"/>
    /// takes the eye position from <c>Transform.LocalMatrix</c> rather than from
    /// <c>Transform.Position</c>, and that matrix is still identity until the transform is updated.
    /// Without the explicit refresh the camera would be treated as sitting at the origin, looking
    /// straight up at a target directly above it - a degenerate rotation, and a blank screen.
    /// </para>
    /// <para>
    /// The aiming also has to happen before the controller is attached, because
    /// <c>Basic3DCameraController.Start</c> caches the transform it finds as the pose that H
    /// restores. Doing it in this order means H resets to a view of the platform too.
    /// </para>
    /// </remarks>
    private void AddCamera()
    {
        var camera = game.Add3DCamera();

        camera.Transform.UpdateWorldMatrix();
        camera.Transform.LookAt(_levels.Current.PlatformCentre, Vector3.UnitY);

        camera.Add3DCameraController();
    }

    /// <summary>
    /// Adds the axis gizmo that shows which way X, Y and Z run.
    /// </summary>
    /// <remarks>
    /// Building a game from code alone means there is no editor viewport to orient in - no grid, no
    /// axis widget, nothing that answers "which way is X" except what the scene draws for itself.
    /// This is that. Its placement is under review; see the plan notes on orientation aids.
    /// </remarks>
    private void AddOrientationGizmo()
    {
        var entity = new Entity(EntityNames.OrientationGizmo);

        entity.AddGizmo(game.GraphicsDevice, showAxisName: true);
        entity.Transform.Position = new Vector3(-7.5f, 1, -7.5f);
        entity.Scene = _scene;
    }

    /// <summary>
    /// Adds the entity carrying the scripts that run the game, rather than any object within it.
    /// </summary>
    private void AddGameManager()
    {
        // Centred on screen and hidden until the board runs out of moves
        var gameOver = new EntityTextComponent()
        {
            Text = string.Empty,
            FontSize = 32,
            PositionMode = TextPositionMode.Screen,
            Anchor = TextAnchor.MiddleCenter,
            Alignment = Stride.Graphics.TextAlignment.Center,
            TextColor = new Color(255, 220, 120),
            EnableShadow = true,
            EnableBackground = true,
            Padding = new Vector2(20, 14),
            LayerDepth = 2f,
            IsVisible = false,
        };

        var entity = new Entity(EntityNames.GameManager)
        {
            gameOver,
            new CubeClickScript
            {
                Grid = _grid,
                Keeper = _keeper,
                Levels = _levels,
                Sounds = _audio!,
                Scoreboard = _scoreboard,
                GameOverText = gameOver,
            },
            new ScreenCentreTextScript { Text = gameOver }
        };

        _clickScript = entity.Get<CubeClickScript>();
        _clickScript!.RestartRequested = Restart;
        _clickScript.NextLevelRequested = NextLevel;

        // The hover preview rides on the same entity: it reads the grid the click writes
        _hover = new HoverHighlightScript
        {
            Grid = _grid,
            Materials = _materials!,
            Click = _clickScript,
        };

        entity.Add(_hover);

        entity.Scene = _scene;
    }

    /// <summary>
    /// Adds the keyboard dropdown that switches the board between the palettes in
    /// <see cref="ColourPalettes"/>, live.
    /// </summary>
    /// <remarks>
    /// The dropdown renders through the same debug overlay as the game instructions, so it costs one
    /// line of screen until it is opened. Choosing an entry repaints the standing board in place -
    /// see <see cref="ApplyPalette"/>.
    /// </remarks>
    private void AddPaletteDropdown()
    {
        _paletteDropdown = new DebugTextDropdown
        {
            Title = "Colours",
            ToggleKey = Keys.P,
            SelectedIndex = _paletteIndex,
            Items = [.. ColourPalettes.All.Select((palette, index) =>
                new DebugTextDropdownItem(Keys.D1 + index, palette.Name, () => ApplyPalette(index)))],
        };

        var overlay = DebugOverlay.GetOrCreate(game);

        overlay.AddSection("Palette", () => _paletteDropdown.GetLines());
    }

    /// <summary>
    /// Switches the whole game to another palette: cubes yet to spawn, the hover variants, and every
    /// cube already standing.
    /// </summary>
    /// <param name="index">Index into <see cref="ColourPalettes.All"/>.</param>
    /// <remarks>
    /// The standing board is repainted by <em>index</em>: a cube wearing the old palette's third
    /// colour takes the new palette's third. Identity is preserved exactly, so groups, moves and the
    /// hover preview agree before and after - only the paint changes. This is why every palette must
    /// hold the same number of colours.
    /// </remarks>
    private void ApplyPalette(int index)
    {
        if (index == _paletteIndex || _spawner is null) return;

        var oldColours = ColourPalettes.All[_paletteIndex].Colours;
        var newColours = ColourPalettes.All[index].Colours;

        _paletteIndex = index;
        _materials = _materialSets[index];

        _spawner.UsePalette(newColours, _materials.Normal);

        if (_hover is not null)
        {
            _hover.Materials = _materials;
        }

        foreach (var cube in _grid.Cubes.Values)
        {
            var component = cube.Get<CubeComponent>();
            var model = cube.Get<ModelComponent>()?.Model;

            if (component is null || model is null) continue;

            var colourIndex = IndexOf(oldColours, component.Color);

            if (colourIndex < 0) continue;

            component.Color = newColours[colourIndex];

            // The model is per cube (Create3DPrimitive builds one each), so repainting its slot
            // touches no other cube. The hover override sits above this and is per component anyway.
            model.Materials[0] = new MaterialInstance { Material = _materials.Normal[component.Color] };
        }
    }

    private static int IndexOf(IReadOnlyList<Color> colours, Color colour)
    {
        for (var i = 0; i < colours.Count; i++)
        {
            if (colours[i] == colour) return i;
        }

        return -1;
    }

    /// <summary>
    /// Replays the current level from scratch, score included.
    /// </summary>
    private void Restart()
    {
        _keeper.Reset();

        RebuildBoard();
    }

    /// <summary>
    /// Advances to the next level's larger board. The score carries over - climbing is the reward,
    /// and starting every board from zero would price the climb at nothing.
    /// </summary>
    /// <remarks>
    /// Progress is saved through <see cref="IProgressStore"/> at the moment of advancing. With the
    /// default fresh store that is a no-op; with a JSON store, the next launch starts here.
    /// </remarks>
    private void NextLevel()
    {
        _progress.Level++;
        _progressStore.Save(_progress);

        _levels.Current = LevelRules.ForNumber(_progress.Level);

        // The platform grows, so its middle rises - re-aim the orbit at the new board's centre
        if (_cameraRotation is not null)
        {
            _cameraRotation.RotationCentre = _levels.Current.PlatformCentre;
        }

        RebuildBoard();
    }

    /// <summary>
    /// Tears the finished game down and lets the ordinary <see cref="Update"/> loop grow whatever
    /// <see cref="LevelState.Current"/> now says, keeping the scene's fixtures - camera, lights,
    /// ground, gizmo - in place.
    /// </summary>
    /// <remarks>
    /// Undoing a playthrough means removing everything it created: cubes and score popups are plain
    /// removals, but the fallen 3D letters own their GPU mesh buffers, so they go through
    /// <see cref="FallingLetters.ReleaseAndRemove"/> - removing them alone would leak a buffer pair
    /// per letter, every game.
    /// </remarks>
    private void RebuildBoard()
    {
        if (_scene is null) return;

        foreach (var entity in _scene.Entities.ToList())
        {
            if (entity.Name is EntityNames.Cube or EntityNames.ScorePopup)
            {
                entity.Remove();
            }
            else if (entity.Name.StartsWith("Letter", StringComparison.Ordinal))
            {
                FallingLetters.ReleaseAndRemove(entity);
            }
        }

        _grid.Clear();
        _clickScript?.ResetForRestart();

        _elapsedTime = 0;

        // Zero, not one: Start spawns layer 0 itself before handing over to Update, but here the
        // loop must build the whole board - starting at 1 left every rebuilt board one layer short
        _layer = 0;
        _platformComplete = false;
    }

    /// <summary>
    /// Adds the on-screen running total and the combo readout beneath it.
    /// </summary>
    private void AddScoreboard()
    {
        // Anchored rather than a fixed pixel position, so both keep their margin from the corner when
        // the window is resized - which this example allows
        var total = new EntityTextComponent()
        {
            Text = "Total Score: 0",
            FontSize = 20,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.TopLeft,
            Offset = new Vector2(16, 16),
            TextColor = Color.White,
            EnableShadow = true,
        };

        var combo = new EntityTextComponent()
        {
            Text = string.Empty,
            FontSize = 16,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.TopLeft,
            Offset = new Vector2(16, 44),
            TextColor = new Color(255, 210, 70),
            EnableShadow = true,
            IsVisible = false,
        };

        // The combo window as a draining bar of characters, directly under the combo line
        var comboBar = new EntityTextComponent()
        {
            Text = string.Empty,
            FontSize = 14,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.TopLeft,
            Offset = new Vector2(16, 66),
            TextColor = new Color(255, 210, 70),
            EnableShadow = true,
            IsVisible = false,
        };

        // How much board is left: cubes standing, and the moves still hidden among them
        var remaining = new EntityTextComponent()
        {
            Text = string.Empty,
            FontSize = 14,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.TopLeft,
            Offset = new Vector2(16, 88),
            TextColor = new Color(200, 205, 215),
            EnableShadow = true,
        };

        _scoreboard = new ScoreboardScript
        {
            Keeper = _keeper,
            Grid = _grid,
            Levels = _levels,
            TotalText = total,
            ComboText = combo,
            ComboBarText = comboBar,
            RemainingText = remaining,
        };

        var entity = new Entity(EntityNames.Scoreboard) { total, combo, comboBar, remaining, _scoreboard };

        entity.Scene = _scene;
    }
}