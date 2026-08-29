using Stride.CommunityToolkit.Rendering.Text;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System.Globalization;
using System.Text;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Scene renderer that draws per-entity debug information - the entity's name and/or position - as
/// screen-space text over the 3D scene.
/// </summary>
/// <remarks>
/// <para>
/// Appearance is controlled through <see cref="EntityDebugSceneRendererOptions"/>. Unlike
/// <see cref="EntityTextRenderer"/>, which draws text that entities opt into by carrying an
/// <see cref="EntityTextComponent"/>, this labels entities automatically and styles them all the same
/// way - it is a debugging overlay to switch on, not authored content.
/// </para>
/// <para>
/// The two share their drawing through <see cref="ScreenTextDrawer"/>, so anchoring, backgrounds,
/// shadows and projection behave identically in both.
/// </para>
/// </remarks>
public class EntityDebugSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture? _backgroundTexture;
    private readonly StringBuilder _stringBuilder = new();
    private readonly EntityDebugSceneRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDebugSceneRenderer"/> class with default rendering options.
    /// </summary>
    public EntityDebugSceneRenderer() => _options = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDebugSceneRenderer"/> class with the specified rendering options.
    /// </summary>
    /// <param name="options">Options to customize debug text appearance. If null, defaults are used.</param>
    public EntityDebugSceneRenderer(EntityDebugSceneRendererOptions? options = null) => _options = options ?? new();

    /// <inheritdoc />
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>(RendererDefaults.DefaultFontPath);
        _backgroundTexture = ScreenTextDrawer.CreateBackgroundTexture(GraphicsDevice);
    }

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        if (!_options.ShowEntityName && !_options.ShowEntityPosition) return;

        if (_spriteBatch is null || _font is null) return;

        // Resolved per frame rather than cached at initialisation, so swapping the scene or the
        // camera does not leave the overlay drawing against the ones it started with
        var scene = SceneInstance.GetCurrent(context)?.RootScene;
        var camera = context.Tags.Get(GraphicsCompositor.Current)?.Cameras[0]?.Camera;

        if (scene is null || camera is null || scene.Entities.Count == 0) return;

        var viewport = drawContext.CommandList.Viewport;
        var screenSize = new Vector2(viewport.Width, viewport.Height);

        if (screenSize.X <= 0 || screenSize.Y <= 0) return;

        var viewProjection = camera.ViewProjectionMatrix;
        var cameraPosition = camera.Entity?.Transform.WorldMatrix.TranslationVector ?? Vector3.Zero;

        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        foreach (var entity in scene.Entities)
        {
            DrawEntity(entity, ref viewProjection, cameraPosition, screenSize);
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Labels one entity and, when enabled, everything nested beneath it.
    /// </summary>
    private void DrawEntity(Entity entity, ref Matrix viewProjection, Vector3 cameraPosition, Vector2 screenSize)
    {
        if (_options.EntityFilter?.Invoke(entity) != false)
        {
            DrawLabel(entity, ref viewProjection, cameraPosition, screenSize);
        }

        if (!_options.IncludeChildEntities) return;

        foreach (var child in entity.Transform.Children)
        {
            DrawEntity(child.Entity, ref viewProjection, cameraPosition, screenSize);
        }
    }

    private void DrawLabel(Entity entity, ref Matrix viewProjection, Vector3 cameraPosition, Vector2 screenSize)
    {
        // The world matrix, not Transform.Position: for a nested entity, Position is relative to its
        // parent, so labelling a child by it would place the text somewhere else entirely
        var worldPosition = entity.Transform.WorldMatrix.TranslationVector;

        if (_options.MaxDistance is { } limit && Vector3.Distance(cameraPosition, worldPosition) > limit)
        {
            return;
        }

        if (!ScreenTextDrawer.TryProject(worldPosition, ref viewProjection, screenSize, out var screenPosition))
        {
            return;
        }

        var position = screenPosition + _options.Offset;

        // With a separate colour the two parts have to be drawn separately, so they are stacked
        // instead of chained along one line - see EntityDebugSceneRendererOptions.PositionColor
        if (_options.PositionColor is { } positionColor && _options.ShowEntityPosition)
        {
            var name = _options.ShowEntityName ? entity.Name : null;
            var nameSize = Vector2.Zero;

            if (!string.IsNullOrEmpty(name))
            {
                nameSize = _spriteBatch!.MeasureString(_font, name, _options.FontSize);

                Draw(name, position, nameSize, _options.FontColor);
            }

            var coordinates = FormatPosition(worldPosition);

            Draw(coordinates, position + new Vector2(0, nameSize.Y), Measure(coordinates), positionColor);

            return;
        }

        var text = GetDisplayText(entity, worldPosition);

        if (string.IsNullOrWhiteSpace(text)) return;

        Draw(text, position, Measure(text), _options.FontColor);
    }

    private Vector2 Measure(string text) => _spriteBatch!.MeasureString(_font, text, _options.FontSize);

    /// <summary>
    /// Draws one line of debug text through the shared drawer.
    /// </summary>
    /// <remarks>
    /// The size is measured once by the caller and passed in. The previous version measured for the
    /// background and then let <c>DrawString</c> measure again internally, which for a position
    /// readout - a string that changes every frame and so can never be cached - was twice the work
    /// for every labelled entity.
    /// </remarks>
    private void Draw(string text, Vector2 position, Vector2 textSize, Color color)
    {
        var style = new ScreenTextStyle
        {
            Font = _font!,
            FontSize = _options.FontSize,
            Color = color,
            Anchor = _options.Anchor,
            Alignment = TextAlignment.Left,
            Scale = 1f,
            Rotation = 0f,
            Opacity = 1f,
            LayerDepth = 0f,
            EnableShadow = _options.EnableShadow,
            ShadowColor = _options.ShadowColor,
            ShadowOffset = _options.ShadowOffset,
            EnableBackground = _options.EnableBackground,
            BackgroundColor = _options.BackgroundColor ?? RendererDefaults.DefaultDebugBackground,
            Padding = _options.Padding,
        };

        ScreenTextDrawer.Draw(_spriteBatch!, _backgroundTexture, text, position, textSize, style);
    }

    /// <summary>
    /// Builds the combined debug text for an entity.
    /// </summary>
    private string GetDisplayText(Entity entity, Vector3 worldPosition)
    {
        _stringBuilder.Clear();

        if (_options.ShowEntityName)
        {
            _stringBuilder.Append(entity.Name);
        }

        if (_options.ShowEntityPosition)
        {
            if (_stringBuilder.Length > 0) _stringBuilder.Append(": ");

            AppendPosition(_stringBuilder, worldPosition);
        }

        return _stringBuilder.ToString();
    }

    private string FormatPosition(Vector3 worldPosition)
    {
        _stringBuilder.Clear();

        AppendPosition(_stringBuilder, worldPosition);

        return _stringBuilder.ToString();
    }

    private static void AppendPosition(StringBuilder builder, Vector3 worldPosition)
        => builder.Append(CultureInfo.InvariantCulture, $"({worldPosition.X:F1}, {worldPosition.Y:F1}, {worldPosition.Z:F1})");

    /// <inheritdoc />
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
        _backgroundTexture?.Dispose();
    }
}