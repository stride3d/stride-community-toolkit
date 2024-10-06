using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Example09_Renderer;

/// <summary>
/// A custom scene renderer that renders 2D text and a background for each entity in the scene.
/// This class demonstrates how to use <see cref="SpriteBatch"/> for 2D rendering within a 3D scene.
/// </summary>
/// <remarks>
/// This renderer uses <see cref="SceneRendererBase"/> to hook into the Stride rendering pipeline, rendering text above each entity
/// with details like the entity's name and position. The renderer demonstrates both static and dynamic text rendering.
/// </remarks>
public class MyCustomSceneRenderer : SceneRendererBase
{
    // Instance of SpriteBatch used to render 2D elements like text
    private SpriteBatch? _spriteBatch;

    // Font used for rendering text
    private SpriteFont? _font;

    // Font size for the text rendered on the screen
    private float _fontSize = 12;

    // The current scene being rendered
    private Scene? _scene;

    // The camera used to convert world positions to screen positions
    private CameraComponent? _camera;

    // A texture used to draw the background behind the text
    private Texture? _colorTexture;

    // Offset for positioning the text relative to the entity's screen position
    private Vector2 _offset = new(0, -50);

    /// <summary>
    /// Initializes the renderer by loading necessary resources like fonts and creating a <see cref="SpriteBatch"/> for rendering 2D elements.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        // Load the default font used for rendering text
        _font = Content.Load<SpriteFont>("StrideDefaultFont");

        // Create a SpriteBatch instance for rendering 2D content
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Create a small texture (1x1 pixel) for drawing the background behind the text
        _colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });
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
        _scene ??= context.Tags.Get(SceneInstance.Current).RootScene;

        // Ensure all required components are initialized
        if (_spriteBatch is null || _camera is null || _scene is null) return;

        // Begin the SpriteBatch for rendering static 2D text
        _spriteBatch.Begin(drawContext.GraphicsContext);

        // Example of static text rendering (does not depend on entities)
        _spriteBatch.DrawString(_font, "Hello Stride", 20, new Vector2(100, 100), Color.White);

        _spriteBatch.End();

        // Begin a new SpriteBatch for rendering dynamic entity information
        _spriteBatch.Begin(drawContext.GraphicsContext);

        foreach (var entity in _scene.Entities)
        {
            // Convert the entity's world position to screen space
            var screen = _camera.WorldToScreenPoint(ref entity.Transform.Position, GraphicsDevice);

            // Text to display the entity's name and position
            var text = $"{entity.Name}: {entity.Transform.Position:N1}";

            // Measure the dimensions of the text to calculate background size
            var textDimensions = _spriteBatch.MeasureString(_font, text, _fontSize);

            // Draw a semi-transparent background behind the text
            _spriteBatch.Draw(_colorTexture, new Rectangle(
                (int)screen.X + (int)_offset.X,
                (int)screen.Y + (int)_offset.Y,
                (int)textDimensions.X,
                (int)textDimensions.Y), new Color(200, 200, 200, 100));

            // Draw the entity's name and position as text on the screen
            _spriteBatch.DrawString(_font, text, _fontSize, screen + _offset, Color.Black);
        }

        // End the SpriteBatch
        _spriteBatch.End();
    }
}