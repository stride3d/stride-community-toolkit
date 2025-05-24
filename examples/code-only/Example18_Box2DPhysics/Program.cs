using Box2D.NET;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

using var game = new Game();

B2WorldId worldId = new();
B2BodyId bodyId = new();
Entity? boxEntity = null;

// Prepare for simulation. Typically we use a time step of 1/60 of a
// second (60Hz) and 4 sub-steps. This provides a high quality simulation
// in most game scenarios.
float timeStep = 1.0f / 60.0f;
int subStepCount = 4;

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    //game.SetupBase3DScene();
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add3DCamera().Add3DCameraController();
    game.AddDirectionalLight();

    game.AddSkybox();
    game.AddProfiler();

    boxEntity = game.Create3DPrimitive(PrimitiveModelType.Cube);

    boxEntity.Transform.Position = new Vector3(0, 4, 0);

    boxEntity.Scene = rootScene;

    // Construct a world object, which will hold and simulate the rigid bodies.
    var worldDef = b2DefaultWorldDef();
    worldDef.gravity = new B2Vec2(0.0f, -10.0f);
    worldId = b2CreateWorld(ref worldDef);

    // Define the ground body.
    var groundBodyDef = b2DefaultBodyDef();
    groundBodyDef.position = new B2Vec2(0.0f, -10.0f);

    // Call the body factory which allocates memory for the ground body
    // from a pool and creates the ground box shape (also from a pool).
    // The body is also added to the world.
    var groundId = b2CreateBody(worldId, ref groundBodyDef);

    // Define the ground box shape. The extents are the half-widths of the box.
    B2Polygon groundBox = b2MakeBox(50.0f, 10.0f);

    // Add the box shape to the ground body.
    B2ShapeDef groundShapeDef = b2DefaultShapeDef();
    b2CreatePolygonShape(groundId, ref groundShapeDef, ref groundBox);

    // Define the dynamic body. We set its position and call the body factory.
    B2BodyDef bodyDef = b2DefaultBodyDef();
    bodyDef.type = B2BodyType.b2_dynamicBody;
    bodyDef.position = new B2Vec2(0.0f, 4.0f);
    bodyId = b2CreateBody(worldId, ref bodyDef);

    // Define another box shape for our dynamic body.
    B2Polygon dynamicBox = b2MakeBox(1.0f, 1.0f);

    // Define the dynamic body shape
    B2ShapeDef shapeDef = b2DefaultShapeDef();

    // Set the box density to be non-zero, so it will be dynamic.
    shapeDef.density = 1.0f;

    // Override the default friction.
    shapeDef.material.friction = 0.3f;

    // Add the shape to the body.
    b2CreatePolygonShape(bodyId, ref shapeDef, ref dynamicBox);

    var position = b2Body_GetPosition(bodyId);
    var rotation = b2Body_GetRotation(bodyId);

    Console.WriteLine($"Initial Position: {position.X}, {position.Y}");
    Console.WriteLine($"Initial Rotation: {b2Rot_GetAngle(rotation)}, {rotation.c}, {rotation.s}");
}

void Update(Scene rootScene, GameTime gameTime)
{
    b2World_Step(worldId, timeStep, subStepCount);

    // Now print the position and angle of the body.
    var position = b2Body_GetPosition(bodyId);
    var rotation = b2Body_GetRotation(bodyId);

    Console.WriteLine($"Updated Position: {position.X}, {position.Y}");
    Console.WriteLine($"Updated Rotation: {b2Rot_GetAngle(rotation)}, {rotation.c}, {rotation.s}");

    boxEntity.Transform.Position = new Vector3(position.X, position.Y, 0f);
    //boxEntity.Transform.Rotation = Stride.Core.Mathematics.Quaternion.RotationZ(rotation);
}