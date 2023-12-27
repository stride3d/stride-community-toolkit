using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Example06_SaveTheCube;
using Example06_CubeClicker;
using NexVYaml;


/// Keybindings
/// S : Save Data to file in %APPDATA% , and one folder higher, not in roaming
/// L : Loading the Data from the Path
/// D : Delete the stored data
/// The Game Automatically loads the data from the previous run on launch

using var game = new Game();
// Register all DataContracted Types
NexYamlSerializerRegistry.Init();

DataSaver<UiData> cubeSaver = new()
{
    // The default if loading fails so we don't have to deal with null
    Data = UiData.Default
};
// load the data from the previous run if possible.
cubeSaver.Data = cubeSaver.TryLoad().Result;

var clickHandler = new ClickHandlerComponent()
{
    DataSaver = cubeSaver
};
game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddCube(rootScene);
}

void AddCube(Scene rootScene)
{
    var entity = game.CreatePrimitive(PrimitiveModelType.Cube);
    entity.Add(clickHandler);
    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}

