using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Draws the world axes in a corner of the screen, turning with the camera, the way Game Studio's
/// space marker shows which way is which in the scene editor.
/// </summary>
/// <remarks>
/// <para>
/// Each axis is an arrow from the centre, a shaft and a cone head in the editor's proportions, with
/// its letter in white just past the tip; the negative halves are small unlettered discs. Ends
/// pointing away from the camera are drawn first and dimmer, so the three arrows read as a solid
/// tripod however the camera turns. The gizmo is screen-space, drawn after the scene and over it, and
/// ignores depth. Its size follows the display scale while <see cref="AutoScale"/> is on.
/// </para>
/// <para>
/// Add it with <c>game.AddOrientationGizmo()</c>, or put an instance in the compositor with
/// <c>AddSceneRenderer</c>. The axis colours are those of the ground gizmo and the editor: red X,
/// green Y, blue Z.
/// </para>
/// </remarks>
public sealed class OrientationGizmoRenderer : SceneRendererBase
{
    /// <summary>The profiling key the renderer draws under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("OrientationGizmo");

    private static readonly Color4 ProfileColor = new(0.9f, 0.6f, 0.3f, 1f);

    private const int TextureSize = 16;

    // The editor's space marker, per unit of arrow length: a cone 0.15 * 0.75 in radius and twice
    // that long, a shaft of 4/7.5 of the cone radius, and the letter at 1.15 along the axis
    private const float HeadLength = 0.225f;
    private const float HeadRadius = 0.1125f;
    private const float ShaftRadius = 0.06f;
    private const float LabelDistance = 1.15f;

    // Below this much of its length on the screen an arrow is seen end-on: a disc, like the cone's base
    private const float EndOnLength = 0.2f;

    private readonly OrientationGizmoEnd[] _ends = new OrientationGizmoEnd[OrientationGizmoMath.EndCount];

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture? _pixel;
    private Texture? _disc;
    private Texture? _head;
    private DisplayScale? _displayScale;

    /// <summary>The corner or edge of the screen the gizmo sits in. The default is the bottom left.</summary>
    public Anchor Anchor { get; set; } = Anchor.BottomLeft;

    /// <summary>The length of an arrow from the centre to its tip, in pixels before the display scale. The other sizes follow it.</summary>
    public float Size { get; set; } = 40f;

    /// <summary>The distance from the screen edge to the gizmo's reach, in pixels before the display scale.</summary>
    public Int2 Margin { get; set; } = new(12, 12);

    /// <summary>The size of the axis letters, in pixels before the display scale.</summary>
    public float FontSize { get; set; } = 12f;

    /// <summary>The whole gizmo's opacity, 0 to 1.</summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>Whether the sizes follow the display scale, so the gizmo is the same physical size on every monitor.</summary>
    public bool AutoScale { get; set; } = true;

    /// <summary>Whether the arrows carry their axis letter past the tip.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Whether the negative halves are drawn, as small unlettered discs.</summary>
    public bool ShowNegativeEnds { get; set; } = true;

    /// <summary>The colour of the X axis.</summary>
    public Color XColor { get; set; } = new(0xFC, 0x37, 0x37);

    /// <summary>The colour of the Y axis.</summary>
    public Color YColor { get; set; } = new(0x32, 0xE3, 0x35);

    /// <summary>The colour of the Z axis.</summary>
    public Color ZColor { get; set; } = new(0x2F, 0x6A, 0xE1);

    /// <summary>The colour of the axis letters. A faint shadow under them keeps them readable on a light sky.</summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <inheritdoc/>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = RendererDefaults.LoadDefaultFont(Content, Services, 32f);
        _pixel = ScreenTextDrawer.CreateBackgroundTexture(GraphicsDevice);
        _disc = CreateShapeTexture(GraphicsDevice, DiscCoverage);
        _head = CreateShapeTexture(GraphicsDevice, HeadCoverage);

        if (Services.GetService<IGame>() is { } game)
        {
            _displayScale = DisplayScale.GetOrCreate(game);
        }
    }

    /// <inheritdoc/>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        using var _ = drawContext.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        if (_spriteBatch is null || _pixel is null || _disc is null || _head is null || Opacity <= 0f) return;

        var camera = CompositorCameras.Find(context);

        if (camera is null) return;

        var viewport = drawContext.CommandList.Viewport;
        var screen = new Vector2(viewport.Width, viewport.Height);

        if (screen.X <= 0f || screen.Y <= 0f) return;

        var scale = AutoScale && _displayScale is not null ? _displayScale.Value : 1f;
        var centre = Centre(screen, scale);
        var view = camera.ViewMatrix;

        OrientationGizmoMath.Project(in view, _ends);

        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        foreach (var end in _ends)
        {
            if (end.Positive)
            {
                DrawArrow(end, centre, scale);
            }
            else if (ShowNegativeEnds)
            {
                DrawNegativeEnd(end, centre, scale);
            }
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Where the gizmo's centre goes: anywhere in the screen less the gizmo's reach and a margin on
    /// each side, the anchor picking the point of that room, so an edge or the centre needs no case.
    /// </summary>
    private Vector2 Centre(Vector2 screen, float scale)
    {
        // The reach is the letter past the tip, plus half of it for the letter's own size
        var extent = (Size * LabelDistance + FontSize * 0.6f) * scale;
        var margin = new Vector2(Margin.X, Margin.Y) * scale;
        var room = Vector2.Max(Vector2.Zero, screen - 2f * (margin + new Vector2(extent)));
        var centre = margin + new Vector2(extent) + Anchor.At(room);

        return new Vector2(MathF.Round(centre.X), MathF.Round(centre.Y));
    }

    private void DrawArrow(in OrientationGizmoEnd end, Vector2 centre, float scale)
    {
        var length = Size * scale;
        var nearness = Nearness(end);
        var color = Shade(AxisColor(end.Axis), 0.55f + 0.45f * nearness, Opacity);
        var tip = centre + end.Direction * length;

        // How much of the arrow lies in the screen: 1 side on, 0 pointing at or away from the camera
        var onScreen = end.Direction.Length();
        var perspective = 0.85f + 0.15f * nearness;
        var headRadius = HeadRadius * length * perspective;

        if (onScreen < EndOnLength)
        {
            // Seen end-on, a cone is its base
            DrawDisc(tip, headRadius, color);
        }
        else
        {
            var direction = end.Direction / onScreen;
            var headLength = HeadLength * length * MathF.Max(onScreen, 0.35f);
            var shaftLength = onScreen * length - headLength;

            DrawLine(centre, direction, shaftLength, ShaftRadius * 2f * length * perspective, color);
            DrawHead(tip, direction, headLength, headRadius * 2f, color);
        }

        if (ShowLabels && _font is not null)
        {
            DrawLabel(end.Label, centre + end.Direction * length * LabelDistance, scale);
        }
    }

    private void DrawNegativeEnd(in OrientationGizmoEnd end, Vector2 centre, float scale)
    {
        var length = Size * scale;
        var nearness = Nearness(end);
        var color = Shade(AxisColor(end.Axis), 0.55f + 0.45f * nearness, Opacity * 0.6f);

        DrawDisc(centre + end.Direction * length, HeadRadius * length * (0.85f + 0.15f * nearness) * 0.9f, color);
    }

    private void DrawLine(Vector2 from, Vector2 direction, float length, float width, Color4 color)
    {
        if (length <= 0.5f) return;

        var rotation = MathF.Atan2(direction.Y, direction.X);

        // A 1x1 texture stretched to the line: the origin is a fraction of the texture, so (0, 0.5)
        // pivots the stretched quad about the middle of its start
        _spriteBatch!.Draw(_pixel, from, null, color, rotation, new Vector2(0f, 0.5f), new Vector2(length, width));
    }

    private void DrawHead(Vector2 tip, Vector2 direction, float length, float width, Color4 color)
    {
        var rotation = MathF.Atan2(direction.Y, direction.X);

        // The head texture points along +X with its apex at the right edge; the origin is the apex
        _spriteBatch!.Draw(_head, tip, null, color, rotation, new Vector2(TextureSize, TextureSize * 0.5f), new Vector2(length, width) / TextureSize);
    }

    private void DrawDisc(Vector2 centre, float radius, Color4 color)
        => _spriteBatch!.Draw(_disc, centre, null, color, 0f, new Vector2(TextureSize * 0.5f), new Vector2(radius * 2f / TextureSize));

    private void DrawLabel(string label, Vector2 at, float scale)
    {
        var fontSize = FontSize * scale;
        var textSize = _font!.MeasureString(label, fontSize);
        var position = new Vector2(MathF.Round(at.X - textSize.X * 0.5f), MathF.Round(at.Y - textSize.Y * 0.5f));
        var shadowOffset = MathF.Max(1f, MathF.Round(scale));

        _spriteBatch!.DrawString(_font, label, fontSize, position + new Vector2(shadowOffset), Shade(Color.Black, 1f, Opacity * 0.6f));
        _spriteBatch.DrawString(_font, label, fontSize, position, Shade(LabelColor, 1f, Opacity));
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
        _pixel?.Dispose();
        _disc?.Dispose();
        _head?.Dispose();
        _spriteBatch = null;
        _pixel = null;
        _disc = null;
        _head = null;
    }

    private Color AxisColor(int axis) => axis switch { 0 => XColor, 1 => YColor, _ => ZColor };

    /// <summary>0 at the far end of the depth range, 1 at the near end: the shade and the sizes follow it.</summary>
    private static float Nearness(in OrientationGizmoEnd end) => (end.Depth + 1f) * 0.5f;

    /// <summary>
    /// The colour darkened by the shade and premultiplied by its alpha times the opacity, which is
    /// how the alpha-blend state reads it.
    /// </summary>
    private static Color4 Shade(Color color, float shade, float opacity)
    {
        var alpha = MathUtil.Clamp(color.A / 255f * opacity, 0f, 1f);
        var factor = shade * alpha / 255f;

        return new Color4(color.R * factor, color.G * factor, color.B * factor, alpha);
    }

    /// <summary>A white shape with an anti-aliased edge, premultiplied, from a coverage function over the texture's pixels.</summary>
    private static Texture CreateShapeTexture(GraphicsDevice device, Func<float, float, float> coverage)
    {
        var pixels = new Color[TextureSize * TextureSize];

        for (var y = 0; y < TextureSize; y++)
        {
            for (var x = 0; x < TextureSize; x++)
            {
                var value = (byte)MathF.Round(MathUtil.Clamp(coverage(x + 0.5f, y + 0.5f), 0f, 1f) * 255f);

                pixels[y * TextureSize + x] = new Color(value, value, value, value);
            }
        }

        return Texture.New2D(device, TextureSize, TextureSize, PixelFormat.R8G8B8A8_UNorm, pixels);
    }

    /// <summary>A disc filling the texture, its edge one pixel in so the anti-aliasing has room.</summary>
    private static float DiscCoverage(float x, float y)
    {
        const float centre = TextureSize * 0.5f;
        const float radius = centre - 1f;

        return radius - MathF.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) + 0.5f;
    }

    /// <summary>An isosceles triangle pointing +X: the base on the left edge, the apex at the right edge, one pixel in.</summary>
    private static float HeadCoverage(float x, float y)
    {
        const float inset = 1f;
        const float baseX = inset;
        const float apexX = TextureSize - inset;
        const float halfWidth = TextureSize * 0.5f - inset;
        const float middle = TextureSize * 0.5f;

        // Signed distance to each edge, positive inside: the base line, then the two slanted edges,
        // each as the cross product with the edge direction over the edge length
        var edgeLength = MathF.Sqrt((apexX - baseX) * (apexX - baseX) + halfWidth * halfWidth);
        var toBase = x - baseX;
        var toUpper = ((apexX - baseX) * (y - (middle - halfWidth)) - halfWidth * (x - baseX)) / edgeLength;
        var toLower = ((apexX - baseX) * ((middle + halfWidth) - y) - halfWidth * (x - baseX)) / edgeLength;

        return MathF.Min(toBase, MathF.Min(toUpper, toLower)) + 0.5f;
    }
}