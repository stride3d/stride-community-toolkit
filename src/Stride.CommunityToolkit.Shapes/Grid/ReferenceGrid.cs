using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using System.Globalization;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// A grid with numbered lines that shows where things are: in world coordinates, the ones models
/// and physics bodies use, or in screen coordinates, the pixels a <see cref="ShapeBatch"/> draws in
/// with <see cref="ShapeBatch.Screen"/> on.
/// </summary>
/// <remarks>
/// <para>
/// The two spaces differ in three ways, and the grid shows all three. World coordinates start at
/// the world's origin, count in world units and have Y pointing up. Screen coordinates start at the
/// top left corner of the window, count in pixels and have Y pointing down.
/// </para>
/// <para>
/// In world space the grid lies in the XY plane under an orthographic camera, as in a 2D scene, and
/// on the ground plane XZ under a perspective one, where it covers a square of <see cref="Extent"/>
/// around the origin with a short Y axis standing in the middle. The axes have Game Studio's
/// colours: X red, Y green, Z blue. The step between the lines follows the zoom in jumps of 1, 2
/// and 5, unless <see cref="MajorStep"/> fixes it.
/// </para>
/// <para>
/// Add one with <c>game.AddGrid()</c>. <see cref="ToggleKey"/> steps through off, world and screen;
/// <see cref="Cycle"/>, <see cref="Visible"/> and <see cref="Space"/> do the same from code.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var grid = game.AddGrid();
///
/// grid.Space = GridSpace.Screen;
/// grid.ToggleKey = Keys.None;
/// </code>
/// </example>
[DataContract(nameof(ReferenceGrid))]
[Display("Reference grid")]
[ComponentCategory("Debug")]
public class ReferenceGrid : SyncScript
{
    /// <summary>The X axis colour, as in Game Studio.</summary>
    public static readonly Color AxisXColor = new(0xFC, 0x37, 0x37);

    /// <summary>The Y axis colour, as in Game Studio.</summary>
    public static readonly Color AxisYColor = new(0x32, 0xE3, 0x35);

    /// <summary>The Z axis colour, as in Game Studio.</summary>
    public static readonly Color AxisZColor = new(0x2F, 0x6A, 0xE1);

    private GridCanvas? _canvas;
    private DebugOverlaySection? _help;
    private DisplayScale? _displayScale;

    /// <summary>Whether the grid is drawn. Defaults to <see langword="true"/>.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>The coordinates the grid shows. Defaults to <see cref="GridSpace.World"/>.</summary>
    public GridSpace Space { get; set; } = GridSpace.World;

    /// <summary>The plane of the world grid. Defaults to <see cref="GridPlane.Auto"/>.</summary>
    public GridPlane Plane { get; set; } = GridPlane.Auto;

    /// <summary>
    /// The key that steps through off, world and screen, or <see cref="Keys.None"/> for no key.
    /// Defaults to G. While a key is set, the debug overlay names it.
    /// </summary>
    public Keys ToggleKey { get; set; } = Keys.G;

    /// <summary>Whether the major lines are numbered and the axes lettered. Defaults to <see langword="true"/>.</summary>
    public bool ShowNumbers { get; set; } = true;

    /// <summary>
    /// Whether the coordinates under the mouse are written beside it, in both spaces. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    public bool ShowCursor { get; set; } = true;

    /// <summary>
    /// The distance between major lines in world units, or 0, the default, to choose it from the
    /// zoom so that the lines stay about a hundred pixels apart.
    /// </summary>
    public float MajorStep { get; set; }

    /// <summary>
    /// How many minor steps make one major step in world space, or 0, the default, to choose four
    /// or five so that the minor lines fall on round numbers.
    /// </summary>
    public int MinorDivisions { get; set; }

    /// <summary>The side of the square the grid covers on the ground plane, in world units. Defaults to 20.</summary>
    public float Extent { get; set; } = 20f;

    /// <summary>The distance between major lines in screen space, in pixels. Defaults to 100.</summary>
    public float ScreenStep { get; set; } = 100f;

    /// <summary>How many minor steps make one major step in screen space. Defaults to 4.</summary>
    public int ScreenMinorDivisions { get; set; } = 4;

    /// <summary>The colour of the lines that are not axes. Defaults to white.</summary>
    public Color LineColor { get; set; } = Color.White;

    /// <summary>The opacity of the minor lines, 0 to 1. Defaults to 0.1.</summary>
    public float MinorOpacity { get; set; } = 0.1f;

    /// <summary>The opacity of the major lines, 0 to 1. Defaults to 0.25.</summary>
    public float MajorOpacity { get; set; } = 0.25f;

    /// <summary>The size of the numbers, in pixels on a 100% display. Defaults to 12.</summary>
    public float FontSize { get; set; } = 12f;

    /// <summary>
    /// Steps to the next state: from hidden to world, from world to screen, from screen to hidden.
    /// </summary>
    public void Cycle()
    {
        if (!Visible)
        {
            Visible = true;
            Space = GridSpace.World;
        }
        else if (Space == GridSpace.World)
        {
            Space = GridSpace.Screen;
        }
        else
        {
            Visible = false;
        }
    }

    /// <inheritdoc/>
    public override void Start()
    {
        var game = (Game)Game;

        // Batches of the grid's own. The world one is depth tested, so the scene hides the lines
        // behind it; the other draws over everything, after the post effects
        _canvas = new GridCanvas(
            this,
            ShapeBatchExtensions.Create(game, depthTest: true, fill: null, afterPostEffects: false),
            ShapeBatchExtensions.Create(game, depthTest: false, fill: null, afterPostEffects: true),
            Entity);

        game.AddEntityTextRenderer();

        _displayScale = DisplayScale.GetOrCreate(game);
        _help = DebugOverlay.GetOrCreate(game).AddSection("Grid", HelpLines);
    }

    /// <inheritdoc/>
    public override void Update()
    {
        if (ToggleKey != Keys.None && Input.IsKeyPressed(ToggleKey)) Cycle();

        if (_canvas is not null && TryGetView(_canvas.Overlay.ScreenSize, out var view)) _canvas.Draw(view, Input.MousePosition);
    }

    /// <inheritdoc/>
    public override void Cancel()
    {
        var game = (Game)Game;

        if (_canvas is not null)
        {
            game.RemoveShapeBatch(_canvas.World);
            game.RemoveShapeBatch(_canvas.Overlay);
            _canvas.RemoveLabels();
        }

        if (_help is not null) DebugOverlay.GetOrCreate(game).RemoveSection(_help);

        _canvas = null;
        _help = null;
    }

    private IReadOnlyList<TextElement> HelpLines()
    {
        if (ToggleKey == Keys.None) return [];

        var state = !Visible ? "off" : Space == GridSpace.World ? "world" : "screen";

        // The screen grid counts the pixels code draws in. On a scaled display one of those is
        // more than one pixel of the monitor, and the line says by how much
        if (Visible && Space == GridSpace.Screen && _displayScale is { Value: var scale } && MathF.Abs(scale - 1f) > 0.001f)
        {
            state += string.Create(CultureInfo.InvariantCulture, $", 1 = {scale:0.##} px");
        }

        return [new(ToggleKey.ToString(), $"Grid: {state}", Color.Gold)];
    }

    /// <summary>The main camera's view of this frame, or nothing while the game has no camera.</summary>
    private bool TryGetView(Vector2 size, out GridView view)
    {
        view = default;

        var cameras = SceneSystem.GraphicsCompositor?.Cameras;

        if (cameras is null || cameras.Count == 0 || cameras[0].Camera is not { } camera || size.X <= 0f || size.Y <= 0f) return false;

        view = new GridView(camera.ViewProjectionMatrix, size, camera.Projection == CameraProjectionMode.Orthographic);

        return true;
    }
}