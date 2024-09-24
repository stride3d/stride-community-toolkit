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
    game.AddGraphicsCompositor().AddCleanUIStage().AddSceneRenderer(new MyCustomRenderer());
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.Add3DGround();

    game.AddSkybox();

    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;

    entity.Add(new SpriteBatchRenderer());
});

public class MyCustomRenderer : SceneRendererBase
{
    private SpriteBatch? _spriteBatch;
    private SpriteFont? _font;

    protected override void InitializeCore()
    {
        base.InitializeCore();

        _font = Content.Load<SpriteFont>("StrideDefaultFont");
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        // Access to the graphics device
        //var graphicsDevice = drawContext.GraphicsDevice;
        //var commandList = drawContext.CommandList;
        // Clears the current render target
        //commandList.Clear(commandList.RenderTargets[0], Color.CornflowerBlue);

        //_spriteBatch = new SpriteBatch(graphicsDevice);
        _spriteBatch.Begin(drawContext.GraphicsContext);
        _spriteBatch.DrawString(_font, "Hello Stride 1.2", 20, new Vector2(100, 100), Color.White);
        //_spriteBatch.Draw(_texture, Vector2.Zero);
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
        spriteBatch.End();
    }
}
