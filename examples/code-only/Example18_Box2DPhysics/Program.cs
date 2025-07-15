using Example18_Box2DPhysics;
using Example18_Box2DPhysics.Helpers;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using static Box2D.NET.B2Bodies;

Box2DSimulation? simulation = null; // The simulation handles everything behind the scenes
UiHelper? uiHelper = null;
InputHandler? inputHandler = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    //game.SetupBase3DScene();
    //game.AddGraphicsCompositor().AddCleanUIStage();
    game.Setup2D(Color.CornflowerBlue);
    //game.Add3DCamera().Add3DCameraController();
    game.Add2DCamera().Add2DCameraController();
    game.AddProfiler();

    simulation = new Box2DSimulation();
    var shapeFactory = new ShapeFactory(game, scene);
    uiHelper = new UiHelper(game);

    var camera = scene.GetCamera();
    if (camera != null)
    {
        inputHandler = new InputHandler(game, scene, simulation, camera, shapeFactory);
    }

    PhysicsHelper.AddGround(simulation.GetWorldId());

    inputHandler?.AddBlackRectangleShapes();

    // Learning 2D
    var shape = shapeFactory.GetShapeModel(Primitive2DModelType.Rectangle2D);

    if (shape == null) return;

    var entity = shapeFactory.CreateEntity(shape, position: new(0, 2));
    var bodyId = simulation.CreateDynamicBody(entity, entity.Transform.Position);

    b2Body_SetGravityScale(bodyId, 0);

    PhysicsHelper.CreateShapePhysics(shape, bodyId);
}

void Update(Scene scene, GameTime gameTime)
{
    simulation?.Update(gameTime.Elapsed);
    inputHandler?.ProcessKeyboardInput();
    inputHandler?.ProcessMouseInput();
    uiHelper?.RenderNavigation(inputHandler?.CubeCount);
}