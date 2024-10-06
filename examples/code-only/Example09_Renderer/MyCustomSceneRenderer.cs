using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Example09_Renderer;

public class MyCustomSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;

    // Font size for the text
    private float _fontSize = 12;

    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _colorTexture;
    private Vector2 _offset = new(0, -50);

    protected override void InitializeCore()
    {
        base.InitializeCore();

        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        var graphicsCompositor = context.Tags.Get(GraphicsCompositor.Current);

        if (graphicsCompositor is null) return;

        _camera ??= graphicsCompositor.Cameras[0].Camera;

        _scene ??= context.Tags.Get(SceneInstance.Current).RootScene;

        if (_spriteBatch is null || _camera is null || _scene is null) return;

        _spriteBatch.Begin(drawContext.GraphicsContext);
        _spriteBatch.DrawString(_font, "Hello Stride", 20, new Vector2(100, 100), Color.White);
        _spriteBatch.End();

        _spriteBatch.Begin(drawContext.GraphicsContext);

        foreach (var entity in _scene.Entities)
        {
            var screen = _camera.WorldToScreenPoint(ref entity.Transform.Position, GraphicsDevice);

            var text = $"{entity.Name}: {entity.Transform.Position:N1}";

            var textDimensions = _spriteBatch.MeasureString(_font, text, _fontSize);

            _spriteBatch.Draw(_colorTexture, new Rectangle(
                (int)screen.X + (int)_offset.X,
                (int)screen.Y + (int)_offset.Y,
                (int)textDimensions.X,
                (int)textDimensions.Y), new Color(200, 200, 200, 100));

            _spriteBatch.DrawString(_font, text, _fontSize, screen + _offset, Color.Black);
        }

        _spriteBatch.End();
    }
}