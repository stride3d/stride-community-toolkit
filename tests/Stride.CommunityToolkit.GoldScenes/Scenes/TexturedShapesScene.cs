using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Textured batches on the 2D camera: one batch filled with a picture generated in code - clamped,
/// so its bounding-box mapping and top-left origin are pinned - across a rounded panel, a disc, a
/// sector, a polygon, a thick line, a tint, a gradient and a shape drawn with the texture off; and
/// a second batch tiling the same picture as a scrolling stripe with wrap addressing.
/// </summary>
internal sealed class TexturedShapesScene : IGoldScene
{
    private ShapeBatch? _pictures;
    private ShapeBatch? _stripes;
    private ComputeTextureColor? _stripe;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase2D(new Color(24, 26, 32));

        var camera = scene.GetCamera() ?? throw new InvalidOperationException("The 2D scene has no camera.");

        camera.OrthographicSize = 14f;

        var picture = CreatePicture(game.GraphicsDevice);

        _pictures = game.AddShapeBatch();
        _pictures.FillWith(picture);

        _stripes = game.AddShapeBatch();
        _stripe = _stripes.FillWith(picture, scale: new Vector2(4f, 1f), addressMode: TextureAddressMode.Wrap);
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_pictures is not { } pictures || _stripes is not { } stripes || _stripe is not { } stripe) return;

        var seconds = (float)time.Total.TotalSeconds;

        pictures.BorderWidth = 3f;

        // Row 1: the picture as it is, in a rounded panel with a glow, a disc, a sector and a hexagon
        pictures.Fill.Set(Color.White, 1f);
        pictures.Glow.Set(8f, new Color(0, 150, 255, 160));
        pictures.DrawRectangle(new Vector3(-8f, 4f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(5f, 3.5f), new Color(110, 200, 255), cornerRadius: 0.5f);
        pictures.Glow.Clear();

        pictures.DrawSolidCircle(new Vector2(-2f, 4f), 1.7f, Color.Cyan);
        pictures.DrawSector(new Vector2(3f, 4f), 1.9f, 0.4f, 4.6f, Color.Orange);

        Span<Vector2> hexagon = stackalloc Vector2[6];

        for (var i = 0; i < 6; i++)
        {
            var angle = i * MathF.PI / 3f;
            hexagon[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 1.8f;
        }

        pictures.DrawSolidPolygon(hexagon, new Vector2(8f, 4f), 0f, Color.LimeGreen, radius: 0.2f);

        // Row 2: a tint, a gradient to nothing over the picture, a thick line, and the same panel
        // with the texture turned off - a flat fill in the same batch
        pictures.Fill.Set(new Color(255, 160, 60), 1f);
        pictures.DrawSolidCircle(new Vector2(-8f, -1.5f), 1.7f, Color.White);

        pictures.Fill.Set(Color.White, 1f);
        pictures.Gradient.Set(new Color(255, 255, 255, 0), Vector2.UnitX);
        pictures.DrawRectangle(new Vector3(-2f, -1.5f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(5f, 3.5f), Color.White, cornerRadius: 0.3f);
        pictures.Gradient.Clear();

        pictures.DrawLine(new Vector3(1.5f, -3f, 0f), new Vector3(5f, 0f, 0f), 1.2f, Color.Yellow);

        pictures.Textured = false;
        pictures.Fill.Set(new Color(4, 14, 30), 0.85f);
        pictures.DrawRectangle(new Vector3(8f, -1.5f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(3.5f, 3.5f), new Color(110, 200, 255), cornerRadius: 0.5f);
        pictures.Textured = true;

        // Row 3: the picture tiled four times across a wide bar and scrolling, in its own batch
        stripe.Offset = new Vector2(seconds * 0.25f, 0f);
        stripes.BorderWidth = 2f;
        stripes.Fill.Set(Color.White, 1f);
        stripes.DrawRectangle(new Vector3(0f, -6f, 0f), Vector3.UnitX, Vector3.UnitY, new Vector2(20f, 1.6f), Color.White, cornerRadius: 0.2f);
    }

    /// <summary>
    /// A 128 by 128 picture with an unmistakable orientation: a warm-to-cool diagonal, a grid of
    /// dark lines, a white square in the top-left corner and a black one at the bottom right.
    /// </summary>
    private static Texture CreatePicture(GraphicsDevice device)
    {
        const int size = 128;
        var pixels = new Color[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var u = x / (float)(size - 1);
                var v = y / (float)(size - 1);
                var t = 0.5f * (u + v);

                var color = Color.Lerp(new Color(255, 120, 40), new Color(40, 110, 255), t);

                if (x % 16 == 0 || y % 16 == 0)
                {
                    color = new Color(20, 20, 30);
                }

                if (x < 24 && y < 24)
                {
                    color = Color.White;
                }
                else if (x >= size - 24 && y >= size - 24)
                {
                    color = Color.Black;
                }

                pixels[y * size + x] = color;
            }
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm_SRgb, pixels);
    }
}