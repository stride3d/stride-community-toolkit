using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Draws the text of every <see cref="EntityTextComponent"/> in the scene, as a screen-space overlay
/// over the rendered 3D scene.
/// </summary>
/// <remarks>
/// <para>
/// Add one of these to the graphics compositor - <c>game.AddSceneRenderer(new EntityTextRenderer())</c> -
/// and any entity carrying an <see cref="EntityTextComponent"/> is drawn, wherever it sits in the
/// entity hierarchy.
/// </para>
/// <para>
/// Text is drawn with <see cref="SpriteBatch"/> and no depth testing, so it always appears on top of
/// the scene rather than being occluded by geometry in front of it.
/// </para>
/// </remarks>
public class EntityTextRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _defaultFont;
    private Texture? _backgroundTexture;
    private readonly List<EntityTextRenderData> _drawList = [];

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _defaultFont = Content.Load<SpriteFont>(RendererDefaults.DefaultFontPath);

        _backgroundTexture = ScreenTextDrawer.CreateBackgroundTexture(GraphicsDevice);
    }

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        if (_spriteBatch is null || _defaultFont is null) return;

        // Resolved per frame rather than cached, so a change of scene or camera is picked up
        var processor = SceneInstance.GetCurrent(context)?.GetProcessor<EntityTextProcessor>();

        if (processor is null || processor.Texts.Count == 0) return;

        var camera = context.Tags.Get(GraphicsCompositor.Current)?.Cameras[0]?.Camera;

        if (camera is null) return;

        var viewport = drawContext.CommandList.Viewport;
        var screenSize = new Vector2(viewport.Width, viewport.Height);

        if (screenSize.X <= 0 || screenSize.Y <= 0) return;

        BuildDrawList(processor);

        if (_drawList.Count == 0) return;

        var viewProjection = camera.ViewProjectionMatrix;
        var cameraPosition = camera.Entity?.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;

        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        foreach (var data in _drawList)
        {
            Draw(data, ref viewProjection, cameraPosition, screenSize);
        }

        _spriteBatch.End();

        _drawList.Clear();
    }

    /// <summary>
    /// Collects the visible texts and orders them so higher layer depths are drawn last, and so end
    /// up on top.
    /// </summary>
    /// <remarks>
    /// The ordering is done here rather than by handing the depth to <see cref="SpriteBatch"/>,
    /// because submitting in order works the same whatever sort mode the batch was begun with and
    /// leaves nothing about the draw order implicit.
    /// </remarks>
    private void BuildDrawList(EntityTextProcessor processor)
    {
        foreach (var data in processor.Texts)
        {
            var component = data.Component;

            if (!component.IsVisible || string.IsNullOrEmpty(component.Text)) continue;

            if (component.Opacity <= 0f || component.Scale <= 0f) continue;

            _drawList.Add(data);
        }

        _drawList.Sort(static (left, right) => left.Component.LayerDepth.CompareTo(right.Component.LayerDepth));
    }

    private void Draw(EntityTextRenderData data, ref Matrix viewProjection, Vector3 cameraPosition, Vector2 screenSize)
    {
        var component = data.Component;
        var opacity = MathUtil.Clamp(component.Opacity, 0f, 1f);

        if (component.PositionMode == TextPositionMode.World)
        {
            if (!TryGetWorldScreenPosition(data, ref viewProjection, cameraPosition, screenSize, ref opacity, out var worldScreenPosition))
            {
                return;
            }

            DrawAt(data, worldScreenPosition + component.Offset, opacity, component.Anchor);

            return;
        }

        // Screen and anchored text is not projected, so it is never culled for being out of view.
        // The old renderer tested the entity's world position before looking at the explicit
        // position, which made a fixed HUD vanish whenever its entity left the frustum.
        if (component.PositionMode == TextPositionMode.Screen)
        {
            DrawAt(data, component.ScreenPosition + component.Offset, opacity, component.Anchor);

            return;
        }

        // Anchored text takes its anchor from the corner it is pinned to, so it always grows inwards
        // and stays on screen. Anchor is ignored here rather than obeyed, because the combination
        // that a HUD wants is the only one that keeps the text visible - and getting it wrong shows
        // up as text half off the edge of the window. Screen mode is there when the caller wants to
        // place and anchor text independently.
        DrawAt(data, ResolveAnchoredPosition(component, screenSize), opacity, GetAnchorForCorner(component.ScreenAnchor));
    }

    /// <summary>
    /// Returns the text anchor that keeps text pinned to the given corner inside the window.
    /// </summary>
    private static TextAnchor GetAnchorForCorner(DisplayPosition corner) => corner switch
    {
        DisplayPosition.TopRight => TextAnchor.TopRight,
        DisplayPosition.BottomLeft => TextAnchor.BottomLeft,
        DisplayPosition.BottomRight => TextAnchor.BottomRight,
        _ => TextAnchor.TopLeft,
    };

    /// <summary>
    /// Projects the entity into screen space, reporting whether the text should be drawn at all and
    /// applying any distance fade to <paramref name="opacity"/>.
    /// </summary>
    private static bool TryGetWorldScreenPosition(
        EntityTextRenderData data,
        ref Matrix viewProjection,
        Vector3 cameraPosition,
        Vector2 screenSize,
        ref float opacity,
        out Vector2 screenPosition)
    {
        screenPosition = default;

        // The world matrix, not Transform.Position: for an entity parented to another, Position is
        // relative to the parent and would place the text somewhere else entirely
        var worldPosition = data.Entity.Transform.WorldMatrix.TranslationVector;
        var component = data.Component;

        if (component.MaxDistance is { } maxDistance || component.FadeStartDistance is not null)
        {
            var distance = Vector3.Distance(cameraPosition, worldPosition);

            if (component.MaxDistance is { } limit && distance > limit) return false;

            if (component.FadeStartDistance is { } fadeStart && component.MaxDistance is { } fadeEnd && fadeEnd > fadeStart)
            {
                var fade = 1f - MathUtil.Clamp((distance - fadeStart) / (fadeEnd - fadeStart), 0f, 1f);

                opacity *= fade;

                if (opacity <= 0f) return false;
            }
        }

        return ScreenTextDrawer.TryProject(worldPosition, ref viewProjection, screenSize, out screenPosition);
    }

    /// <summary>
    /// Resolves a window corner into a pixel position, with the offset always pointing inwards.
    /// </summary>
    private static Vector2 ResolveAnchoredPosition(EntityTextComponent component, Vector2 screenSize)
        => component.ScreenAnchor switch
        {
            DisplayPosition.TopRight => new Vector2(screenSize.X - component.Offset.X, component.Offset.Y),
            DisplayPosition.BottomLeft => new Vector2(component.Offset.X, screenSize.Y - component.Offset.Y),
            DisplayPosition.BottomRight => new Vector2(screenSize.X - component.Offset.X, screenSize.Y - component.Offset.Y),
            _ => component.Offset,
        };

    private void DrawAt(EntityTextRenderData data, Vector2 position, float opacity, TextAnchor anchor)
    {
        var component = data.Component;
        var font = component.Font ?? _defaultFont!;

        var style = new ScreenTextStyle
        {
            Font = font,
            FontSize = component.FontSize,
            Color = component.TextColor,
            Anchor = anchor,
            Alignment = component.Alignment,
            Scale = component.Scale,
            Rotation = component.Rotation,
            Opacity = opacity,
            LayerDepth = component.LayerDepth,
            EnableShadow = component.EnableShadow,
            ShadowColor = component.ShadowColor,
            ShadowOffset = component.ShadowOffset,
            EnableBackground = component.EnableBackground,
            BackgroundColor = component.BackgroundColor ?? RendererDefaults.DefaultBackground,
            Padding = component.Padding,
        };

        ScreenTextDrawer.Draw(
            _spriteBatch!,
            _backgroundTexture,
            component.Text,
            position,
            data.GetMeasuredSize(_spriteBatch!, font),
            style);
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
        _backgroundTexture?.Dispose();
    }
}