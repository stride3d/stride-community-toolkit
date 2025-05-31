using Box2D.NET;
using Example.Common;
using Example18_Box2DPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Hulls;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

const string ShapeName = "Box2DShape";

B2WorldId worldId = new();
var boxSize = new Vector2(0.2f, 0.2f);
var rectangleSize = new Vector2(0.2f, 0.3f);
int cubes = 0;
int debugX = 5;
int debugY = 30;

List<Shape2DModel> _2DShapes = [
    new() { Type = Primitive2DModelType.Square2D, Color = Color.Green, Size = boxSize },
    new() { Type = Primitive2DModelType.Rectangle2D, Color = Color.Orange, Size = rectangleSize },
    new() { Type = Primitive2DModelType.Circle2D, Color = Color.Red, Size = boxSize / 2 },
    new() { Type = Primitive2DModelType.Triangle2D, Color = Color.Purple, Size = boxSize },
    new() { Type = Primitive2DModelType.Capsule, Color = Color.Blue, Size = new Vector2(boxSize.X, boxSize.Y * 2) }
];

// The simulation handles everything behind the scenes!
Box2DSimulation? box2DSimulation = null;
CameraComponent? camera = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.Window.AllowUserResizing = true;
    //game.SetupBase3DScene();
    //game.AddGraphicsCompositor().AddCleanUIStage();
    game.Setup2D(Color.CornflowerBlue);
    //game.Add3DCamera().Add3DCameraController();
    game.Add2DCamera().Add2DCameraController();
    game.AddProfiler();

    camera = rootScene.GetCamera();

    box2DSimulation = new Box2DSimulation();

    worldId = box2DSimulation.GetWorldId();

    AddGround(worldId);

    AddRectangleShapes(rootScene);
}

void Update(Scene scene, GameTime gameTime)
{
    box2DSimulation?.Update(gameTime.Elapsed);

    ProcessKeyboardInput(scene);

    ProcessMouseInput(scene);

    RenderNavigation();
}

void ProcessKeyboardInput(Scene scene)
{
    if (game.Input.IsKeyPressed(Keys.M))
    {
        Add2DShapes(scene, Primitive2DModelType.Square2D, 10);
    }
    else if (game.Input.IsKeyPressed(Keys.R))
    {
        Add2DShapes(scene, Primitive2DModelType.Rectangle2D, 10);
    }
    else if (game.Input.IsKeyPressed(Keys.C))
    {
        Add2DShapes(scene, Primitive2DModelType.Circle2D, 10);
    }
    else if (game.Input.IsKeyPressed(Keys.T))
    {
        Add2DShapes(scene, Primitive2DModelType.Triangle2D, 10);
    }
    else if (game.Input.IsKeyPressed(Keys.V))
    {
        Add2DShapes(scene, Primitive2DModelType.Capsule, 10);
    }
    else if (game.Input.IsKeyPressed(Keys.P))
    {
        Add2DShapes(scene, count: 50);
    }
    else if (game.Input.IsKeyPressed(Keys.J))
    {
        Add2DShapesWithConstraint(scene, 10);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        // Should be remove also constraints?
        foreach (var entity in scene.Entities.Where(w => w.Name.EndsWith(ShapeName)).ToList())
        {
            box2DSimulation?.RemoveBody(entity);
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    if (game.Input.IsKeyPressed(Keys.G))
    {
        AddRectangleShapes(scene);
    }
}

void ProcessMouseInput(Scene scene)
{
    if (box2DSimulation is null || camera is null) return;

    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        var mousePosition = game.Input.MousePosition;
        var ray = camera.CalculateRayPlaneIntersectionPoint(mousePosition);

        if (ray is null)
        {
            Console.WriteLine("No intersection with the plane found for the mouse ray.");

            return;
        }

        var hitBodyId = box2DSimulation.OverlapPoint(ray.Value);

        if (hitBodyId.HasValue)
        {
            var entity = box2DSimulation?.GetEntity(hitBodyId.Value);

            if (entity != null)
            {
                // If the entity is a 2D primitive, we can apply an impulse to it
                var impulse = new B2Vec2(0.0f, 3.0f); // Apply an upward impulse
                b2Body_ApplyLinearImpulseToCenter(hitBodyId.Value, impulse, true);
                Console.WriteLine($"Applied impulse to {entity.Name} at position {b2Body_GetPosition(hitBodyId.Value)}");
            }
            else
            {
                var bodyName = b2Body_GetName(hitBodyId.Value);
                Console.WriteLine($"Hit body with name {bodyName} but no associated entity found.");
            }
        }
    }
}

void Add2DShapes(Scene scene, Primitive2DModelType? type = null, int count = 5)
{
    for (int i = 1; i <= count; i++)
    {
        var shapeModel = Get2DShape(type);

        if (shapeModel == null) return;

        var entity = CreateEntity(scene, shapeModel);

        var bodyId = box2DSimulation.CreateDynamicBody(entity, entity.Transform.Position);

        Create2DShapePhysics(shapeModel, bodyId);
    }

    SetCubeCount(scene);
}

void Add2DShapesWithConstraint(Scene scene, int count = 5)
{
    var defaultLength = 1f;

    for (int i = 1; i <= count; i++)
    {
        var shapeModel1 = Get2DShape();
        var shapeModel2 = Get2DShape();

        if (shapeModel1 == null || shapeModel2 == null) return;

        var entity1 = CreateEntity(scene, shapeModel1);
        var entity2 = CreateEntity(scene, shapeModel2);
        entity2.Transform.Position = new Vector3(entity1.Transform.Position.X + defaultLength, entity1.Transform.Position.Y, entity1.Transform.Position.Z);

        var myBodyIdA = box2DSimulation.CreateDynamicBody(entity1, entity1.Transform.Position);
        var myBodyIdB = box2DSimulation.CreateDynamicBody(entity2, entity2.Transform.Position);

        Create2DShapePhysics(shapeModel1, myBodyIdA);
        Create2DShapePhysics(shapeModel2, myBodyIdB);

        B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
        jointDef.hertz = 2.0f;
        jointDef.dampingRatio = 0.5f;
        jointDef.length = defaultLength;
        jointDef.maxLength = defaultLength;
        jointDef.minLength = defaultLength;
        jointDef.enableSpring = false;
        jointDef.enableLimit = false;
        //jointDef.collideConnected = true;

        jointDef.bodyIdA = myBodyIdA;
        jointDef.bodyIdB = myBodyIdB;
        jointDef.localAnchorA = new B2Vec2(0, 0);
        jointDef.localAnchorB = new B2Vec2(0.0f, 0);

        var anchorA = b2Body_GetLocalPoint(myBodyIdA, jointDef.localAnchorA);
        var anchorB = b2Body_GetLocalPoint(myBodyIdB, jointDef.localAnchorB);

        var myJointId = b2CreateDistanceJoint(worldId, ref jointDef);
    }

    SetCubeCount(scene);
}

Entity CreateEntity(Scene scene, Shape2DModel shape, Color? color = null)
{
    var entity = game.Create2DPrimitive(shape.Type, new()
    {
        Size = shape.Size,
        Material = game.CreateFlatMaterial(color ?? shape.Color)
    });

    entity.Name = $"{shape.Type}-{ShapeName}";
    entity.Transform.Position = GetRandomPosition();
    entity.Scene = scene;

    return entity;
}

void AddRectangleShapes(Scene scene)
{
    for (int i = 0; i < 50; i++)
    {
        var shapeModel = Get2DShape(Primitive2DModelType.Rectangle2D);

        if (shapeModel == null) return;

        var entity = CreateEntity(scene, shapeModel, Color.Black);

        var bodyId = box2DSimulation.CreateDynamicBody(entity, entity.Transform.Position);

        Create2DShapePhysics(shapeModel, bodyId);
    }

    SetCubeCount(scene);
}

Shape2DModel? Get2DShape(Primitive2DModelType? type = null)
{
    if (type == null)
    {
        int randomIndex = Random.Shared.Next(_2DShapes.Count);

        return _2DShapes[randomIndex];
    }

    return _2DShapes.Find(x => x.Type == type);
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Count(w => w.Name.EndsWith(ShapeName));

static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);

static void Create2DShapePhysics(Shape2DModel shapeModel, B2BodyId bodyId)
{
    // Create shape for the body
    var shapeDef = b2DefaultShapeDef();
    shapeDef.density = 2.0f;
    shapeDef.material.friction = 0.3f;

    if (shapeModel.Type == Primitive2DModelType.Square2D || shapeModel.Type == Primitive2DModelType.Rectangle2D)
    {
        var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
        b2CreatePolygonShape(bodyId, ref shapeDef, ref box);
    }
    else if (shapeModel.Type == Primitive2DModelType.Circle2D)
    {
        var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);
        b2CreateCircleShape(bodyId, ref shapeDef, ref circle);
    }
    else if (shapeModel.Type == Primitive2DModelType.Triangle2D)
    {
        var meshData = TriangleProceduralModel.New(shapeModel.Size);
        var points2 = meshData.Vertices.Select(v => new B2Vec2(v.Position.X, v.Position.Y)).ToArray();

        //// Define the three vertices of your triangle
        //// For an equilateral triangle centered at origin with size as the width
        //float halfWidth = shapeModel.Size.X / 2;
        //float halfHeight = shapeModel.Size.Y / 2;

        //B2Vec2[] points = new B2Vec2[3] {
        //    new B2Vec2(0, halfHeight),            // Top vertex
        //    new B2Vec2(-halfWidth, -halfHeight),  // Bottom left
        //    new B2Vec2(halfWidth, -halfHeight)    // Bottom right
        //};

        // Create a hull from these points
        B2Hull hull = b2ComputeHull(points2, 3);

        // Create a polygon shape from the hull
        B2Polygon triangle = b2MakePolygon(ref hull, 0.0f);

        // Create the shape on the body
        b2CreatePolygonShape(bodyId, ref shapeDef, ref triangle);
    }
    else if (shapeModel.Type == Primitive2DModelType.Capsule)
    {
        var capsule = new B2Capsule(
            new(0, -shapeModel.Size.X / 2),
            new(0, (shapeModel.Size.Y / 2) - shapeModel.Size.X / 2),
            shapeModel.Size.X / 2);

        b2CreateCapsuleShape(bodyId, ref shapeDef, ref capsule);
    }
}

void RenderNavigation()
{
    var space = 0;
    game.DebugTextSystem.Print($"Cubes: {cubes}", new Int2(x: debugX, y: debugY));
    space += 30;
    game.DebugTextSystem.Print($"X - Delete all cubes and shapes", new Int2(x: debugX, y: debugY + space), Color.Red);
    space += 20;
    game.DebugTextSystem.Print($"M - Generate 2D squares", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"R - Generate 2D rectangles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"C - Generate 2D circles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"T - Generate 2D triangles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"V - Generate 2D capsules", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"J - Generate random shapes with constraint", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"P - Generate random shapes", new Int2(x: debugX, y: debugY + space));
}

static void AddGround(B2WorldId worldId)
{
    // Define the ground body.
    var groundBodyDef = b2DefaultBodyDef();
    groundBodyDef.position = new B2Vec2(0.0f, -10.0f);
    groundBodyDef.name = "Ground";

    var groundId = b2CreateBody(worldId, ref groundBodyDef);

    B2Polygon groundBox = b2MakeBox(50.0f, 10);

    // Add the box shape to the ground body.
    B2ShapeDef groundShapeDef = b2DefaultShapeDef();
    b2CreatePolygonShape(groundId, ref groundShapeDef, ref groundBox);
}