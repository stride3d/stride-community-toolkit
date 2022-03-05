namespace Stride.Examples.Models;

public static class CapsuleExample
{
    public static void Run()
    {
        using var game = new Game();

        game.Run(start: Start);

        void Start(Scene rootScene)
        {
            game.SetupBase3DScene();

            AddCapusule(rootScene, game);
        }
    }

    private static void AddCapusule(Scene rootScene, Game game)
    {
        var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }
}