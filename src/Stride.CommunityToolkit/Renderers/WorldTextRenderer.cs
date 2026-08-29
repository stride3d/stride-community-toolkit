using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Draws every <see cref="WorldTextComponent"/> as text standing in the 3D scene rather than over it.
/// </summary>
/// <remarks>
/// <para>
/// Add one to the graphics compositor - <c>game.AddSceneRenderer(new WorldTextRenderer())</c> - and any
/// entity carrying a <see cref="WorldTextComponent"/> is drawn at its transform, scaled by perspective
/// and, by default, hidden by geometry in front of it.
/// </para>
/// <para>
/// <b>How it works, and what it costs.</b> <see cref="SpriteBatch"/> can be given explicit view and
/// projection matrices, which turns its 2D quads into geometry positioned in world space. Folding each
/// text's world matrix into the view matrix places and orients it, and the depth state decides whether
/// the scene occludes it. The catch is that the matrices belong to the batch rather than the draw call,
/// so each text needs its own <c>Begin</c>/<c>End</c> pair and therefore its own draw call. That is
/// fine for the tens of labels this is meant for - axis names, debug markers, a sign over a spawn point
/// - and would not be fine for thousands. Batching them would mean rendering each distinct string to a
/// texture and drawing those through <see cref="Sprite3DBatch"/>, which takes a world matrix per
/// sprite; worth doing only if the counts ever justify the cache it needs.
/// </para>
/// </remarks>
public class WorldTextRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _defaultFont;

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _defaultFont = Content.Load<SpriteFont>(RendererDefaults.DefaultFontPath);
    }

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        if (_spriteBatch is null || _defaultFont is null) return;

        var processor = SceneInstance.GetCurrent(context)?.GetProcessor<WorldTextProcessor>();

        if (processor is null || processor.Texts.Count == 0) return;

        var camera = context.Tags.Get(GraphicsCompositor.Current)?.Cameras[0]?.Camera;

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
        var textSize = data.GetMeasuredSize(_spriteBatch!, font);

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

        _spriteBatch.DrawString(
            font,
            component.Text,
            component.FontSize,
            Vector2.Zero,
            new Color4(component.TextColor.R / 255f, component.TextColor.G / 255f, component.TextColor.B / 255f, component.TextColor.A / 255f * opacity),
            0f,
            new Vector2(textSize.X * anchorFactor.X, textSize.Y * anchorFactor.Y),
            Vector2.One,
            SpriteEffects.None,
            0f,
            component.Alignment);

        _spriteBatch.End();
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
        if (component.MaxDistance is null && component.FadeStartDistance is null) return true;

        var distance = Vector3.Distance(cameraPosition, origin);

        if (component.MaxDistance is { } limit && distance > limit) return false;

        if (component.FadeStartDistance is { } fadeStart && component.MaxDistance is { } fadeEnd && fadeEnd > fadeStart)
        {
            opacity *= 1f - MathUtil.Clamp((distance - fadeStart) / (fadeEnd - fadeStart), 0f, 1f);
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