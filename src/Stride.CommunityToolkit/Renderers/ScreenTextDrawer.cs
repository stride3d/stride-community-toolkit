using Stride.CommunityToolkit.Rendering.Text;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// The shared drawing path for screen-space text: projecting a world position, resolving the anchor,
/// and drawing the background, shadow and glyphs.
/// </summary>
/// <remarks>
/// Extracted because the two text renderers had drifted into carrying the same handful of defects
/// independently - projecting from a local rather than a world position, multiplying a background
/// colour into itself, culling text that had an explicit screen position. Each was fixed once, in one
/// renderer, and stayed broken in the other. There is now one copy to be wrong.
/// </remarks>
internal static class ScreenTextDrawer
{
    /// <summary>
    /// Projects a world position into screen pixels, reporting whether it is visible at all.
    /// </summary>
    /// <param name="worldPosition">The point to project.</param>
    /// <param name="viewProjection">The camera's view-projection matrix.</param>
    /// <param name="screenSize">Size of the render target in pixels.</param>
    /// <param name="screenPosition">The resulting pixel position.</param>
    /// <returns><see langword="false"/> when the point is behind the camera or outside the view.</returns>
    public static bool TryProject(Vector3 worldPosition, ref Matrix viewProjection, Vector2 screenSize, out Vector2 screenPosition)
    {
        screenPosition = default;

        var clipPosition = Vector4.Transform(new Vector4(worldPosition, 1f), viewProjection);

        // Behind the camera
        if (clipPosition.W <= 0f) return false;

        var inverseW = 1f / clipPosition.W;
        var normalizedX = clipPosition.X * inverseW;
        var normalizedY = clipPosition.Y * inverseW;
        var normalizedZ = clipPosition.Z * inverseW;

        if (normalizedZ < 0f || normalizedZ > 1f) return false;
        if (normalizedX < -1f || normalizedX > 1f) return false;
        if (normalizedY < -1f || normalizedY > 1f) return false;

        screenPosition = new Vector2(
            (normalizedX * 0.5f + 0.5f) * screenSize.X,
            (0.5f - normalizedY * 0.5f) * screenSize.Y);

        return true;
    }

    /// <summary>
    /// Draws one piece of text, with its background and shadow, at a screen position.
    /// </summary>
    /// <param name="spriteBatch">An already-begun sprite batch.</param>
    /// <param name="backgroundTexture">A 1x1 opaque white texture used for the background rectangle.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="position">Where to draw it, in pixels.</param>
    /// <param name="textSize">The measured size of the text, before <see cref="ScreenTextStyle.Scale"/>.</param>
    /// <param name="style">How to draw it.</param>
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture? backgroundTexture,
        string text,
        Vector2 position,
        Vector2 textSize,
        in ScreenTextStyle style)
    {
        var opacity = MathUtil.Clamp(style.Opacity, 0f, 1f);
        var scale = new Vector2(style.Scale);

        // SpriteBatch subtracts the origin in unscaled text pixels, so the anchor is a fraction of the
        // measured size. This is the thing TextAlignment cannot do: alignment only arranges lines
        // within a block and leaves single-line text exactly where it was, whichever value it takes.
        var anchorFactor = GetAnchorFactor(style.Anchor);
        var origin = new Vector2(textSize.X * anchorFactor.X, textSize.Y * anchorFactor.Y);

        if (style.EnableBackground && backgroundTexture is not null)
        {
            var scaledSize = textSize * scale;
            var topLeft = position - origin * scale - style.Padding;

            var background = new RectangleF(
                topLeft.X,
                topLeft.Y,
                scaledSize.X + style.Padding.X * 2,
                scaledSize.Y + style.Padding.Y * 2);

            var colour = style.BackgroundColor;

            colour.A *= opacity;

            // The rectangle stays axis-aligned and does not turn with Rotation. Rotated text with a
            // background is rare enough that the limitation is worth stating rather than working around.
            spriteBatch.Draw(backgroundTexture, background, colour);
        }

        if (style.EnableShadow)
        {
            spriteBatch.DrawString(
                style.Font,
                text,
                style.FontSize,
                position + style.ShadowOffset,
                WithOpacity(style.ShadowColor, opacity),
                style.Rotation,
                origin,
                scale,
                SpriteEffects.None,
                style.LayerDepth,
                style.Alignment);
        }

        spriteBatch.DrawString(
            style.Font,
            text,
            style.FontSize,
            position,
            WithOpacity(style.Color, opacity),
            style.Rotation,
            origin,
            scale,
            SpriteEffects.None,
            style.LayerDepth,
            style.Alignment);
    }

    /// <summary>
    /// Maps an anchor to the fraction of the text's width and height sitting before the anchor point.
    /// </summary>
    public static Vector2 GetAnchorFactor(TextAnchor anchor) => anchor switch
    {
        TextAnchor.TopLeft => new Vector2(0f, 0f),
        TextAnchor.TopCenter => new Vector2(0.5f, 0f),
        TextAnchor.TopRight => new Vector2(1f, 0f),
        TextAnchor.MiddleLeft => new Vector2(0f, 0.5f),
        TextAnchor.MiddleCenter => new Vector2(0.5f, 0.5f),
        TextAnchor.MiddleRight => new Vector2(1f, 0.5f),
        TextAnchor.BottomLeft => new Vector2(0f, 1f),
        TextAnchor.BottomCenter => new Vector2(0.5f, 1f),
        TextAnchor.BottomRight => new Vector2(1f, 1f),
        _ => Vector2.Zero,
    };

    /// <summary>
    /// Creates the 1x1 texture the background rectangle is stretched from.
    /// </summary>
    /// <remarks>
    /// Opaque white on purpose: the colour is supplied when the rectangle is drawn, so anything baked
    /// in here would be multiplied into the requested colour. Both renderers used to bake their
    /// default background in as well, squaring the colour and its alpha - which is why a background
    /// asked for at alpha 0.01 arrived at 0.0001 and never appeared.
    /// </remarks>
    public static Texture CreateBackgroundTexture(GraphicsDevice graphicsDevice)
        => Texture.New2D(graphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [Color.White]);

    private static Color4 WithOpacity(Color color, float opacity)
    {
        var result = color.ToColor4();

        result.A *= opacity;

        return result;
    }
}