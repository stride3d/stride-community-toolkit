using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Compositing;

public class EntityDebugRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _colorTexture;
    private readonly EntityDebugRendererOptions _options;

    public EntityDebugRenderer(EntityDebugRendererOptions? options = null)
    {
        _options = options ?? new();
    }

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
            var screen = _camera.WorldToScreenPoint(ref entity.Transform.Position, GraphicsDevice);

            string text = string.Empty;

            if (_options.ShowEntityName)
            {
                text += entity.Name;
            }

            if (_options.ShowEntityPosition)
            {
                text += $": {entity.Transform.Position:N1}";
            }

            if (string.IsNullOrWhiteSpace(text)) continue;

            ShowBackground(screen, text);

            _spriteBatch.DrawString(
                _font,
                text,
                _options.FontSize,
                screen + _options.Offset,
                _options.FontColor);
        }

        _spriteBatch.End();
    }

    private void ShowBackground(Vector2 screen, string text)
    {
        if (!_options.ShowFontBackground) return;

        var dim = _spriteBatch!.MeasureString(_font, text, _options.FontSize);

        _spriteBatch.Draw(_colorTexture, new Rectangle(
            (int)screen.X + (int)_options.Offset.X,
            (int)screen.Y + (int)_options.Offset.Y,
            (int)dim.X,
            (int)dim.Y), new Color(200, 200, 200, 100));

    }
}