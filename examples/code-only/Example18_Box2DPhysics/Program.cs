using Example18_Box2DPhysics;
using Example18_Box2DPhysics.Helpers;
using Example18_Box2DPhysics.Physics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using static Box2D.NET.B2Bodies;

// Example 18: Box2D Physics Integration
// This example demonstrates how to integrate Box2D.NET with Stride game engine
// for 2D physics simulations with shapes, collisions, and interactive controls

// Global variables for the demo
Box2DSimulation? simulation = null;
SceneManager? sceneManager = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Configure the game window
    game.Window.AllowUserResizing = true;
    game.Window.Title = "Box2D Physics Example - Stride Community Toolkit";

    // Setup 2D scene with camera and controls
    game.SetupBase2D(clearColor: new Color(0.2f));
    //game.AddGraphicsCompositor();
    //game.AddGraphicsCompositor2();
    //game.Add2DGraphicsCompositor(clearColor);
    //game.Add3DCamera().Add3DCameraController();
    //game.AddSkybox();
    game.AddProfiler();
    game.AddRootRenderFeature(new MeshOutlineRenderFeature2
    {
        ScaleAdjust = 0.04f
    });

    // Wall colour is: b2_colorPaleGreen = 0x98FB98,
    // Awake shape colour is:  b2_colorPink = 0xFFC0CB,
    // Sleep shape colour is:  b2_colorGray = 0x808080,

    // Initialize the Box2D physics simulation
    simulation = new Box2DSimulation();
    ConfigurePhysicsWorld(simulation);

    // Initialize the demo manager to handle all demo logic
    var camera = scene.GetCamera();

    if (camera is null)
    {
        throw new InvalidOperationException("Camera not found in scene");
    }
    else
    {
        sceneManager = new SceneManager(game, scene, simulation, camera);
        sceneManager.Initialize();
    }

    // Create the initial scene setup
    CreateInitialScene(scene);
}

void Update(Scene scene, GameTime gameTime)
{
    // Update physics simulation
    simulation?.Update(gameTime.Elapsed);

    // Update demo manager (handles input and UI)
    sceneManager?.Update(gameTime);
}

void ConfigurePhysicsWorld(Box2DSimulation simulation)
{
    // Configure gravity (negative Y is down)
    simulation.Gravity = new Vector2(0f, GameConfig.Gravity);

    // Enable contact events for collision detection
    simulation.EnableContactEvents = true;
    simulation.EnableSensorEvents = true;

    // Set physics timestep properties
    simulation.TimeScale = 1.0f;
    simulation.MaxStepsPerFrame = 3;
}

void CreateInitialScene(Scene scene)
{
    if (simulation == null) return;

    // Add ground for physics objects to collide with
    WorldGeometryBuilder.AddGround(simulation.GetWorldId());

    // Create a simple demonstration with a few shapes
    var shapeFactory = new ShapeFactory(game, scene);

    // Create a single shape with zero gravity for demonstration
    var shape = shapeFactory.GetShapeModel(Primitive2DModelType.Rectangle2D);

    if (shape != null)
    {
        var entity = shapeFactory.CreateEntity(shape, position: new Vector2(0, 2));
        var bodyId = simulation.CreateDynamicBody(entity, entity.Transform.Position);

        // Set zero gravity for this body to demonstrate control
        b2Body_SetGravityScale(bodyId, 0);

        ShapeFixtureBuilder.AttachShape(shape, bodyId);
    }

    // Add some initial shapes for interaction
    sceneManager?.AddInitialShapes();
}