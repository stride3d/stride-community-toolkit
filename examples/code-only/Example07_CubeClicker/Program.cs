using Example07_CubeClicker;
using NexVYaml;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

// Keybindings
// S : Save Data to file in %APPDATA% , and one folder higher, not in roaming
// L : Loading the Data from the Path
// D : Delete the stored data
// The Game Automatically loads the data from the previous run on launch
// If a corrupted Yaml exists, go to the %APPDATA% path and manually delete the file
// it doesn't matter if you click on the cube or not, it's just a regular cube..

using var game = new Game();

// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

DataSaver<UiData> dataSaver = new()
{
    // The default if loading fails so we don't have to deal with null
    Data = UiData.Default
};

// Load the data from the previous run if possible.
dataSaver.Data = await dataSaver.TryLoadAsync();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddGameUI(rootScene);

    AddCube(rootScene);
}

void AddGameUI(Scene rootScene)
{
    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");
    var entity = new GameUI(font, dataSaver).Create();

    entity.Scene = rootScene;
}

void AddCube(Scene rootScene)
{
    var clickHandler = new ClickHandlerComponent()
    {
        DataSaver = dataSaver
    };

    var entity = game.CreatePrimitive(PrimitiveModelType.Cube);
    entity.Add(clickHandler);
    entity.Transform.Position = new Vector3(0, 8, 0);
    entity.Scene = rootScene;
}