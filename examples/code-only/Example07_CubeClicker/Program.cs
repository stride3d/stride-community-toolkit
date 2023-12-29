using Example07_CubeClicker;
using NexVYaml;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

// The Game automatically loads the data from the previous run on launch
// If a corrupted Yaml exists, go to \bin\Debug\net8.0\data\ path and manually delete the file

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

var dataSaver = new DataSaver<UiData>()
{
    // The default if loading fails so we don't have to deal with null
    Data = UiData.Default
};

// Load the data from the previous run if possible.
await dataSaver.TryLoadAsync();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    CreateAndRegisterGameUI(rootScene);

    AddFirstCube(rootScene);
}

void CreateAndRegisterGameUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    var gameUI = new GameUI(font, dataSaver);

    var uiEntity = gameUI.Create();
    uiEntity.Add(new ClickHandlerComponent());
    uiEntity.Scene = rootScene;

    game.Services.AddService(gameUI);
}

void AddFirstCube(Scene rootScene)
{
    var entity = game.CreatePrimitive(PrimitiveModelType.Cube);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}