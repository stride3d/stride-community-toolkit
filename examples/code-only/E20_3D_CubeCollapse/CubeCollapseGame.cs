using CubeCollapse.Components;
using CubeCollapse.Gameplay;
using CubeCollapse.Scripts;
using CubeCollapse.Setup;
using CubeCollapse.Shared;
using Stride.BepuPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;

namespace CubeCollapse;

/// <summary>
/// Runs Cube Collapse: builds the scene once, then grows the platform a layer at a time until it
/// is complete and hands it over to the physics.
/// </summary>
/// <remarks>
/// This is the piece a reader should start from. It owns the order things happen in and delegates the
/// detail: <see cref="CubeSpawner"/> builds cubes, <see cref="MaterialFactory"/> builds the look, and
/// the scripts under <c>Scripts/</c> handle everything that reacts to the player.
/// </remarks>
/// <param name="game">The running game.</param>
/// <param name="progressStore">
/// Where progress is read and written. Defaults to <see cref="FreshProgressStore"/>, which starts
/// every launch at level one; pass a <see cref="JsonProgressStore"/> to resume instead.
/// </param>
public class CubeCollapseGame(Game game, IProgressStore? progressStore = null)
{
    /// <summary>Seed for cube colours, so the same board comes up every run while tuning.</summary>
    private const int Seed = 1;

    /// <summary>
    /// How far the ground's visible surface sits below the surface the cubes rest on. A cube's bottom
    /// face and the floor in the same plane fight for the depth buffer, which the two-sided glass
    /// shows as a flickering bottom; a centimetre apart they do not. The material gallery stands its
    /// glass a centimetre up for the same reason.
    /// </summary>
    private const float GroundGap = 0.01f;

    /// <summary>
    /// The glass key light's distance across and up from the board: the r of the gallery's key-light
    /// recipe. Large against the board, so every cube gets about the same light.
    /// </summary>
    private const float KeyDistance = 20f;

    private readonly CubeGrid _grid = new();
    private readonly ScoreKeeper _keeper = new();
    private readonly LevelState _levels = new();

    // Pass a JsonProgressStore at construction and a new launch resumes at the last level reached;
    // the rest of the game only sees the interface
    private readonly IProgressStore _progressStore = progressStore ?? new FreshProgressStore();
    private GameProgress _progress = new();

    // One full material set per palette, all built at startup: materials are GPU resources, and a
    // palette switch should be a repaint, not an allocation
    private CubeMaterialSet[] _materialSets = [];
    private int _paletteIndex;
    private int _colourCount = GameSettings.ColourCount;

    // Built in Start, once there is a scene to put them in
    private KeyMenu _paletteMenu = null!;
    private CubeMaterialSet _materials = null!;
    private CubeSpawner _spawner = null!;
    private GameAudio _audio = null!;
    private ScoreboardScript _scoreboard = null!;
    private CubeClickScript _clickScript = null!;
    private HoverHighlightScript _hover = null!;
    private CameraRotationScript _cameraRotation = null!;
    private Scene _scene = null!;

    // Two lighting rigs, one in the scene at a time. Colour palettes: the Bepu playground's default
    // directional light plus a weak fill so no cube face goes dark. Glass palettes: the material
    // gallery's steeper sun and key light, so the glass looks as it does there
    private Entity _light = null!;
    private Entity _fillLight = null!;
    private Entity _sun = null!;
    private Entity _keyLight = null!;

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

        var cameraEntity = AddCamera();

        game.AddSceneRenderer(new EntityTextRenderer());
        _light = game.AddDirectionalLight();
        _fillLight = AddFillLight(_light);
        LowerGroundSurface(game.Add3DGround());

        // The gallery's sun: the default directional light, steepened the way its LightTheRing does,
        // with the thickness term its subsurface station reads
        _sun = game.AddDirectionalLight();
        _sun.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-65f)) * Quaternion.RotationY(MathUtil.DegreesToRadians(-180f));
        ((LightDirectional)_sun.Get<LightComponent>().Type).Shadow.ComputeTransmittance = true;

        // The gallery's key light: r up from the ring's centre, reaching each exhibit at forty-five
        // degrees from r times root two away, intensity 30 r squared, range 2.5 r. Here r = KeyDistance,
        // on the camera's side of the board
        _keyLight = new Entity("Key light")
        {
            new LightComponent
            {
                Intensity = 30f * KeyDistance * KeyDistance,
                Type = new LightPoint { Radius = KeyDistance * 2.5f, Color = new ColorRgbProvider(Color.White) },
            },
        };

        // "--palette 4" starts on the fourth palette, for screenshots of the other looks
        var arguments = Environment.GetCommandLineArgs();
        var paletteAt = Array.IndexOf(arguments, "--palette");

        if (paletteAt >= 0 && paletteAt + 1 < arguments.Length && int.TryParse(arguments[paletteAt + 1], out var palette) && palette >= 1 && palette <= ColourPalettes.All.Count)
        {
            _paletteIndex = palette - 1;
        }

        // "--colours 3" plays with the first three colours of every palette: larger groups, an easier board
        var coloursAt = Array.IndexOf(arguments, "--colours");

        if (coloursAt >= 0 && coloursAt + 1 < arguments.Length && int.TryParse(arguments[coloursAt + 1], out var colours) && colours >= 2 && colours <= 4)
        {
            _colourCount = colours;
        }

        _materialSets = [.. ColourPalettes.All.Select(palette => MaterialFactory.CreateCubeMaterialSet(game, palette))];
        _materials = _materialSets[_paletteIndex];

        _spawner = new CubeSpawner(game, scene, _grid, _levels, Seed);
        _spawner.UsePalette(Colours(_paletteIndex), _materials.Normal);

        _audio = new GameAudio(game);

        AddOrientationGizmo();
        AddPaletteMenu();

        UseLighting(ColourPalettes.All[_paletteIndex].Glass);

        _spawner.SpawnLayer(0);

        AddScoreboard();
        AddGameManager();

        _cameraRotation = new CameraRotationScript { RotationCentre = _levels.Current.PlatformCentre };
        cameraEntity.Add(_cameraRotation);

        ConfigureSolverForLockedStacks(cameraEntity.GetSimulation());
    }

    /// <summary>
    /// Grows the platform, then releases it. Called once per frame.
    /// </summary>
    /// <param name="scene">The root scene.</param>
    /// <param name="time">Timing for the current frame.</param>
    public void Update(Scene scene, GameTime time)
    {
        _paletteMenu.Update(game.Input);

        _elapsedTime += time.Elapsed.TotalSeconds;

        var layers = _levels.Current.Layers;

        if (_elapsedTime >= GameSettings.Interval && _layer <= layers - 1)
        {
            _elapsedTime = 0;

            _spawner.SpawnLayer(_layer);

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

            // Going dynamic re-applies the shape inertia and undoes the rotation lock. SimulationUpdate
            // would catch it next step; locking here closes that one step of freedom.
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
    /// <param name="simulation">The simulation stepping the cubes.</param>
    private static void ConfigureSolverForLockedStacks(BepuSimulation simulation) => simulation.Simulation.Solver.SubstepCount = 2;

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
    /// The aim must be set before <c>Basic3DCameraController.Start</c> runs, because it caches the
    /// transform it finds as the pose that H restores. Aiming here means H resets to a view of the
    /// platform too.
    /// </para>
    /// </remarks>
    /// <returns>The camera entity.</returns>
    private Entity AddCamera()
    {
        var camera = game.Add3DCamera();

        camera.Transform.UpdateWorldMatrix();
        camera.Transform.LookAt(_levels.Current.PlatformCentre, Vector3.UnitY);

        camera.Add3DCameraController();

        return camera;
    }

    /// <summary>
    /// Adds the axis gizmo that shows which way X, Y and Z run.
    /// </summary>
    /// <remarks>
    /// A code-only game has no editor viewport to orient in, so the scene draws its own axes.
    /// </remarks>
    private void AddOrientationGizmo()
    {
        var entity = new Entity(EntityNames.OrientationGizmo, new Vector3(-7.5f, 1, -7.5f));

        entity.AddGizmo(game.GraphicsDevice, showAxisName: true);
        entity.Scene = _scene;
    }

    /// <summary>
    /// Adds the entity carrying the scripts that run the game, rather than any object within it.
    /// </summary>
    private void AddGameManager()
    {
        // Centred on screen and hidden until the board runs out of moves
        var gameOver = new EntityTextComponent
        {
            Text = string.Empty,
            FontSize = 32,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = Anchor.MiddleCenter,
            Anchor = Anchor.MiddleCenter,
            Alignment = TextAlignment.Center,
            TextColor = new Color(255, 220, 120),
            EnableShadow = true,
            EnableBackground = true,
            Padding = new Vector2(20, 14),
            LayerDepth = 2f,
            IsVisible = false,
        };

        _clickScript = new CubeClickScript
        {
            Grid = _grid,
            Keeper = _keeper,
            Levels = _levels,
            Sounds = _audio,
            Scoreboard = _scoreboard,
            GameOverText = gameOver,
            // A score in the colour of the cubes it cleared. Glass tints are too pale to read as
            // text, so a glass palette shows the deeper colour its hover uses
            PopupColour = colour => ColourPalettes.All[_paletteIndex].Glass ? MaterialFactory.Deepen(colour) : colour,
            RestartRequested = Restart,
            NextLevelRequested = NextLevel,
        };

        // The hover preview rides on the same entity: it reads the grid the click writes
        _hover = new HoverHighlightScript { Grid = _grid, Materials = _materials, Click = _clickScript };

        _scene.Entities.Add(new Entity(EntityNames.GameManager) { gameOver, _clickScript, _hover });
    }

    /// <summary>
    /// Adds the keyboard key menu that switches the board between the palettes in
    /// <see cref="ColourPalettes"/>, live.
    /// </summary>
    /// <remarks>
    /// The key menu renders through the same text overlay as the game instructions, so it costs one
    /// line of screen until it is opened. Choosing an entry repaints the standing board in place -
    /// see <see cref="ApplyPalette"/>.
    /// </remarks>
    private void AddPaletteMenu()
    {
        _paletteMenu = new KeyMenu
        {
            Title = "Colours",
            ToggleKey = Keys.P,
            SelectedIndex = _paletteIndex,
            Items = [.. ColourPalettes.All.Select((palette, index) =>
                new KeyMenuItem(Keys.D1 + index, palette.Name, () => ApplyPalette(index)))],
        };

        TextOverlay.GetOrCreate(game).AddSection("Palette", _paletteMenu.GetLines);
    }

    /// <summary>
    /// Swaps the lighting rigs: the material gallery's for the glass palette, the board's own for the
    /// others. The key light stands above the board on the camera's side and comes in at forty-five
    /// degrees, the way the gallery's reaches its exhibits.
    /// </summary>
    private void UseLighting(bool glass)
    {
        _light.Scene = glass ? null : _scene;
        _fillLight.Scene = glass ? null : _scene;
        _sun.Scene = glass ? _scene : null;
        _keyLight.Scene = glass ? _scene : null;

        var cameraSide = Vector3.Normalize(new Vector3(1f, 0f, 1f)) * KeyDistance;

        _keyLight.Transform.Position = _levels.Current.PlatformCentre + cameraSide + new Vector3(0f, KeyDistance, 0f);
    }

    /// <summary>
    /// A weak directional light from the camera's starting side, without shadows. The main light
    /// shines toward +Z, so from the starting view it lights only the tops; the two side faces the
    /// camera sees, +X and +Z, would stay dark. The fill comes from the opposite corner at the same
    /// height and gives both of them a quarter of the main light's strength.
    /// </summary>
    private Entity AddFillLight(Entity light)
    {
        var fill = game.AddDirectionalLight("Fill light", enableShadows: false, intensity: light.Get<LightComponent>().Intensity * 0.25f);
        var direction = Vector3.Transform(-Vector3.UnitZ, light.Transform.Rotation);
        var across = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z) / MathF.Sqrt(2f);

        fill.Transform.Rotation = Quaternion.BetweenDirections(-Vector3.UnitZ, new Vector3(-across, direction.Y, -across));

        return fill;
    }

    /// <summary>
    /// Moves the ground's model, not its collider, down by <see cref="GroundGap"/>: the cubes are
    /// physics bodies and settle on the collider wherever it is, so only the picture can move.
    /// </summary>
    private static void LowerGroundSurface(Entity ground)
    {
        if (ground.Get<ModelComponent>() is not { } model) return;

        ground.Remove(model);
        ground.AddChild(new Entity("Ground surface", new Vector3(0f, -GroundGap, 0f)) { model });
    }

    /// <summary>The colours the board plays with from a palette: the first <see cref="_colourCount"/> of them.</summary>
    private IReadOnlyList<Color> Colours(int paletteIndex) => [.. ColourPalettes.All[paletteIndex].Colours.Take(_colourCount)];

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
        if (index == _paletteIndex) return;

        var oldColours = Colours(_paletteIndex);
        var newColours = Colours(index);

        _paletteIndex = index;
        _materials = _materialSets[index];
        UseLighting(ColourPalettes.All[index].Glass);

        _spawner.UsePalette(newColours, _materials.Normal);
        _hover.Materials = _materials;

        foreach (var cube in _grid.Cubes.Values)
        {
            var component = cube.Get<CubeComponent>();
            var model = cube.Get<ModelComponent>()?.Model;

            if (component is null || model is null) continue;

            var colourIndex = IndexOf(oldColours, component.Color);

            if (colourIndex < 0) continue;

            component.Color = newColours[colourIndex];

            // The model is per cube (CreatePrimitive builds one each), so repainting its slot
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
    /// Advances to the next level's larger board. The score carries over, so the climb is rewarded.
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
        _cameraRotation.RotationCentre = _levels.Current.PlatformCentre;

        // The key light follows the bigger board
        UseLighting(ColourPalettes.All[_paletteIndex].Glass);

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
        foreach (var entity in _scene.Entities.ToList())
        {
            if (entity.Name is EntityNames.Cube or EntityNames.ScorePopup)
            {
                entity.Remove();
            }
            else if (entity.Name.StartsWith(EntityNames.LetterPrefix, StringComparison.Ordinal))
            {
                FallingLetters.ReleaseAndRemove(entity);
            }
        }

        _grid.Clear();
        _clickScript.ResetForRestart();

        _elapsedTime = 0;

        // Zero, not one: Start spawns layer 0 itself, but a rebuild leaves the whole board to Update
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
        var total = new EntityTextComponent
        {
            Text = "Total Score: 0",
            FontSize = 20,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = Anchor.TopLeft,
            Offset = new Vector2(16, 16),
            TextColor = Color.White,
            EnableShadow = true,
        };

        var combo = new EntityTextComponent
        {
            Text = string.Empty,
            FontSize = 16,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = Anchor.TopLeft,
            Offset = new Vector2(16, 44),
            TextColor = new Color(255, 210, 70),
            EnableShadow = true,
            IsVisible = false,
        };

        // The combo window as a draining bar of characters, directly under the combo line
        var comboBar = new EntityTextComponent
        {
            Text = string.Empty,
            FontSize = 14,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = Anchor.TopLeft,
            Offset = new Vector2(16, 66),
            TextColor = new Color(255, 210, 70),
            EnableShadow = true,
            IsVisible = false,
        };

        // How much board is left: cubes standing, and the moves still hidden among them
        var remaining = new EntityTextComponent
        {
            Text = string.Empty,
            FontSize = 14,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = Anchor.TopLeft,
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

        _scene.Entities.Add(new Entity(EntityNames.Scoreboard) { total, combo, comboBar, remaining, _scoreboard });
    }
}