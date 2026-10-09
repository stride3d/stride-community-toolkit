using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Draws every <see cref="WorldTextComponent"/> as text standing in the 3D scene rather than over it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WorldTextProcessor"/> adds one to the scene's compositor when the scene has a
/// <see cref="WorldTextComponent"/>. Each text is drawn at its entity's transform, scaled by
/// perspective and, by default, hidden by geometry in front of it.
/// </para>
/// <para>
/// <b>How it works, and what it costs.</b> <see cref="SpriteBatch"/> can be given explicit view and
/// projection matrices, which turns its 2D quads into geometry positioned in world space. Folding each
/// text's world matrix into the view matrix places and orients it, and the depth state decides whether
/// the scene occludes it. The catch is that the matrices belong to the batch rather than the draw call,
/// so each text needs its own <c>Begin</c>/<c>End</c> pair and therefore its own draw call: fine for
/// tens of labels, not for thousands.
/// </para>
/// </remarks>
public class WorldTextRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _defaultFont;
    private DisplayScale? _displayScale;

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _defaultFont = RendererDefaults.LoadDefaultFont(Content, Services, 32f);

        // The display's scale, for components whose rasterisation size follows it. Absent outside a
        // game, which leaves every text rasterised at exactly the FontSize asked for.
        if (Services.GetService<IGame>() is { } game)
        {
            _displayScale = DisplayScale.GetOrCreate(game);
        }
    }

    /// <summary>The block the profiler shows this renderer's GPU time under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("WorldText");

    private static readonly Color4 ProfileColor = new(0.9f, 0.9f, 0.3f, 1f);

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        using var _ = drawContext.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        if (_spriteBatch is null || _defaultFont is null) return;

        var processor = SceneInstance.GetCurrent(context)?.GetProcessor<WorldTextProcessor>();

        if (processor is null || processor.Texts.Count == 0) return;

        var camera = CompositorCameras.Find(context);

        if (camera is null) return;

        var view = camera.ViewMatrix;
        var projection = camera.ProjectionMatrix;
        var cameraPosition = camera.Entity?.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;

        foreach (var data in processor.Texts)
        {
            Draw(data, ref view, ref projection, cameraPosition, drawContext);
        }
    }

    private void Draw(WorldTextRenderData data, ref Matrix view, ref Matrix projection, Vector3 cameraPosition, RenderDrawContext drawContext)
    {
        var component = data.Component;

        if (!component.IsVisible || string.IsNullOrEmpty(component.Text)) return;
        if (component.Opacity <= 0f || component.Height <= 0f) return;

        var entity = data.Entity;

        if (entity is null) return;

        var world = entity.Transform.WorldMatrix;
        var origin = world.TranslationVector + Vector3.TransformNormal(component.Offset, world);

        var opacity = MathUtil.Clamp(component.Opacity, 0f, 1f);

        if (!TryApplyDistance(component, cameraPosition, origin, ref opacity)) return;

        var font = component.Font ?? _defaultFont!;

        // Rasterised that much larger on a scaled display, where the same world height covers that
        // many more pixels. Sharpness only: Height still decides the size in the world, and the scale
        // below divides the larger measurement straight back out
        var display = component.AutoScale && _displayScale is not null ? _displayScale.Value : 1f;
        var fontSize = component.FontSize * display;
        var textSize = data.GetMeasuredSize(_spriteBatch!, font, fontSize);

        if (textSize.Y <= 0f) return;

        // The text is measured in font pixels; this is what turns it into world units. Flipping Y is
        // not cosmetic: SpriteBatch builds its quads with Y increasing downwards, the screen
        // convention, so without the flip every string is drawn upside down in the world.
        var scale = component.Height / textSize.Y;
        var textToWorld = Matrix.Scaling(scale, -scale, scale) * GetOrientation(component, entity, origin, cameraPosition);

        textToWorld.TranslationVector = origin;

        _spriteBatch!.Begin(
            drawContext.GraphicsContext,
            textToWorld * view,
            projection,
            SpriteSortMode.Deferred,
            BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: component.DepthTest ? DepthStencilStates.DepthRead : DepthStencilStates.None,
            rasterizerState: RasterizerStates.CullNone);

        // Drawn at the origin of its own space, because that space has already been placed in the
        // world; the anchor is expressed as the SpriteBatch origin, in unscaled text pixels
        var anchorFactor = ScreenTextDrawer.GetAnchorFactor(component.Anchor);
        var anchorOrigin = new Vector2(textSize.X * anchorFactor.X, textSize.Y * anchorFactor.Y);

        if (component.GlowSize > 0f && component.GlowColor.A > 0)
        {
            // The reach is in font pixels at FontSize, so it grows with the rasterisation to keep
            // the same distance in the world
            DrawGlow(font, component, fontSize, component.GlowSize * display, anchorOrigin, opacity);
        }

        // Both alphas count: the colour's own says how transparent this text is by nature, Opacity is
        // the dimmer on top of it - and distance fading has already been folded into that dimmer
        DrawText(font, component, fontSize, Vector2.Zero, anchorOrigin, ToColor4(component.TextColor, opacity * component.TextColor.A / 255f));

        _spriteBatch.End();
    }

    /// <summary>
    /// A glow without a blur pass: the string drawn again in the glow colour at offsets around the
    /// letters, on two rings. The copies overlap most near the glyphs and least at the outer edge,
    /// so the alpha stacks into a falloff. All in the same batch, so it costs no extra draw calls.
    /// </summary>
    private void DrawGlow(SpriteFont font, WorldTextComponent component, float fontSize, float glowSize, Vector2 anchorOrigin, float opacity)
    {
        const int OuterCopies = 12;
        const int InnerCopies = 6;

        var strength = component.GlowColor.A / 255f * opacity;
        var outer = ToColor4(component.GlowColor, strength * 0.22f);
        var inner = ToColor4(component.GlowColor, strength * 0.35f);

        for (var i = 0; i < OuterCopies; i++)
        {
            var (sin, cos) = MathF.SinCos(i * MathF.Tau / OuterCopies);

            DrawText(font, component, fontSize, new Vector2(cos, sin) * glowSize, anchorOrigin, outer);
        }

        for (var i = 0; i < InnerCopies; i++)
        {
            var (sin, cos) = MathF.SinCos((i + 0.5f) * MathF.Tau / InnerCopies);

            DrawText(font, component, fontSize, new Vector2(cos, sin) * glowSize * 0.5f, anchorOrigin, inner);
        }
    }

    /// <summary>
    /// Draws the string at an offset from the origin of its own space, which has already been placed
    /// in the world; the anchor is expressed as the SpriteBatch origin, in unscaled text pixels.
    /// </summary>
    // fontSize: the rasterisation size, the component's own times the display's scale
    private void DrawText(SpriteFont font, WorldTextComponent component, float fontSize, Vector2 offset, Vector2 anchorOrigin, Color4 color)
        => _spriteBatch!.DrawString(
            font,
            component.Text,
            fontSize,
            offset,
            color,
            0f,
            anchorOrigin,
            Vector2.One,
            SpriteEffects.None,
            0f,
            component.Alignment);

    /// <summary>
    /// The colour as SpriteBatch takes it, with its alpha replaced by the given one and its channels
    /// premultiplied by that alpha.
    /// </summary>
    /// <remarks>
    /// The premultiply is what makes alpha mean "fade". <see cref="BlendStates.AlphaBlend"/> is the
    /// premultiplied blend - source factor <c>One</c> - so a colour handed over straight is added at
    /// full strength while only the background is scaled down: text at a tenth alpha would still be
    /// drawn at full brightness over a dark scene and simply never fade out. Scaling the channels by
    /// the alpha first turns the same blend back into the fade it reads as.
    /// </remarks>
    private static Color4 ToColor4(Color color, float alpha)
    {
        alpha = MathUtil.Clamp(alpha, 0f, 1f);

        return new(color.R / 255f * alpha, color.G / 255f * alpha, color.B / 255f * alpha, alpha);
    }

    /// <summary>
    /// Builds the rotation part of the text's world matrix.
    /// </summary>
    private static Matrix GetOrientation(WorldTextComponent component, Entity entity, Vector3 origin, Vector3 cameraPosition)
    {
        if (!component.Billboard)
        {
            // Keep the entity's own orientation, but strip its scale: scale is applied through Height
            // so the text does not end up sized twice
            var rotation = entity.Transform.WorldMatrix;

            rotation.TranslationVector = Vector3.Zero;

            return Matrix.RotationQuaternion(Quaternion.RotationMatrix(rotation));
        }

        var toCamera = cameraPosition - origin;

        if (component.KeepUpright)
        {
            // Turning about Y only, so the text never rolls when the camera tilts
            toCamera.Y = 0f;
        }

        if (toCamera.LengthSquared() < MathUtil.ZeroTolerance)
        {
            return Matrix.Identity;
        }

        toCamera.Normalize();

        var up = component.KeepUpright ? Vector3.UnitY : Vector3.Normalize(Vector3.Cross(Vector3.Cross(toCamera, Vector3.UnitY), toCamera));

        if (!float.IsFinite(up.X) || Math.Abs(Vector3.Dot(toCamera, up)) > 0.999f)
        {
            up = Vector3.UnitZ;
        }

        var right = Vector3.Normalize(Vector3.Cross(up, toCamera));

        up = Vector3.Cross(toCamera, right);

        return new Matrix
        {
            M11 = right.X,
            M12 = right.Y,
            M13 = right.Z,
            M21 = up.X,
            M22 = up.Y,
            M23 = up.Z,
            M31 = toCamera.X,
            M32 = toCamera.Y,
            M33 = toCamera.Z,
            M44 = 1f,
        };
    }

    /// <summary>
    /// Applies the distance cutoff and fade, reporting whether the text should be drawn at all.
    /// </summary>
    private static bool TryApplyDistance(WorldTextComponent component, Vector3 cameraPosition, Vector3 origin, ref float opacity)
    {
        if (component.MaxDistance <= 0f && component.FadeStartDistance <= 0f) return true;

        var distance = Vector3.Distance(cameraPosition, origin);

        if (component.MaxDistance > 0f && distance > component.MaxDistance) return false;

        if (component.FadeStartDistance > 0f && component.MaxDistance > component.FadeStartDistance)
        {
            opacity *= 1f - MathUtil.Clamp((distance - component.FadeStartDistance) / (component.MaxDistance - component.FadeStartDistance), 0f, 1f);
        }

        return opacity > 0f;
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
    }
}