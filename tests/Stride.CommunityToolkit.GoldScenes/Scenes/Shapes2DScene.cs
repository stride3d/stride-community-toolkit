using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// ShapeBatch on the 2D camera: every planar shape family in three rows, with the per-draw states
/// that have broken before - fill and outline colours, glow, dashes, gradient, opacity - and a
/// pixel polyline, so a change to the shader's 2D path shows up here first.
/// </summary>
internal sealed class Shapes2DScene : IGoldScene
{
    private ShapeBatch _shapes = null!;

    public SceneSetup Setup => GoldSetup.Orthographic(14f);

    public void Start(Game game, Scene scene)
    {
        _shapes = game.AddShapeBatch();
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        var seconds = (float)time.Total.TotalSeconds;

        _shapes.BorderWidth = 3f;
        _shapes.StrokeWidth = 3f;
        _shapes.Fill.Alpha = 0.45f;

        // Row 1: a HUD panel, a ring, an annulus and a donut segment
        _shapes.Fill.Set(new Color(4, 14, 30), 0.85f);
        _shapes.Glow.Set(8f, new Color(0, 150, 255, 160));
        _shapes.DrawRectangle(new Vector3(-8f, 4f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(5f, 3f), new Color(110, 200, 255), cornerRadius: 0.4f);
        _shapes.Glow.Clear();
        _shapes.Fill.Set(null, 0.45f);

        _shapes.DrawRing(new Vector3(-2f, 4f, 0f), Vector3.UnitZ, 1.4f, Color.Cyan);
        _shapes.DrawAnnulus(new Vector3(3f, 4f, 0f), Vector3.UnitZ, 1.5f, 0.9f, Color.Turquoise);

        _shapes.Fill.Color = Color.DodgerBlue;
        _shapes.DrawSector(new Vector3(8f, 4f, 0f), Vector3.UnitZ, 1.6f, 0.5f, 4.0f, Color.White, innerRadius: 0.8f);
        _shapes.Fill.Color = null;

        // Row 2: a dashed ring turning, a round-capped arc, a gradient to nothing, three opacities
        _shapes.Dash.Set(8f, 6f, seconds * 20f);
        _shapes.DrawRing(new Vector3(-8f, 0f, 0f), Vector3.UnitZ, 1.5f, Color.Orange);
        _shapes.Dash.Clear();

        _shapes.Fill.Alpha = 0.9f;
        _shapes.DrawArc(new Vector3(-2f, 0f, 0f), Vector3.UnitZ, 1.5f, MathF.PI * 0.5f, -MathF.PI * 1.2f, Color.LimeGreen, width: 0.4f);
        _shapes.Fill.Alpha = 0.45f;

        _shapes.BorderWidth = 1.5f;
        _shapes.Fill.Set(new Color(120, 200, 255, 140), 1f);
        _shapes.Gradient.Set(new Color(120, 200, 255, 0), Vector2.UnitX);
        _shapes.DrawRectangle(new Vector3(3f, 0f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(5f, 1.4f), new Color(120, 200, 255), cornerRadius: 0.3f);
        _shapes.Gradient.Clear();
        _shapes.Fill.Set(null, 0.45f);
        _shapes.BorderWidth = 3f;

        _shapes.Glow.Set(10f);

        for (var i = 0; i < 3; i++)
        {
            _shapes.Opacity = 1f - i * 0.35f;
            _shapes.DrawDisc(new Vector3(6.5f + i * 1.5f, 0f, 0f), Vector3.UnitZ, 0.6f, Color.Gold);
        }

        _shapes.Opacity = 1f;
        _shapes.Glow.Clear();

        // Row 3: a pixel polyline, a closed concave bracket with marching dashes, a pixel line and a thick line
        Span<Vector2> wave = stackalloc Vector2[32];

        for (var i = 0; i < wave.Length; i++)
        {
            var x = -11f + 8f * i / (wave.Length - 1);

            wave[i] = new Vector2(x, -4f + MathF.Sin(x * 1.3f) * 1f);
        }

        _shapes.DrawPixelPolyline(wave, 2f, Color.White);

        ReadOnlySpan<Vector2> bracket = [new(-2f, -5f), new(-2f, -3.5f), new(-1.25f, -2.75f), new(1.25f, -2.75f), new(2f, -3.5f), new(2f, -5f), new(1f, -5f), new(1f, -4.25f), new(-1f, -4.25f), new(-1f, -5f)];

        _shapes.Dash.Set(8f, 5f, seconds * 25f);
        _shapes.DrawPixelPolyline(bracket, 2f, Color.Cyan, closed: true);
        _shapes.Dash.Clear();

        _shapes.DrawPixelLine(new Vector3(5f, -3f, 0f), new Vector3(11f, -3f, 0f), 2f, Color.White);
        _shapes.DrawLine(new Vector3(5f, -4.5f, 0f), new Vector3(11f, -5f, 0f), 0.35f, Color.HotPink);
    }
}