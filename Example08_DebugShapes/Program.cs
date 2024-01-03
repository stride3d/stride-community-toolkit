using Example08_DebugShapes.Scripts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    AddDebugComponent(rootScene);
    SetupBaseScene();
}

void Update(Scene scene, GameTime time)
{

}

void SetupBaseScene()
{
    game.AddGraphicsCompositor();
    game.Add3DCamera().AddInteractiveCameraScript();
    game.AddDirectionalLight();
    game.AddSkybox();
    game.Add3DGround();
}

void AddDebugComponent(Scene scene)
{
    var entity = new Entity("Debug Shapes")
    {
        new ShapeUpdater()
    };
    scene.Entities.Add(entity);
}