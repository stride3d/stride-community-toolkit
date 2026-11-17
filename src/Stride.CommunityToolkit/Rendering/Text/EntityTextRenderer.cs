using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

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
public class EntityTextRenderer : SpriteTextRendererBase
{
    private readonly List<EntityTextRenderData> _drawList = [];

    /// <summary>The block the profiler shows this renderer's GPU time under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("EntityText");

    private static readonly Color4 ProfileColor = new(0.9f, 0.7f, 0.3f, 1f);

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        using var _ = drawContext.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        // Resolved per frame rather than cached, so a change of scene is picked up
        var processor = SceneInstance.GetCurrent(context)?.GetProcessor<EntityTextProcessor>();

        if (processor is null || processor.Texts.Count == 0) return;

        if (!TryBeginFrame(context, drawContext, out var frame)) return;

        BuildDrawList(processor);

        if (_drawList.Count == 0) return;

        var viewProjection = frame.Camera.ViewProjectionMatrix;
        var cameraPosition = frame.Camera.Entity?.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;

        frame.Batch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        foreach (var data in _drawList)
        {
            Draw(data, ref viewProjection, cameraPosition, frame.ScreenSize);
        }

        frame.Batch.End();

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
        var display = DisplayScaleFactor(component.AutoScale);
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

        // Anchored text takes its anchor from the point it is pinned to, so it always grows inwards
        // and stays on screen. Anchor is ignored here rather than obeyed, because the combination
        // that a HUD wants is the only one that keeps the text visible - and getting it wrong shows
        // up as text half off the edge of the window. Screen mode is there when the caller wants to
        // place and anchor text independently.
        DrawAt(data, ResolveAnchoredPosition(component.ScreenAnchor, offset, screenSize), opacity, component.ScreenAnchor, display);
    }

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

        if (!ScreenTextDrawer.TryApplyDistanceFade(component.FadeStartDistance, component.MaxDistance, cameraPosition, worldPosition, ref opacity)) return false;

        return ScreenTextDrawer.TryProject(worldPosition, ref viewProjection, screenSize, out screenPosition);
    }

    /// <summary>
    /// Resolves a point of the window into a pixel position, with the offset always pointing inwards: away from
    /// a right edge or a bottom edge, and as given from the left, the top or the middle.
    /// </summary>
    private static Vector2 ResolveAnchoredPosition(Anchor anchor, Vector2 offset, Vector2 screenSize)
    {
        var x = anchor is Anchor.TopRight or Anchor.MiddleRight or Anchor.BottomRight ? -offset.X : offset.X;
        var y = anchor is Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight ? -offset.Y : offset.Y;

        return anchor.At(screenSize) + new Vector2(x, y);
    }

    /// <summary>Draws one text at a screen position, styled from its component.</summary>
    /// <param name="data">The text and its entity.</param>
    /// <param name="position">Where the anchor lands, in pixels.</param>
    /// <param name="opacity">The opacity after any distance fade.</param>
    /// <param name="anchor">The point of the text put at <paramref name="position"/>.</param>
    /// <param name="display">The display's scale, applied to every pixel figure; 1 when not following it.</param>
    private void DrawAt(EntityTextRenderData data, Vector2 position, float opacity, Anchor anchor, float display)
    {
        var component = data.Component;
        var font = component.Font ?? DefaultFont!;

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
            SpriteBatch!,
            BackgroundTexture,
            component.Text,
            position,
            data.GetMeasuredSize(SpriteBatch!, font, fontSize),
            style);
    }
}