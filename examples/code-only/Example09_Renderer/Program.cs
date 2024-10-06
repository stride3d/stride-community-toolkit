using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddProfiler();

    game.AddSceneRenderer(new MyCustomSceneRenderer());

    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;

    //entity.Add(new SpriteBatchRenderer());
});

public class MyCustomSceneRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Scene? _scene;
    private CameraComponent? _camera;
    private Texture? _colorTexture = null;
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
            //var screen = entity.Transform.Position;

            var dim = _spriteBatch.MeasureString(_font, text, 12);

            _spriteBatch.Draw(_colorTexture, new Rectangle(
                (int)screen.X + (int)_offset.X,
                (int)screen.Y + (int)_offset.Y,
                (int)dim.X,
                (int)dim.Y), new Color(200, 200, 200, 100));

            _spriteBatch.DrawString(_font, text, 12, screen + _offset, Color.Black);
        }

        _spriteBatch.End();
    }
}

public class SpriteBatchRenderer : SyncScript
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;
    private Texture? _texture;
    private float _fontSize = 25;
    private string _text = "This text is in Arial 20 with anti-alias\nand multiline...";
    private DelegateSceneRenderer? _sceneRenderer;
    private CameraComponent? _camera = null;

    public override void Start()
    {
        _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        //_ctx = new RenderDrawContext(Services, RenderContext.GetShared(Services), Game.GraphicsContext);
        _sceneRenderer = new DelegateSceneRenderer(Draw);
        _camera = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

        var renderCollection = (SceneRendererCollection)SceneSystem.GraphicsCompositor.Game;

        renderCollection.Add(_sceneRenderer);

        //_texture = Content.Load<Texture>("Path to your texture asset");
    }

    public override void Update()
    {
        //_sceneRenderer.Draw(_ctx);

        //var spriteBatch = new SpriteBatch(GraphicsDevice);

        //// don't forget the begin
        //spriteBatch.Begin(Game.GraphicsContext);

        //// draw the text "Helloworld!" in red from the center of the screen
        //spriteBatch.DrawString(_font, "Hello World!", new Vector2(200, 200), Color.Red);

        //// don't forget the end
        //spriteBatch.End();


        //_spriteBatch.Begin(Game.GraphicsContext);

        //_spriteBatch.DrawString(_font, _text, new Vector2(300, 300), Color.White);

        //_spriteBatch.End();
    }

    private void Draw(RenderDrawContext drawContext)
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);

        var screen = _camera.WorldToScreenPoint(ref Entity.Transform.Position, GraphicsDevice);

        spriteBatch.Begin(drawContext.GraphicsContext);
        spriteBatch.DrawString(_font, "Hello World 2", 20, screen + new Vector2(0, -50), Color.Red);
        //spriteBatch.DrawString(_font, "Hello World 2", screen + new Vector2(0, -50), Color.Red, rotation: 0.5f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f, TextAlignment.Left);
        spriteBatch.End();
    }
}