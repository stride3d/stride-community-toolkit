# Capsule with rigid body in Visual Basic

[!INCLUDE [capsule-with-rigid-body](../../../includes/manual/examples/capsule-with-rigid-body.md)]

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/Example01_Basic3DScene_VBasic).

[!code-vb[](../../../../examples/code-only/Example01_Basic3DScene_VBasic/Program.vb)]

- `Private game As New Game()` This line of code creates a new instance of the `Game` class. The Game class is central to the Stride engine, managing the overall game loop, scenes, and updates to the entities.
- `GameExtensions.Run(game, Nothing, AddressOf StartGame)` This line initiates the game loop. The `Run` method, from `GameExtensions`, is responsible for starting the game and it takes a reference to the `StartGame` method as a parameter. This method is called once when the game starts. The `Nothing` argument here is for an optional parameter that is not being used in this case.
- `game.SetupBase3DScene()` This line sets up a basic 3D scene. It's a helper method provided to quickly set up a scene with a default camera, lighting, and skybox.
- `Dim entity = game.CreatePrimitive(PrimitiveModelType.Capsule)` Here, a new entity is created in the form of a 3D capsule primitive. The `CreatePrimitive` method is a helper method provided to create basic 3D shapes.
- `entity.Transform.Position = New Vector3(0, 8, 0)` This line sets the position of the created entity in the 3D space. The `Position` property of the `Transform` component determines the location of the entity.
- `entity.Scene = rootScene` Finally, the entity is added to the `rootScene`. The `Scene` property of an entity determines which scene it belongs to.