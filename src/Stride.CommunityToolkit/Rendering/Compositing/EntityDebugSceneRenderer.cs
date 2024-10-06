using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System.Text;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// Renders debug information (such as entity names and positions) for all entities in a scene.
/// </summary>
/// <remarks>
/// This class is designed to display debug information such as entity names and positions on the screen
/// using 2D text rendering over the 3D scene. It also allows optional customization, such as font size, color, and background.
/// </remarks>
public class EntityDebugSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _colorTexture;
    private readonly EntityDebugSceneRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDebugSceneRenderer"/> class with default rendering options.
    /// </summary>
    public EntityDebugSceneRenderer() => _options = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDebugSceneRenderer"/> class with the specified rendering options.
    /// </summary>
    /// <param name="options">The options to customize the appearance of the debug text. If null, default options are used.</param>
    public EntityDebugSceneRenderer(EntityDebugSceneRendererOptions? options = null) => _options = options ?? new();

    /// <summary>
    /// Initializes core resources needed by the renderer, such as the font and sprite batch.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });
    }

    /// <summary>
    /// Draws debug information (such as entity names and positions) for all entities in the current scene.
    /// </summary>
    /// <param name="context">The current rendering context, which provides information such as the scene and camera.</param>
    /// <param name="drawContext">The context used to draw graphical elements.</param>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        var graphicsCompositor = context.Tags.Get(GraphicsCompositor.Current);

        if (graphicsCompositor is null) return;

        if (_camera is null)
        {
            _camera = graphicsCompositor.Cameras[0].Camera;
        }

        //_camera ??= graphicsCompositor.Cameras[0].Camera;

        if (_scene is null)
        {
            _scene = context.Tags.Get(SceneInstance.Current).RootScene;
        }

        //_scene ??= context.Tags.Get(SceneInstance.Current).RootScene;

        if (_spriteBatch is null || _camera is null || _scene is null) return;

        _spriteBatch.Begin(drawContext.GraphicsContext);

        foreach (var entity in _scene.Entities)
        {
            var screenPosition = _camera.WorldToScreenPoint(ref entity.Transform.Position, GraphicsDevice);

            var debugText = BuildDebugText(entity);

            if (string.IsNullOrWhiteSpace(debugText)) continue;

            DrawBackground(screenPosition, debugText);

            _spriteBatch.DrawString(
                _font,
                debugText,
                _options.FontSize,
                screenPosition + _options.Offset,
                _options.FontColor);
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Builds the debug text for a given entity based on the renderer's options.
    /// </summary>
    /// <param name="entity">The entity for which to generate the debug text.</param>
    /// <returns>The debug text to be rendered for the entity, or an empty string if no text should be shown.</returns>
    private string BuildDebugText(Entity entity)
    {
        var stringBuilder = new StringBuilder();

        if (_options.ShowEntityName)
        {
            stringBuilder.Append(entity.Name);
        }

        if (_options.ShowEntityPosition)
        {
            if (stringBuilder.Length > 0) stringBuilder.Append(": ");

            stringBuilder.AppendFormat("{0:N1}", entity.Transform.Position);
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Draws a background rectangle behind the debug text to make it more readable.
    /// </summary>
    /// <param name="screenPosition">The screen position where the debug text will be drawn.</param>
    /// <param name="text">The text for which the background will be rendered.</param>
    private void DrawBackground(Vector2 screenPosition, string text)
    {
        if (!_options.ShowFontBackground) return;

        var textDimensions = _spriteBatch!.MeasureString(_font, text, _options.FontSize);

        var backgroundRectangle = new Rectangle(
           (int)(screenPosition.X + _options.Offset.X),
           (int)(screenPosition.Y + _options.Offset.Y),
           (int)textDimensions.X,
           (int)textDimensions.Y);

        _spriteBatch.Draw(_colorTexture, backgroundRectangle, new Color(200, 200, 200, 100));
    }
}