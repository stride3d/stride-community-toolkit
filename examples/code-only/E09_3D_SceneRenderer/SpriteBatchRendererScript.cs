using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace E09_3D_SceneRenderer;

/// <summary>
/// A startup script that adds a renderer of its own to the compositor and draws one label over its entity
/// with a <see cref="SpriteBatch"/>, at the entity's position projected to the screen.
/// </summary>
public class SpriteBatchRendererScript : StartupScript
{
    private SpriteBatch? _spriteBatch;

    private SpriteFont? _font;

    private readonly float _fontSize = 12;

    private CameraComponent? _camera;

    private Vector2 _offset = new(0, -80);

    /// <summary>
    /// Creates the sprite batch, loads the font and adds a <see cref="DelegateSceneRenderer"/> to the compositor.
    /// </summary>
    public override void Start()
    {
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

        _font = Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

        _camera = SceneSystem.SceneInstance.RootScene.GetCamera();

        var sceneRenderer = new DelegateSceneRenderer(Draw);

        SceneSystem.GraphicsCompositor.AddSceneRenderer(sceneRenderer);
    }

    /// <summary>
    /// Draws the label at the entity's screen position.
    /// </summary>
    /// <param name="drawContext">The rendering context used to issue draw commands.</param>
    private void Draw(RenderDrawContext drawContext)
    {
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

        var screen = _camera.WorldToScreenPoint(ref worldPos, GraphicsDevice);

        _spriteBatch.Begin(drawContext.GraphicsContext);

        _spriteBatch.DrawString(_font, "Example 3: I am a capsule", _fontSize, screen + _offset, Color.Red);

        _spriteBatch.End();
    }
}