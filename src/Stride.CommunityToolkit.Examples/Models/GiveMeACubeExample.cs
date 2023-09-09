using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.GameDefaults.Extensions;
using Stride.GameDefaults.ProceduralModels;

namespace Stride.Examples.Models;

public static class GiveMeACubeExample
{
    public static void Run()
    {
        using var game = new Game();

        game.Run(start: (Scene rootScene) =>
        {
            game.SetupBase3DScene();

            var entity = game.CreatePrimitive(PrimitiveModelType.Cube);

            entity.Transform.Position = new Vector3(1f, 0.5f, 3f);

            entity.Scene = rootScene;
        });
    }
}
