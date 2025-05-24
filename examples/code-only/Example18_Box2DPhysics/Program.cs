using Box2D.NET;
using Example.Common;
using Example18_Box2DPhysics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.Helpers;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

const float Depth = 1;
const string ShapeName = "BepuCube";

var boxSize = new Vector3(0.2f, 0.2f, Depth);
var rectangleSize = new Vector3(0.2f, 0.3f, Depth);
int cubes = 0;
int debugX = 5;
int debugY = 30;

List<Shape2DModel> _2DShapes = [
    new() { Type = Primitive2DModelType.Square, Color = Color.Green, Size = (Vector2)boxSize },
    new() { Type = Primitive2DModelType.Rectangle, Color = Color.Orange, Size = (Vector2)rectangleSize },
    new() { Type = Primitive2DModelType.Circle, Color = Color.Red, Size = (Vector2)boxSize / 2 },
    new() { Type = Primitive2DModelType.Triangle, Color = Color.Purple, Size = (Vector2)boxSize }
];

List<Shape3DModel> _3DShapes = [
    new() { Type = PrimitiveModelType.Cube, Color = Color.Green, Size = boxSize },
    new() { Type = PrimitiveModelType.RectangularPrism, Color = Color.Orange, Size = rectangleSize },
    new() { Type = PrimitiveModelType.Cylinder, Color = Color.Red, Size = boxSize },
    new() { Type = PrimitiveModelType.TriangularPrism, Color = Color.Purple, Size = boxSize }
];

Box2DSimulation? box2DSimulation = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    //game.SetupBase3DScene();
    game.AddGraphicsCompositor().AddCleanUIStage();
    game.Add2DCamera().Add2DCameraController();
    game.AddDirectionalLight();

    game.AddSkybox();
    game.AddProfiler();

    box2DSimulation = new Box2DSimulation();

    var worldId = box2DSimulation.GetWorldId();

    // Define the ground body.
    var groundBodyDef = b2DefaultBodyDef();
    groundBodyDef.position = new B2Vec2(0.0f, -10.0f);
    var groundId = b2CreateBody(worldId, ref groundBodyDef);

    B2Polygon groundBox = b2MakeBox(50.0f, 10.0f);

    // Add the box shape to the ground body.
    B2ShapeDef groundShapeDef = b2DefaultShapeDef();
    b2CreatePolygonShape(groundId, ref groundShapeDef, ref groundBox);

    AddBoxEntityWithPhysics(rootScene);
}

void Update(Scene scene, GameTime gameTime)
{
    // The simulation handles everything behind the scenes!
    box2DSimulation?.Update(gameTime.Elapsed);

    if (game.Input.IsKeyPressed(Keys.M))
    {
        Add2DShapes(scene, Primitive2DModelType.Square, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.R))
    {
        Add2DShapes(scene, Primitive2DModelType.Rectangle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.C))
    {
        Add2DShapes(scene, Primitive2DModelType.Circle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.T))
    {
        Add2DShapes(scene, Primitive2DModelType.Triangle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.P))
    {
        Add2DShapes(scene, count: 30);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(w => w.Name == "BepuCube" || w.Name == "Cube").ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    if (game.Input.IsKeyPressed(Keys.G))
    {
        AddBoxEntityWithPhysics(scene);
    }

    RenderNavigation();
}

void Add2DShapes(Scene scene, Primitive2DModelType? type = null, int count = 5)
{
    //var entity = new Entity();

    for (int i = 1; i <= count; i++)
    {
        var shapeModel = Get2DShape(type);

        if (shapeModel == null) return;

        var entity = game.Create2DPrimitive(shapeModel.Type,
            new()
            {
                Size = shapeModel.Size,
                Material = game.CreateMaterial(shapeModel.Color)
            });

        entity.Name = ShapeName;
        entity.Transform.Position = GetRandomPosition();
        entity.Scene = scene;

        var bodyId2 = box2DSimulation.CreateDynamicBody(entity, entity.Transform.Position);

        // Create shape for the body
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.3f;
        if (shapeModel.Type == Primitive2DModelType.Square || shapeModel.Type == Primitive2DModelType.Rectangle)
        {
            var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
            b2CreatePolygonShape(bodyId2, ref shapeDef, ref box);
        }
        //else if (shapeModel.Type == Primitive2DModelType.Circle)
        //{
        //    var circle = b2MakeCircle(shapeModel.Size.X / 2);
        //    b2CreateCircleShape(bodyId2, ref shapeDef, ref circle);
        //}
        //else if (shapeModel.Type == Primitive2DModelType.Triangle)
        //{
        //    var triangle = b2MakeTriangle(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
        //    b2CreatePolygonShape(bodyId2, ref shapeDef, ref triangle);
        //}
    }
}

void AddBoxEntityWithPhysics(Scene rootScene)
{
    for (int i = 0; i < 50; i++)
    {
        var boxEntity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DCreationOptions { Size = boxSize });
        boxEntity.Transform.Position = VectorHelper.RandomVector3([-5, 5], [10, 20], [0, 0]);
        boxEntity.Scene = rootScene;
        // Register with physics simulation
        var bodyId2 = box2DSimulation.CreateDynamicBody(boxEntity, boxEntity.Transform.Position);

        // Create shape for the body
        var dynamicBox = b2MakeBox(boxSize.X, boxSize.Y);
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = 1.0f;
        shapeDef.material.friction = 0.3f;
        b2CreatePolygonShape(bodyId2, ref shapeDef, ref dynamicBox);

        var position = b2Body_GetPosition(bodyId2);
        var rotation = b2Body_GetRotation(bodyId2);

        Console.WriteLine($"Initial Position: {position.X}, {position.Y}");
        Console.WriteLine($"Initial Rotation: {b2Rot_GetAngle(rotation)}, {rotation.c}, {rotation.s}");
    }
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

void SetCubeCount(Scene scene) => cubes = scene.Entities.Where(w => w.Name == "BepuCube" || w.Name == "Cube").Count();

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
    game.DebugTextSystem.Print($"P - Generate random 2D shapes", new Int2(x: debugX, y: debugY + space));
}

static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);