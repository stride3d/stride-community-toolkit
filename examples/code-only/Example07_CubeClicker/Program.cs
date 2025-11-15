using Example07_CubeClicker.Managers;
using Example07_CubeClicker.Scripts;
using NexVYaml;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Engine;
using Stride.Graphics;

// This example demonstrates how to load and save game data. When the game starts,
// it automatically loads the click data and cube positions from the previous session.
// The player interacts with dynamically generated cubes, with the game tracking left
// and right mouse clicks.
// In case of a corrupted Yaml file, navigate to the \bin\Debug\net10.0\data\
// directory and delete the file manually.

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera().Add3DCameraController(displayPosition: DisplayPosition.BottomRight);
    game.AddDirectionalLight();
    game.Add3DGround();
    game.AddSkybox();
    game.AddGroundGizmo(showAxisName: true);

    CreateAndRegisterGameManagerUI(rootScene);
}

void CreateAndRegisterGameManagerUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    var gameManager = new GameManager(font);
    game.Services.AddService(gameManager);

    var uiEntity = gameManager.CreateUI();
    uiEntity.Add(new ClickHandlerComponent());
    uiEntity.Scene = rootScene;
}