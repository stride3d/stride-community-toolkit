using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Draws the text of every <see cref="EntityTextComponent"/> in the scene, as a screen-space overlay
/// over the rendered 3D scene.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EntityTextProcessor"/> adds one to the scene's compositor when the scene has an
/// <see cref="EntityTextComponent"/>, wherever it sits in the entity hierarchy.
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
    private DisplayScale? _displayScale;
    private readonly List<EntityTextRenderData> _drawList = [];

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _defaultFont = RendererDefaults.LoadDefaultFont(Content, Services, 32f);

        _backgroundTexture = ScreenTextDrawer.CreateBackgroundTexture(GraphicsDevice);

        // The display's scale, for components whose pixel sizes follow it. Absent outside a game,
        // which leaves every size at exactly the pixels asked for.
        if (Services.GetService<IGame>() is { } game)
        {
            _displayScale = DisplayScale.GetOrCreate(game);
        }
    }

    /// <summary>The block the profiler shows this renderer's GPU time under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("EntityText");

    private static readonly Color4 ProfileColor = new(0.9f, 0.7f, 0.3f, 1f);

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        using var _ = drawContext.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        if (_spriteBatch is null || _defaultFont is null) return;

        // Resolved per frame rather than cached, so a change of scene or camera is picked up
        var processor = SceneInstance.GetCurrent(context)?.GetProcessor<EntityTextProcessor>();

        if (processor is null || processor.Texts.Count == 0) return;

        var camera = CompositorCameras.Find(context);

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

        // Everything the component measures in pixels is multiplied by this; a projected world
        // position is not, because it is not a design figure but where the entity happens to be
        var display = component.AutoScale && _displayScale is not null ? _displayScale.Value : 1f;
        var offset = component.Offset * display;

        if (component.PositionMode == TextPositionMode.World)
        {
            if (!TryGetWorldScreenPosition(data, ref viewProjection, cameraPosition, screenSize, ref opacity, out var worldScreenPosition))
            {
                return;
            }

            DrawAt(data, worldScreenPosition + offset, opacity, component.Anchor, display);

            return;
        }

        // Screen and anchored text is not projected, so it is never culled for being out of view.
        // The old renderer tested the entity's world position before looking at the explicit
        // position, which made a fixed HUD vanish whenever its entity left the frustum.
        if (component.PositionMode == TextPositionMode.Screen)
        {
            DrawAt(data, component.ScreenPosition * display + offset, opacity, component.Anchor, display);

            return;
        }

        // Anchored text takes its anchor from the corner it is pinned to, so it always grows inwards
        // and stays on screen. Anchor is ignored here rather than obeyed, because the combination
        // that a HUD wants is the only one that keeps the text visible - and getting it wrong shows
        // up as text half off the edge of the window. Screen mode is there when the caller wants to
        // place and anchor text independently.
        DrawAt(data, ResolveAnchoredPosition(component.ScreenAnchor, offset, screenSize), opacity, GetAnchorForCorner(component.ScreenAnchor), display);
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

        if (component.MaxDistance > 0f || component.FadeStartDistance > 0f)
        {
            var distance = Vector3.Distance(cameraPosition, worldPosition);

            if (component.MaxDistance > 0f && distance > component.MaxDistance) return false;

            if (component.FadeStartDistance > 0f && component.MaxDistance > component.FadeStartDistance)
            {
                var fade = 1f - MathUtil.Clamp((distance - component.FadeStartDistance) / (component.MaxDistance - component.FadeStartDistance), 0f, 1f);

                opacity *= fade;

                if (opacity <= 0f) return false;
            }
        }

        return ScreenTextDrawer.TryProject(worldPosition, ref viewProjection, screenSize, out screenPosition);
    }

    /// <summary>
    /// Resolves a window corner into a pixel position, with the offset always pointing inwards.
    /// </summary>
    private static Vector2 ResolveAnchoredPosition(DisplayPosition corner, Vector2 offset, Vector2 screenSize)
        => corner switch
        {
            DisplayPosition.TopRight => new Vector2(screenSize.X - offset.X, offset.Y),
            DisplayPosition.BottomLeft => new Vector2(offset.X, screenSize.Y - offset.Y),
            DisplayPosition.BottomRight => new Vector2(screenSize.X - offset.X, screenSize.Y - offset.Y),
            _ => offset,
        };

    // display: the display's scale, applied to every pixel figure; 1 when not following it
    private void DrawAt(EntityTextRenderData data, Vector2 position, float opacity, TextAnchor anchor, float display)
    {
        var component = data.Component;
        var font = component.Font ?? _defaultFont!;

        // Rasterised at the scaled size rather than drawn scaled, so the glyphs stay sharp
        var fontSize = component.FontSize * display;

        var style = new ScreenTextStyle
        {
            Font = font,
            FontSize = fontSize,
            Color = component.TextColor,
            Anchor = anchor,
            Alignment = component.Alignment,
            Scale = component.Scale,
            Rotation = component.Rotation,
            Opacity = opacity,
            LayerDepth = component.LayerDepth,
            EnableShadow = component.EnableShadow,
            ShadowColor = component.ShadowColor,
            ShadowOffset = component.ShadowOffset * display,
            EnableOutline = component.EnableOutline,
            OutlineColor = component.OutlineColor,
            OutlineWidth = component.OutlineWidth * display,
            EnableBackground = component.EnableBackground,
            BackgroundColor = component.BackgroundColor ?? RendererDefaults.DefaultBackground,
            Padding = component.Padding * display,
        };

        ScreenTextDrawer.Draw(
            _spriteBatch!,
            _backgroundTexture,
            component.Text,
            position,
            data.GetMeasuredSize(_spriteBatch!, font, fontSize),
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