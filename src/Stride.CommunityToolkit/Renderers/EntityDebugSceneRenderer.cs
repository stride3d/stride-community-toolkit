using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System.Globalization;
using System.Text;

namespace Stride.CommunityToolkit.Renderers;

/// <summary>
/// Scene renderer that draws per-entity debug information (e.g., entity name and/or position) as screen-space text.
/// </summary>
/// <remarks>
/// Uses 2D text rendering over the 3D scene via <see cref="SpriteBatch"/>. Appearance can be customized through
/// <see cref="EntityDebugSceneRendererOptions"/> (font size/color, background, offsets, etc.).
/// </remarks>
public class EntityDebugSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _backgroundTexture;
    private readonly StringBuilder _stringBuilder = new();
    private readonly Color4 _defaultBackground = new(0.9f, 0.9f, 0.9f, 0.01f);
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

    /// <summary>
    /// Initializes core resources needed by the renderer (font, sprite batch, background texture).
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        _backgroundTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [(Color)_defaultBackground]);

        var graphicsCompositor = Context.Tags.Get(GraphicsCompositor.Current);
        _camera = graphicsCompositor?.Cameras.Count > 0 ? graphicsCompositor.Cameras[0].Camera : null;
        _scene = SceneInstance.GetCurrent(Context)?.RootScene;
    }

    /// <summary>
    /// Draws the configured debug information for each entity in the current scene.
    /// </summary>
    /// <param name="context">Rendering context providing access to the compositor and scene.</param>
    /// <param name="drawContext">Draw context used to submit draw calls.</param>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        // Quick exit if there is nothing to render
        if (!_options.ShowEntityName && !_options.ShowEntityPosition)
        {
            return;
        }

        if (_spriteBatch is null || _camera is null || _scene is null)
        {
            return;
        }

        var entities = _scene.Entities;
        var count = entities.Count;

        if (count == 0) return;

        // ViewProjection transforms a world-space position into clip space (pre-perspective divide)
        var viewProjection = _camera.ViewProjectionMatrix;

        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        for (int i = 0; i < count; i++)
        {
            var entity = entities[i];
            var worldPos = entity.Transform.Position;

            // Transform to clip space
            var clipPosition = Vector4.Transform(new Vector4(worldPos, 1f), viewProjection);
            if (clipPosition.W <= 0f)
                continue; // behind the camera

            // Perspective divide -> Normalized Device Coordinates (NDC)
            var inverseW = 1f / clipPosition.W;
            var normalizedDeviceCoordinatesX = clipPosition.X * inverseW;
            var normalizedDeviceCoordinatesY = clipPosition.Y * inverseW;
            var normalizedDeviceCoordinatesZ = clipPosition.Z * inverseW;

            // Clip test in NDC: x and y in [-1,1], z in [0,1]
            var outsideNdc = normalizedDeviceCoordinatesZ < 0f || normalizedDeviceCoordinatesZ > 1f || normalizedDeviceCoordinatesX < -1f || normalizedDeviceCoordinatesX > 1f || normalizedDeviceCoordinatesY < -1f || normalizedDeviceCoordinatesY > 1f;

            // culled
            if (outsideNdc) continue;

            // Convert to screen-space using engine helper
            var screenPosition = _camera.WorldToScreenPoint(ref worldPos, GraphicsDevice);
            var finalPosition = screenPosition + _options.Offset;

            var textDisplay = GetDisplayText(entity);

            if (string.IsNullOrWhiteSpace(textDisplay)) continue;

            DrawTextBackground(textDisplay, finalPosition);

            _spriteBatch.DrawString(
                _font,
                textDisplay,
                _options.FontSize,
                finalPosition,
                _options.FontColor);
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Builds the debug text for a given entity based on the renderer's options.
    /// </summary>
    /// <param name="entity">The entity for which to generate debug text.</param>
    /// <returns>Debug text for the entity, or an empty string if nothing should be displayed.</returns>
    private string GetDisplayText(Entity entity)
    {
        _stringBuilder.Clear();

        if (_options.ShowEntityName)
        {
            _stringBuilder.Append(entity.Name);
        }

        if (_options.ShowEntityPosition)
        {
            if (_stringBuilder.Length > 0) _stringBuilder.Append(": ");

            var p = entity.Transform.Position;
            _stringBuilder.Append(CultureInfo.InvariantCulture, $"({p.X:F1}, {p.Y:F1}, {p.Z:F1})");
        }

        return _stringBuilder.ToString();
    }

    /// <summary>
    /// Draws a background rectangle behind the text to improve readability (when enabled).
    /// </summary>
    /// <param name="text">The text for which the background size is computed.</param>
    /// <param name="finalPosition">Screen-space position where the text is drawn.</param>
    private void DrawTextBackground(string text, Vector2 finalPosition)
    {
        if (!_options.EnableBackground)
            return;

        var textDimensions = _spriteBatch!.MeasureString(_font, text, _options.FontSize);

        var backgroundRectangle = new RectangleF(
            finalPosition.X - _options.Padding,
            finalPosition.Y - _options.Padding,
            textDimensions.X + _options.Padding * 2,
            textDimensions.Y + _options.Padding * 2);

        var bgColor = _options.BackgroundColor ?? _defaultBackground;
        if (bgColor.A <= 0f)
            return; // fully transparent, skip draw

        _spriteBatch.Draw(_backgroundTexture, backgroundRectangle, bgColor);
    }

    /// <summary>
    /// Disposes resources used by the renderer.
    /// </summary>
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
        _backgroundTexture?.Dispose();
    }
}