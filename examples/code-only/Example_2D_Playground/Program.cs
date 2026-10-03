using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// A scratch 2D scene on Bepu, for trying things out. It is not an example: it has no metadata
// block, so the docs and the launcher do not list it. Keep it this small. When something tried
// here grows into a lesson, it becomes an example of its own, and this file goes back to basics.
//
// Its 3D twin is Example_Bepu_Playground.

const string ShapeName = "Shape";

(Primitive2DModelType Type, Color Colour)[] shapes =
[
    (Primitive2DModelType.Square, new Color(70, 160, 235)),
    (Primitive2DModelType.Rectangle, new Color(240, 150, 60)),
    (Primitive2DModelType.Circle, new Color(235, 100, 80)),
    (Primitive2DModelType.Capsule, new Color(110, 200, 110)),
    (Primitive2DModelType.Triangle, new Color(190, 130, 230)),
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