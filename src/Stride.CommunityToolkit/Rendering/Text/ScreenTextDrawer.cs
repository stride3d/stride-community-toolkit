using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// The shared drawing path for screen-space text: projecting a world position, resolving the anchor,
/// and drawing the background, shadow and glyphs.
/// </summary>
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
    internal static bool TryProject(Vector3 worldPosition, ref Matrix viewProjection, Vector2 screenSize, out Vector2 screenPosition)
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
    internal static void Draw(
        SpriteBatch spriteBatch,
        Texture? backgroundTexture,
        string text,
        Vector2 position,
        Vector2 textSize,
        ScreenTextStyle style)
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
            DrawBackground(spriteBatch, backgroundTexture, position, textSize, style);
        }

        if (style.EnableShadow)
        {
            DrawString(style.ShadowOffset, WithOpacity(style.ShadowColor, opacity));
        }

        if (style.EnableOutline && style.OutlineWidth > 0f)
        {
            var outlineColor = WithOpacity(style.OutlineColor, opacity);

            // The string once in each of the eight directions round itself, the width away: the text
            // drawn over them leaves a ring of that width
            for (var direction = 0; direction < 8; direction++)
            {
                var (sin, cos) = MathF.SinCos(direction * MathF.PI / 4f);

                DrawString(new Vector2(cos, sin) * style.OutlineWidth, outlineColor);
            }
        }

        DrawString(Vector2.Zero, WithOpacity(style.Color, opacity));

        void DrawString(Vector2 offset, Color4 color)
            => spriteBatch.DrawString(style.Font, text, style.FontSize, position + style.TextOffset + offset, color, style.Rotation, origin, scale, SpriteEffects.None, style.LayerDepth, style.Alignment);
    }

    /// <summary>
    /// Draws only the background strip a text of <paramref name="textSize"/> at <paramref name="position"/>
    /// would get, for a caller that then draws the text itself in more than one run or colour.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to draw with, already begun.</param>
    /// <param name="backgroundTexture">A one-pixel texture, tinted to the background colour.</param>
    /// <param name="position">Where the text is drawn, in pixels.</param>
    /// <param name="textSize">The measured size of the whole text, before <see cref="ScreenTextStyle.Scale"/>.</param>
    /// <param name="style">How the text is drawn; the padding, background colour, anchor and opacity are used.</param>
    internal static void DrawBackground(
        SpriteBatch spriteBatch,
        Texture backgroundTexture,
        Vector2 position,
        Vector2 textSize,
        in ScreenTextStyle style)
    {
        var opacity = MathUtil.Clamp(style.Opacity, 0f, 1f);
        var scale = new Vector2(style.Scale);
        var anchorFactor = GetAnchorFactor(style.Anchor);
        var origin = new Vector2(textSize.X * anchorFactor.X, textSize.Y * anchorFactor.Y);
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

    /// <summary>
    /// Maps an anchor to the fraction of the text's width and height sitting before the anchor point.
    /// <see cref="Anchor"/> is laid out as a 3 by 3 grid row by row, so the ordinal gives the column and the row.
    /// </summary>
    internal static Vector2 GetAnchorFactor(Anchor anchor) => new Vector2((int)anchor % 3, (int)anchor / 3) * 0.5f;

    /// <summary>
    /// Applies a distance cutoff and fade to <paramref name="opacity"/>: nothing beyond <paramref name="maxDistance"/>,
    /// a linear fade from <paramref name="fadeStartDistance"/> to it when both are set, and no change when neither is.
    /// Zero means "not set" for either.
    /// </summary>
    /// <returns><see langword="false"/> when the text should not be drawn at all.</returns>
    internal static bool TryApplyDistanceFade(float fadeStartDistance, float maxDistance, Vector3 cameraPosition, Vector3 position, ref float opacity)
    {
        if (maxDistance <= 0f && fadeStartDistance <= 0f) return true;

        var distance = Vector3.Distance(cameraPosition, position);

        if (maxDistance > 0f && distance > maxDistance) return false;

        if (fadeStartDistance > 0f && maxDistance > fadeStartDistance)
        {
            opacity *= 1f - MathUtil.Clamp((distance - fadeStartDistance) / (maxDistance - fadeStartDistance), 0f, 1f);
        }

        return opacity > 0f;
    }

    /// <summary>
    /// Creates the 1x1 texture the background rectangle is stretched from.
    /// </summary>
    /// <remarks>
    /// Opaque white on purpose: the colour is supplied when the rectangle is drawn, so anything baked
    /// in here would be multiplied into the requested colour.
    /// </remarks>
    internal static Texture CreateBackgroundTexture(GraphicsDevice graphicsDevice)
        => Texture.New2D(graphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [Color.White]);

    /// <summary>The colour as the sprite batch takes it: its own alpha times the opacity, premultiplied.</summary>
    private static Color4 WithOpacity(Color color, float opacity) => Premultiplied(color, color.A / 255f * opacity);

    /// <summary>
    /// The colour as the sprite batch takes it: its alpha replaced by <paramref name="alpha"/> and its
    /// channels premultiplied by it.
    /// </summary>
    /// <remarks>
    /// The premultiply is what makes alpha mean "fade". The batch is begun with
    /// <see cref="BlendStates.AlphaBlend"/>, which is the premultiplied blend - source factor
    /// <c>One</c> - so a colour handed over straight is added at full strength while only the
    /// background is scaled down: text at a tenth opacity stayed at full brightness over a dark
    /// scene, and the default half-alpha shadow drew as a solid second copy. Scaling the channels
    /// by the alpha first turns the same blend back into the fade it reads as. The background
    /// rectangle does not need it: its texture is opaque white, and the sprite batch premultiplies a
    /// texture's own alpha into the tint on the way through.
    /// </remarks>
    internal static Color4 Premultiplied(Color color, float alpha)
    {
        alpha = MathUtil.Clamp(alpha, 0f, 1f);

        return new Color4(color.R / 255f * alpha, color.G / 255f * alpha, color.B / 255f * alpha, alpha);
    }
}