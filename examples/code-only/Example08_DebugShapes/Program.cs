using Example08_DebugShapes.Scripts;
using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    AddDebugComponent(rootScene);
    SetupBaseScene();
}

void SetupBaseScene()
{
    game.AddGraphicsCompositor();
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.AddSkybox();
    game.Add3DGround();
    game.AddDebugShapes();
}

void AddDebugComponent(Scene scene)
{
    var entity = new Entity("Debug Shapes")
    {
        new ShapeUpdater()
    };

    scene.Entities.Add(entity);
}