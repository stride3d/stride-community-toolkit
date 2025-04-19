using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// A custom scene renderer that renders 2D text and a background for each entity in the scene.
/// This class demonstrates how to use <see cref="SpriteBatch"/> for 2D rendering within a 3D scene.
/// </summary>
/// <remarks>
/// This renderer uses <see cref="SceneRendererBase"/> to hook into the Stride rendering pipeline, rendering text above each entity
/// with details like the entity's name and position. The renderer demonstrates both dynamic text rendering.
/// This is useful for debugging or displaying information in a game scene.
/// The text change isn't supported at the moment. Use it just for static text.
/// </remarks>
public class EntityTextRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _backgroundTexture;
    private readonly Color4 _defaultBackground = new(0.9f, 0.9f, 0.9f, 0.01f);
    private readonly Dictionary<Entity, Vector2> _metricsCache = [];

    /// <summary>
    /// Initializes the renderer by loading necessary resources like fonts and creating a <see cref="SpriteBatch"/> for rendering 2D elements.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Create a SpriteBatch instance for rendering 2D content
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load the default font used for rendering text
        _font = Content.Load<SpriteFont>("StrideDefaultFont");

        // Create a small texture (1x1 pixel) for drawing the background behind the text
        _backgroundTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [(Color)_defaultBackground]);
    }

    /// <summary>
    /// Performs the actual drawing of the scene, including rendering text and backgrounds for each entity.
    /// This method is called every frame.
    /// </summary>
    /// <param name="context">The rendering context that contains information about the current frame.</param>
    /// <param name="drawContext">The draw context used for issuing draw commands to the graphics card.</param>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        // Get the active GraphicsCompositor (handles camera and rendering order)
        var graphicsCompositor = context.Tags.Get(GraphicsCompositor.Current);

        if (graphicsCompositor is null) return;

        // Get the first camera in the graphics compositor (typically the main camera)
        _camera ??= graphicsCompositor.Cameras[0].Camera;

        // Get the root scene for the current frame
        _scene ??= SceneInstance.GetCurrent(context).RootScene;

        // Ensure all required components are initialized
        if (_spriteBatch is null || _camera is null || _scene is null) return;

        // Begin the SpriteBatch for rendering
        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        foreach (var entity in _scene.Entities)
        {
            var textDisplay = entity.Get<EntityTextComponent>();

            if (textDisplay == null) continue;

            // Convert the entity's world position to screen space
            var screenPosition = _camera.WorldToScreenPoint(ref entity.Transform.Position, GraphicsDevice);
            var finalPosition = screenPosition + textDisplay.Offset;

            DrawTextBackground(entity, textDisplay, finalPosition);

            _spriteBatch.DrawString(
                _font,
                textDisplay.Text,
                textDisplay.FontSize,
                finalPosition,
                textDisplay.TextColor,
                textDisplay.Alignment
            );
        }

        _spriteBatch.End();
    }

    private void DrawTextBackground(Entity entity, EntityTextComponent textDisplay, Vector2 finalPosition)
    {
        if (!textDisplay.EnableBackground) return;

        if (_metricsCache.TryGetValue(entity, out var textDimensions))
        {
            var backgroundRectangle = new RectangleF(
                finalPosition.X - textDisplay.Padding,
                finalPosition.Y - textDisplay.Padding,
                textDimensions.X + textDisplay.Padding * 2,
                textDimensions.Y + textDisplay.Padding * 2);

            _spriteBatch!.Draw(_backgroundTexture, backgroundRectangle, textDisplay.BackgroundColor ?? _defaultBackground);
        }
        else
        {
            _metricsCache[entity] = _spriteBatch!.MeasureString(_font, textDisplay.Text, textDisplay.FontSize);
        }
    }

    /// <summary>
    /// Cleans up resources used by the renderer, such as the sprite batch and background texture.
    /// </summary>
    protected override void Destroy()
    {
        base.Destroy();

        _spriteBatch?.Dispose();
        _backgroundTexture?.Dispose();
    }
}