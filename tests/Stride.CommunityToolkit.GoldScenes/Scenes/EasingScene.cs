using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Every easing curve as a graph in a grid, with a dot on each at the simulated time, drawn through
/// ShapeBatch on the 2D camera. One golden pins all thirty-three functions at once: a change to any
/// formula moves a curve or its dot, and a change to the pixel polyline path moves them all.
/// </summary>
internal sealed class EasingScene : IGoldScene
{
    private const int Columns = 6;
    private const float TileSize = 2.1f;
    private const float PitchX = 3.8f;
    private const float PitchY = 3f;

    private ShapeBatch? _shapes;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase2D(new Color(24, 26, 32));

        var camera = scene.GetCamera() ?? throw new InvalidOperationException("The 2D scene has no camera.");

        camera.OrthographicSize = 24f;

        _shapes = game.AddShapeBatch();
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_shapes is not { } shapes) return;

        // Frame 60 on the fixed step is one second: half way along a two-second run
        var t = Math.Clamp((float)time.Total.TotalSeconds / 2f, 0f, 1f);

        Span<Vector2> points = stackalloc Vector2[49];

        foreach (var (index, function) in Enum.GetValues<EasingFunction>().Index())
        {
            var centre = new Vector2(-11f + TileSize / 2f + index % Columns * PitchX, 8f - index / Columns * PitchY);
            var left = centre.X - TileSize / 2f;
            var bottom = centre.Y - TileSize / 2f;

            shapes.BorderWidth = 1f;
            shapes.Fill.Set(new Color(32, 36, 46), 0.9f);
            shapes.DrawRectangle(new Vector3(centre, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(TileSize), new Color(70, 76, 90), cornerRadius: 0.12f);
            shapes.Fill.Set(null, 1f);

            for (var i = 0; i < points.Length; i++)
            {
                var s = i / (float)(points.Length - 1);

                points[i] = new Vector2(left + s * TileSize, bottom + function.Ease(s) * TileSize);
            }

            shapes.DrawPixelPolyline(points, 2f, new Color(90, 200, 255));
            shapes.DrawPixelDisc(new Vector3(left + t * TileSize, bottom + function.Ease(t) * TileSize, 0f), 4f, Color.White);
            shapes.DrawPixelDisc(new Vector3(left + function.Ease(t) * TileSize, bottom - 0.22f, 0f), 3.5f, new Color(255, 170, 60));
        }
    }
}