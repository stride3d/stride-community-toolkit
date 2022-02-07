using (var game = new Game())
{
    game.Run(start: Start);

    void Start(Scene rootScene)
    {
        game.SetupBase3DScene();

        var entity = game.CreatePrimitive(PrimitiveModelType.Capsule);

        entity.Transform.Position = new Vector3(0, 8, 0);
        entity.Scene = rootScene;
    }
}