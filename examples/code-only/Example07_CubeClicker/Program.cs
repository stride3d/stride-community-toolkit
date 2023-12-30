using Example07_CubeClicker;
using Example07_CubeClicker.Scripts;
using NexVYaml;
using Stride.CommunityToolkit.Engine;
using Stride.Engine;
using Stride.Graphics;

// This example demonstrates how to load and save game data, specifically tracking left and right mouse clicks on dynamically generated cubes.
// Upon launch, the game automatically loads data from the previous session.
// In case of a corrupted Yaml file, navigate to the \bin\Debug\net8.0\data\ directory and delete the file manually.

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    CreateAndRegisterGameManagerUI(rootScene);
}

void CreateAndRegisterGameManagerUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    //var cubeCollector = new CubeCollector();
    var gameUI = new GameUIManager(font);
    game.Services.AddService(gameUI);

    var uiEntity = gameUI.Create();
    uiEntity.Add(new ClickHandlerComponent());
    //uiEntity.Add(cubeCollector);
    uiEntity.Scene = rootScene;

    //await cubeCollector.LoadCubeDataAsync();
}