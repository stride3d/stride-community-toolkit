using Example18_Box2DPhysics;
using Example18_Box2DPhysics.Helpers;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

Box2DSimulation? simulation = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Add2DGraphicsCompositor(Color.CornflowerBlue);
    game.Add2DCamera().Add2DCameraController();
    game.AddProfiler();

    simulation = new Box2DSimulation();
    var shapeFactory = new ShapeFactory(game, scene);

    var camera = scene.GetCamera();

    PhysicsHelper.AddGround(simulation.GetWorldId());

    for (var i = 1; i <= 50; i++)
    {
        var shapeModel = shapeFactory.GetShapeModel(Primitive2DModelType.Triangle2D);

        if (shapeModel == null) return;

        var entity = shapeFactory.CreateEntity(shapeModel, Color.Blue);
        var bodyId = simulation.CreateDynamicBody(entity, entity.Transform.Position);

        PhysicsHelper.CreateShapePhysics(shapeModel, bodyId);
    }
}

void Update(Scene scene, GameTime gameTime)
{
    simulation?.Update(gameTime.Elapsed);
}