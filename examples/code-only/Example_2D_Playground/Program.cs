using Example.Common;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Shapes;
using Stride.CommunityToolkit.Windows;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A scratch 2D scene on Bepu, for trying things out. It is not an example: it has no metadata
// block, so the docs and the launcher do not list it. Keep it this small. When something tried
// here grows into a lesson, it becomes an example of its own, and this file goes back to basics.
//
// Its 3D twin is Example_Bepu_Playground.

WindowsDpiManager.EnablePerMonitorV2();

const string ShapeName = "Shape";

var theme = ColorThemes.Default;

// The same five colours as the 2D and 3D twins of the first examples, from Example.Common
(Primitive2DModelType Type, Color Colour)[] shapes =
[
    (Primitive2DModelType.Square, theme.Blue),
    (Primitive2DModelType.Rectangle, theme.Orange),
    (Primitive2DModelType.Circle, theme.Red),
    (Primitive2DModelType.Capsule, theme.Green),
    (Primitive2DModelType.Triangle, theme.Purple),
];

var created = 0;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "2D playground";

    game.SetupBase2DScene();
    game.AddProfiler();

    // G steps the grid through off, world and screen. On the command line, "--screen" starts it
    // in screen space and "--no-cursor" leaves out the coordinates under the mouse
    var grid = game.AddGrid();
    var arguments = Environment.GetCommandLineArgs();

    if (arguments.Contains("--screen")) grid.Space = GridSpace.Screen;
    if (arguments.Contains("--no-cursor")) grid.ShowCursor = false;

    AddShapes(scene, shapes.Length);

    DebugOverlay.GetOrCreate(game).AddSection("2D playground", () =>
    [
        new("Space", "Add ten shapes", Color.Gold),
        new("X", "Remove every shape", Color.Gold),
        new(""),
        new($"{scene.Entities.Count(entity => entity.Name == ShapeName)} shapes", Color.LightGreen),
    ]);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Space)) AddShapes(scene, 10);

    if (game.Input.IsKeyPressed(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(entity => entity.Name == ShapeName).ToList()) entity.Scene = null;
    }
}

/// <summary>
/// Drops <paramref name="count"/> shapes, taking each kind in turn. The positions follow a fixed
/// pattern, not a random one, so two runs start alike.
/// </summary>
void AddShapes(Scene scene, int count)
{
    for (var i = 0; i < count; i++, created++)
    {
        var (type, colour) = shapes[created % shapes.Length];

        var entity = game.Create2DPrimitive(type, new() { Material = game.CreateFlatMaterial(colour) });

        entity.Name = ShapeName;
        entity.Transform.Position = new Vector3(created * 7 % 11 - 5, 6 + created % 10 * 1.5f, 0);
        entity.Scene = scene;
    }
}