using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace E09_3D_SceneRenderer;

/// <summary>
/// A scene renderer that draws a label over every entity with a <see cref="SpriteBatch"/>: the name and
/// the position on a dark background, plus one static line. <see cref="SceneRendererBase"/> is the hook
/// into the rendering pipeline.
/// </summary>
public class MyCustomSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;

    private SpriteFont? _font;

    private const float FontSize = 12;

    private Scene? _scene;

    private CameraComponent? _camera;

    private Texture? _colorTexture;

    private readonly Color4 _defaultBackground = new(0.9f, 0.9f, 0.9f, 0.01f);

    private Vector2 _offset = new(0, -50);

    /// <summary>
    /// Creates the sprite batch, loads the font and makes the background texture.
    /// </summary>
    protected override void InitializeCore()
    {
        base.InitializeCore();

        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font = Content.Load<SpriteFont>("/Stride.Engine/StrideDefaultFont");

        // One pixel, stretched behind the text
        _colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [(Color)_defaultBackground]);
    }

    /// <summary>
    /// Releases what <see cref="InitializeCore"/> created. The font came from the content manager,
    /// which owns it; the sprite batch and the texture are this renderer's own.
    /// </summary>
    protected override void Destroy()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _colorTexture?.Dispose();
        _colorTexture = null;

        base.Destroy();
    }

    /// <summary>
    /// Draws the labels. Called every frame.
    /// </summary>
    /// <param name="context">The rendering context that contains information about the current frame.</param>
    /// <param name="drawContext">The draw context used for issuing draw commands to the graphics card.</param>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        var graphicsCompositor = context.Tags.Get(GraphicsCompositor.Current);

        if (graphicsCompositor is null) return;

        _camera ??= graphicsCompositor.Cameras[0].Camera;

        _scene ??= SceneInstance.GetCurrent(context).RootScene;

        if (_spriteBatch is null || _camera is null || _scene is null) return;

        _spriteBatch.Begin(drawContext.GraphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        // Static text, the same every frame
        _spriteBatch.DrawString(_font, "Example 1", 20, new Vector2(100, 100), _defaultBackground);

        _spriteBatch.End();

        _spriteBatch.Begin(drawContext.GraphicsContext);

        foreach (var entity in _scene.Entities)
        {
            var worldPos = entity.Transform.Position;

            // Only what is in front of the camera and on screen gets a label
            if (!_camera.TryWorldToScreenPoint(worldPos, GraphicsDevice, out var screenPosition)) continue;

            var finalPosition = screenPosition + _offset;

            var text = $"{entity.Name}: {worldPos:N1}";

            var textDimensions = _spriteBatch.MeasureString(_font, text, FontSize);

            _spriteBatch.Draw(_colorTexture, new Rectangle((int)finalPosition.X - 2, (int)finalPosition.Y - 2, (int)textDimensions.X + 4, (int)textDimensions.Y + 4), _defaultBackground);
            _spriteBatch.DrawString(_font, text, FontSize, finalPosition, Color.Black);
        }

        _spriteBatch.End();
    }
}