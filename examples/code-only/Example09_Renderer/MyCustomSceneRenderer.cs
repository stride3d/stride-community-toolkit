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

    private readonly Color4 _defaultBackground = new(0.9f, 0.9f, 0.9f, 0.01f);

    // Offset for positioning the text relative to the entity's screen position
    private Vector2 _offset = new(0, -50);

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
        _colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [(Color)_defaultBackground]);
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

        // Begin the SpriteBatch for rendering static 2D text
        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        // Example of static text rendering (does not depend on entities)
        _spriteBatch.DrawString(_font, "Example 1", 20, new Vector2(100, 100), _defaultBackground);

        _spriteBatch.End();

        var viewProjection = _camera.ViewProjectionMatrix;

        // Begin a new SpriteBatch for rendering dynamic entity information
        _spriteBatch.Begin(drawContext.GraphicsContext);

        foreach (var entity in _scene.Entities)
        {
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

            // Convert the entity's world position to screen space
            var screenPosition = _camera.WorldToScreenPoint(ref worldPos, GraphicsDevice);
            var finalPosition = screenPosition + _offset;

            // Text to display the entity's name and position
            var text = $"{entity.Name}: {worldPos:N1}";

            // Measure the dimensions of the text to calculate background size
            var textDimensions = _spriteBatch.MeasureString(_font, text, _fontSize);

            // Draw a semi-transparent background behind the text
            _spriteBatch.Draw(_colorTexture, new Rectangle(
                (int)finalPosition.X - 2,
                (int)finalPosition.Y - 2,
                (int)textDimensions.X + 4,
                (int)textDimensions.Y + 4), _defaultBackground);

            // Draw the entity's name and position as text on the screen
            _spriteBatch.DrawString(_font, text, _fontSize, finalPosition, Color.Black);
        }

        // End the SpriteBatch
        _spriteBatch.End();
    }
}