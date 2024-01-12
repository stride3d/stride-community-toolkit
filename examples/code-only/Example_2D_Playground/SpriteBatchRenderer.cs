using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Example_2D_Playground;

public class SpriteBatchRenderer : SyncScript
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture? _texture;
    private float _fontSize = 25;
    private string _text = "This text is in Arial 20 with anti-alias\nand multiline...";
    private DelegateSceneRenderer _sceneRenderer;
    private RenderDrawContext _ctx;

    public override void Start()
    {
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        _sceneRenderer = new DelegateSceneRenderer(Draw);
        _ctx = new RenderDrawContext(Services, RenderContext.GetShared(Services), Game.GraphicsContext);

        //_texture = Content.Load<Texture>("Path to your texture asset");
    }

    public override void Update()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

        // don't forget the begin
        spriteBatch.Begin(Game.GraphicsContext);

        // draw the text "Helloworld!" in red from the center of the screen
        spriteBatch.DrawString(_font, "Helloworld!", new Vector2(0.5f, 0.5f), Color.Red);

        // don't forget the end
        spriteBatch.End();

        _sceneRenderer.Draw(_ctx);

        var _cameraComponent = Entity.Scene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();
        //var text = "Your Text Here";
        var viewMatrix = _cameraComponent.ViewMatrix;
        var projectionMatrix = _cameraComponent.ProjectionMatrix;
        var textureToWorldSpace = Matrix.RotationX(MathUtil.Pi) * Matrix.Translation(0, 0, 0.25f);
        var textScale = 1.0f;
        var colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

        //_spriteBatch.Begin(
        //    Game.GraphicsContext,
        //    textureToWorldSpace * viewMatrix,
        //    projectionMatrix,
        //    SpriteSortMode.BackToFront,
        //    BlendStates.AlphaBlend,
        //    GraphicsDevice.SamplerStates.LinearClamp,
        //    DepthStencilStates.None);

        _spriteBatch.Begin(Game.GraphicsContext);

        var dim = _font.MeasureString(_text);

        int x = 20, y = 20;

        _spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Green);
        _font.PreGenerateGlyphs(_text, _font.Size * Vector2.One);
        _spriteBatch.DrawString(_font, _text, new Vector2(x, y), Color.White);

        //var textSize = _spriteBatch.MeasureString(_font, text);

        //_spriteBatch.DrawString(_font, text, Vector2.One * 0.5f, Color.White, 0, textSize / 2, Vector2.One / _fontSize * textScale, SpriteEffects.None, 0, TextAlignment.Center);

        _spriteBatch.End();
    }

    private void Draw(RenderDrawContext ctx)
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

        // don't forget the begin
        spriteBatch.Begin(Game.GraphicsContext);

        // draw the text "Helloworld!" in red from the center of the screen
        spriteBatch.DrawString(_font, "Helloworld!", new Vector2(0.5f, 0.5f), Color.Red);

        // don't forget the end
        spriteBatch.End();
    }
}