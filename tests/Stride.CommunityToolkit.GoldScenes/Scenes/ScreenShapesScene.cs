using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Screen shapes over a 3D scene, from the same depth-tested batch as the world shapes: a crosshair
/// at the centre, a widget in each corner placed by Corner(), a dashed and a glowing outline, a
/// pixel-width line, a sector and a rounded panel drawn into a viewport rectangle, and a world disc
/// on the ground under a pillar that the screen shapes must sit over regardless of depth. Pins the
/// pixel mapping, the Y-down convention, the viewport offset and the near-plane depth.
/// </summary>
internal sealed class ScreenShapesScene : IGoldScene
{
    private ShapeBatch? _shapes;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 5f, 12f));
        game.SetCameraRotation(new Vector3(0f, -18f, 0f));

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f),
            Size = new Vector3(30f, 0.5f, 30f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        // A pillar in the middle of the view: the crosshair and the panel sit over it, not behind it
        var pillar = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Pillar",
            Material = game.CreateMaterial(new Color(96, 103, 116), metalness: 0.1f, glossiness: 0.35f),
            Size = new Vector3(1.6f, 4f, 1.6f),
            Position = new Vector3(0f, 2f, 0f),
        });

        pillar.Scene = scene;

        _shapes = game.AddShapeBatch(depthTest: true);
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_shapes is not { } shapes) return;

        var seconds = (float)time.Total.TotalSeconds;

        // In the world first: a disc on the ground, cut by the pillar, as any depth-tested shape is
        shapes.BorderWidth = 3f;
        shapes.Fill.Alpha = 0.45f;
        shapes.DrawDisc(new Vector3(0f, 0.02f, 1.5f), Vector3.UnitY, 3f, Color.OrangeRed);

        // Then on the screen, from the same batch
        shapes.Screen = true;

        var centre = shapes.ScreenSize * 0.5f;

        // A crosshair: a ring and two pixel lines with a gap at the middle
        shapes.BorderWidth = 2f;
        shapes.DrawRing(new Vector3(centre, 0f), Vector3.UnitZ, 22f, Color.White);
        shapes.DrawPixelLine(new Vector3(centre.X - 40f, centre.Y, 0f), new Vector3(centre.X - 10f, centre.Y, 0f), 2f, Color.White);
        shapes.DrawPixelLine(new Vector3(centre.X + 10f, centre.Y, 0f), new Vector3(centre.X + 40f, centre.Y, 0f), 2f, Color.White);
        shapes.DrawPixelLine(new Vector3(centre.X, centre.Y - 40f, 0f), new Vector3(centre.X, centre.Y - 10f, 0f), 2f, Color.White);
        shapes.DrawPixelLine(new Vector3(centre.X, centre.Y + 10f, 0f), new Vector3(centre.X, centre.Y + 40f, 0f), 2f, Color.White);

        // Corner widgets, each placed as a corner plus an offset
        shapes.Fill.Set(new Color(4, 14, 30), 0.7f);
        shapes.Glow.Set(8f, new Color(0, 150, 255, 160));
        shapes.DrawRectangle(new Vector3(shapes.Corner(ScreenCorner.TopLeft) + new Vector2(110f, 60f), 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(180f, 80f), new Color(110, 200, 255), cornerRadius: 12f);
        shapes.Glow.Clear();

        shapes.Fill.Set(null, 0.45f);
        shapes.Dash.Set(8f, 6f, seconds * 30f);
        shapes.DrawRing(new Vector3(shapes.Corner(ScreenCorner.TopRight) + new Vector2(-90f, 80f), 0f), Vector3.UnitZ, 50f, Color.Orange);
        shapes.Dash.Clear();

        // A radial gauge: a faint full track and a bright arc, clockwise from twelve because Y is down
        var bottomLeft = shapes.Corner(ScreenCorner.BottomLeft) + new Vector2(100f, -90f);

        shapes.Fill.Alpha = 0.25f;
        shapes.DrawArc(bottomLeft, 50f, 0f, MathF.Tau, Color.Gray, width: 14f);
        shapes.Fill.Alpha = 0.9f;
        shapes.DrawArc(bottomLeft, 50f, -MathF.PI * 0.5f, MathF.Tau * 0.65f, Color.LimeGreen, width: 14f);
        shapes.Fill.Alpha = 0.45f;

        // A sector in the bottom-right corner, its sweep turning clockwise on screen
        shapes.Fill.Color = Color.DodgerBlue;
        shapes.DrawSector(shapes.Corner(ScreenCorner.BottomRight) + new Vector2(-100f, -90f), 60f, 0.3f, 4.2f, Color.White, innerRadius: 28f);
        shapes.Fill.Color = null;

        // A viewport rectangle: the panel and its bar are drawn in the rectangle's own coordinates
        shapes.Viewport = new RectangleF(360f, 470f, 560f, 150f);
        shapes.Fill.Set(new Color(4, 14, 30), 0.7f);
        shapes.BorderWidth = 1.5f;
        shapes.DrawRectangle(new Vector3(shapes.Corner(ScreenCorner.BottomRight) * 0.5f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(560f, 150f), new Color(110, 200, 255), cornerRadius: 10f);
        shapes.Fill.Set(new Color(255, 120, 40, 200), 1f);
        shapes.Gradient.Set(new Color(255, 230, 120), Vector2.UnitX);
        shapes.DrawRectangle(new Vector3(200f, 75f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(340f, 24f), Color.Orange);
        shapes.Gradient.Clear();
        shapes.Fill.Set(null, 0.45f);
        shapes.Viewport = null;

        shapes.BorderWidth = 3f;
        shapes.Screen = false;
    }
}