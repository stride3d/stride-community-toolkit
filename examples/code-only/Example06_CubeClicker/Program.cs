using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Example06_SaveTheCube;
using Example06_CubeClicker;

using var game = new Game();

var data = new UiData();
DataSaver<UiData> cubeSaver = new()
{
    Data = data
};
if(cubeSaver.TryLoad(out var loadedState))
{
    cubeSaver = loadedState;
}
else
{
    data.Clickables.Add(new Left());
    data.Clickables.Add(new Right());
}

var clickHandler = new ClickHandlerComponent()
{
    DataSaver = cubeSaver
};
game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddCapsule(rootScene);
}

void AddCapsule(Scene rootScene)
{
    var entity = game.CreatePrimitive(PrimitiveModelType.Cube);
    entity.Add(clickHandler);
    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = rootScene;
}

