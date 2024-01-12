using Example04_MyraUI;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Engine;
using Stride.Games;

using var game = new Game();

// State flag to track health bar visibility
bool isHealthBarVisible = false;

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    SetupBase3DScene();
}

void Update(Scene rootScene, GameTime time)
{
    InitializeHealthBar();
}

void SetupBase3DScene()
{
    game.AddGraphicsCompositor()
        .AddCleanUIStage() //optional
        .AddSceneRenderer(new MyraSceneRenderer());
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();
    game.AddSkybox();
    game.Add3DGround();
}

/// <summary>
/// Initializes the health bar if it is not already visible.
/// </summary>
void InitializeHealthBar()
{
    if (isHealthBarVisible) return;

    var mainView = game.Services.GetService<MainView>();

    if (mainView == null) return;

    // Create and add a new health bar to the main view
    mainView.Widgets.Add(UIUtils.CreateHealthBar(-50, "#FFD961FF"));

    isHealthBarVisible = true;
}