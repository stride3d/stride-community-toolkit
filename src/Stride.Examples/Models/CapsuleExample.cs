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

            AddCapsule(rootScene, game.CreatePrimitive(PrimitiveModelType.Capsule));
        }
    }

    private static void AddCapsule(Scene rootScene, Entity entity)
    {
        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }
}