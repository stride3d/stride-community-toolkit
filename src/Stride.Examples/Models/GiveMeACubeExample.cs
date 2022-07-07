using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.GameDefaults;
using Stride.GameDefaults.Extensions;

public class GiveMeACubeExample
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
