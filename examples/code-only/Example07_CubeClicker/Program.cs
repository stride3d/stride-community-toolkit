using Example07_CubeClicker;
using Example07_CubeClicker.Scripts;
using NexVYaml;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

// This example demonstrates how to load and save game data, specifically tracking left and right mouse clicks on dynamically generated cubes.
// Upon launch, the game automatically loads data from the previous session.
// In case of a corrupted Yaml file, navigate to the \bin\Debug\net8.0\data\ directory and delete the file manually.

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

var dataSaver = new DataSaver<UiData>()
{
    // The default if loading fails so we don't have to deal with null
    Data = UiData.Default
};

// Load the data from the previous run if possible.
await dataSaver.TryLoadAsync(GameUI.ClickDataFileName);

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    CreateAndRegisterGameUI(rootScene);

    AddFirstCube(rootScene);
}

async void CreateAndRegisterGameUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    var cubeCollector = new CubeCollector();
    var gameUI = new GameUI(font, dataSaver, cubeCollector);
    game.Services.AddService(gameUI);

    var uiEntity = gameUI.Create();
    uiEntity.Add(new ClickHandlerComponent());
    uiEntity.Add(cubeCollector);
    uiEntity.Scene = rootScene;
    await cubeCollector.LoadCubeDataAsync();
}

void AddFirstCube(Scene rootScene)
{
    var entity = game.CreatePrimitive(PrimitiveModelType.Cube, "Cube");
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}