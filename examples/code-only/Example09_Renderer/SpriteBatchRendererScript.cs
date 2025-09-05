using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Example09_Renderer;

/// <summary>
/// A startup script that demonstrates adding a custom scene renderer to the Stride engine's rendering pipeline.
/// This script uses <see cref="SpriteBatch"/> to render 2D text on the screen.
/// </summary>
/// <remarks>
/// This example showcases how to create a custom renderer and add it through the <see cref="StartupScript"/> mechanism.
/// It demonstrates rendering 2D text based on the world position of an entity, converting it to screen space using the camera's view.
/// </remarks>
public class SpriteBatchRendererScript : StartupScript
{
    // Instance of SpriteBatch used to render 2D elements
    private SpriteBatch? _spriteBatch;

    // Font used for rendering text
    private SpriteFont? _font;

    // Font size for the text
    private float _fontSize = 12;

    // Reference to the camera component used for world-to-screen transformation
    private CameraComponent? _camera;

    // Offset to position the text relative to the world position
    private Vector2 _offset = new(0, -80);

    /// <summary>
    /// Initializes the scene renderer by creating a <see cref="DelegateSceneRenderer"/> that renders custom 2D text using <see cref="SpriteBatch"/>.
    /// This method is called when the script starts.
    /// </summary>
    public override void Start()
    {
        // Initialize the SpriteBatch for drawing 2D content
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

        // Load the default font used for rendering text
        _font = Content.Load<SpriteFont>("StrideDefaultFont");

        // Get the camera from the root scene to calculate world-to-screen coordinates
        _camera = SceneSystem.SceneInstance.RootScene.GetCamera();

        // Create a custom scene renderer using a delegate for the Draw method
        var sceneRenderer = new DelegateSceneRenderer(Draw);

        // Add the custom scene renderer to the graphics compositor, which will manage its rendering
        SceneSystem.GraphicsCompositor.AddSceneRenderer(sceneRenderer);
    }

    /// <summary>
    /// Custom draw method that renders 2D text on the screen using the world position of the entity.
    /// This method is called during the rendering phase.
    /// </summary>
    /// <param name="drawContext">The rendering context used to issue draw commands.</param>
    private void Draw(RenderDrawContext drawContext)
    {
        // Ensure the necessary components (SpriteBatch, font, and camera) are initialized
        if (_spriteBatch == null || _font == null || _camera == null) return;

        // ViewProjection transforms a world-space position into clip space (pre-perspective divide)
        var viewProjection = _camera.ViewProjectionMatrix;

        var worldPos = Entity.Transform.Position;

        // Transform to clip space
        var clipPosition = Vector4.Transform(new Vector4(worldPos, 1f), viewProjection);
        if (clipPosition.W <= 0f)
            return; // behind the camera

        // Perspective divide -> Normalized Device Coordinates (NDC)
        var inverseW = 1f / clipPosition.W;
        var normalizedDeviceCoordinatesX = clipPosition.X * inverseW;
        var normalizedDeviceCoordinatesY = clipPosition.Y * inverseW;
        var normalizedDeviceCoordinatesZ = clipPosition.Z * inverseW;

        // Clip test in NDC: x and y in [-1,1], z in [0,1]
        var outsideNdc = normalizedDeviceCoordinatesZ < 0f || normalizedDeviceCoordinatesZ > 1f || normalizedDeviceCoordinatesX < -1f || normalizedDeviceCoordinatesX > 1f || normalizedDeviceCoordinatesY < -1f || normalizedDeviceCoordinatesY > 1f;

        // culled
        if (outsideNdc) return;

        // Convert the world position of the entity to screen space coordinates
        var screen = _camera.WorldToScreenPoint(ref worldPos, GraphicsDevice);

        // Begin the 2D rendering process using SpriteBatch
        _spriteBatch.Begin(drawContext.GraphicsContext);

        // Draw a red "I am a capsule" text at the specified screen position, applying the offset
        _spriteBatch.DrawString(_font, "Example 3: I am a capsule", _fontSize, screen + _offset, Color.Red);

        // End the SpriteBatch rendering process
        _spriteBatch.End();
    }
}