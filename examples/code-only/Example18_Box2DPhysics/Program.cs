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

static class GameConfig
{
    public const string ShapeName = "Box2DShape";
    public const int DefaultSpacing = 20;
    public const int DefaultDebugX = 5;
    public const int DefaultDebugY = 30;
    public const int HeaderSpacing = 30;
    public static readonly Vector2 BoxSize = new(0.2f, 0.2f);
    public static readonly Vector2 RectangleSize = new(0.2f, 0.3f);
}

class ShapeFactory
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly List<Shape2DModel> _shapes =
    [
        new() { Type = Primitive2DModelType.Square2D, Color = Color.Green, Size = GameConfig.BoxSize },
        new() { Type = Primitive2DModelType.Rectangle2D, Color = Color.Orange, Size = GameConfig.RectangleSize },
        new() { Type = Primitive2DModelType.Circle2D, Color = Color.Red, Size = GameConfig.BoxSize / 2 },
        new() { Type = Primitive2DModelType.Triangle2D, Color = Color.Purple, Size = GameConfig.BoxSize },
        new()
        {
            Type = Primitive2DModelType.Capsule, Color = Color.Blue,
            Size = new Vector2(GameConfig.BoxSize.X, GameConfig.BoxSize.Y * 2)
        }
    ];

    public ShapeFactory(Game game, Scene scene)
    {
        _game = game;
        _scene = scene;
    }

    public Shape2DModel? GetShapeModel(Primitive2DModelType? type = null)
    {
        if (type == null)
        {
            int randomIndex = Random.Shared.Next(_shapes.Count);
            return _shapes[randomIndex];
        }

        return _shapes.Find(x => x.Type == type);
    }

    public Entity CreateEntity(Shape2DModel shape, Color? color = null, Vector2? position = null)
    {
        var entity = _game.Create2DPrimitive(shape.Type, new()
        {
            Size = shape.Size,
            Material = _game.CreateFlatMaterial(color ?? shape.Color)
        });

        entity.Name = $"{shape.Type}-{GameConfig.ShapeName}";
        entity.Transform.Position = position.HasValue ? (Vector3)position : GetRandomPosition();
        entity.Scene = _scene;

        return entity;
    }

    private static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);
}

class InputHandler
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Box2DSimulation _simulation;
    private readonly CameraComponent _camera;
    private readonly ShapeFactory _shapeFactory;
    private readonly B2WorldId _worldId;
    public int CubeCount { get; private set; }

    public InputHandler(Game game, Scene scene, Box2DSimulation simulation,
        CameraComponent camera, ShapeFactory shapeFactory)
    {
        _game = game;
        _scene = scene;
        _simulation = simulation;
        _camera = camera;
        _shapeFactory = shapeFactory;
        _worldId = simulation.GetWorldId();
    }

    public void ProcessKeyboardInput()
    {
        if (_game.Input.IsKeyPressed(Keys.M))
        {
            AddShapes(Primitive2DModelType.Square2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.R))
        {
            AddShapes(Primitive2DModelType.Rectangle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.C))
        {
            AddShapes(Primitive2DModelType.Circle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.T))
        {
            AddShapes(Primitive2DModelType.Triangle2D, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.V))
        {
            AddShapes(Primitive2DModelType.Capsule, 10);
        }
        else if (_game.Input.IsKeyPressed(Keys.P))
        {
            AddShapes(count: 50);
        }
        else if (_game.Input.IsKeyPressed(Keys.J))
        {
            AddShapesWithConstraint(10);
        }
        else if (_game.Input.IsKeyReleased(Keys.X))
        {
            ClearAllShapes();
        }
        else if (_game.Input.IsKeyPressed(Keys.G))
        {
            AddBlackRectangleShapes();
        }

        void ClearAllShapes()
        {
            // Should we remove also constraints?
            foreach (var entity in _scene.Entities.Where(w => w.Name.EndsWith(GameConfig.ShapeName)).ToList())
            {
                _simulation?.RemoveBody(entity);
                entity.Remove();
            }

            SetCubeCount();
        }
    }

    public void ProcessMouseInput()
    {
        if (!_game.Input.IsMouseButtonPressed(MouseButton.Left)) return;

        var mousePosition = _game.Input.MousePosition;
        var ray = _camera.CalculateRayPlaneIntersectionPoint(mousePosition);

        if (ray is null)
        {
            Console.WriteLine("No intersection with the plane found for the mouse ray.");

            return;
        }

        var hitBodyId = _simulation.OverlapPoint(ray.Value);

        if (!hitBodyId.HasValue) return;

        var entity = _simulation.GetEntity(hitBodyId.Value);

        if (entity == null)
        {
            var bodyName = b2Body_GetName(hitBodyId.Value);
            Console.WriteLine($"Hit body with name {bodyName} but no associated entity found.");

            return;
        }

        var position = b2Body_GetPosition(hitBodyId.Value);
        // If the entity is a 2D primitive, we can apply an impulse to it
        var impulse = new B2Vec2(0.0f, 3.0f); // Apply an upward impulse

        b2Body_ApplyLinearImpulseToCenter(hitBodyId.Value, impulse, true);

        Console.WriteLine($"Applied impulse to {entity.Name} at position {position.X} , {position.Y}");
    }

    private void AddShapes(Primitive2DModelType? type = null, int count = 5, Color? color = null)
    {
        for (var i = 1; i <= count; i++)
        {
            var shapeModel = _shapeFactory.GetShapeModel(type);

            if (shapeModel == null) return;

            var entity = _shapeFactory.CreateEntity(shapeModel, color);
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            PhysicsHelper.CreateShapePhysics(shapeModel, bodyId);
        }

        SetCubeCount();
    }

    private void SetCubeCount() => CubeCount = _scene.Entities.Count(w => w.Name.EndsWith(GameConfig.ShapeName));

    public void AddBlackRectangleShapes() => AddShapes(Primitive2DModelType.Rectangle2D, 50, Color.Black);

    private void AddShapesWithConstraint(int count = 5)
    {
        var defaultLength = 1f;

        for (var i = 1; i <= count; i++)
        {
            var shapeModel1 = _shapeFactory.GetShapeModel();
            var shapeModel2 = _shapeFactory.GetShapeModel();

            if (shapeModel1 == null || shapeModel2 == null) return;

            var entity1 = _shapeFactory.CreateEntity(shapeModel1);
            var entity2 = _shapeFactory.CreateEntity(shapeModel2);
            entity2.Transform.Position = new Vector3(entity1.Transform.Position.X + defaultLength,
                entity1.Transform.Position.Y, entity1.Transform.Position.Z);

            var bodyIdA = _simulation.CreateDynamicBody(entity1, entity1.Transform.Position);
            var bodyIdB = _simulation.CreateDynamicBody(entity2, entity2.Transform.Position);

            PhysicsHelper.CreateShapePhysics(shapeModel1, bodyIdA);
            PhysicsHelper.CreateShapePhysics(shapeModel2, bodyIdB);

            B2DistanceJointDef jointDef = b2DefaultDistanceJointDef();
            jointDef.hertz = 2.0f;
            jointDef.dampingRatio = 0.5f;
            jointDef.length = defaultLength;
            jointDef.maxLength = defaultLength;
            jointDef.minLength = defaultLength;
            jointDef.enableSpring = false;
            jointDef.enableLimit = false;
            //jointDef.collideConnected = true;

            jointDef.@base.bodyIdA = bodyIdA;
            jointDef.@base.bodyIdB = bodyIdB;
            jointDef.@base.localFrameA.p = new B2Vec2(0, 0);
            jointDef.@base.localFrameB.p = new B2Vec2(0.0f, 0);

            b2CreateDistanceJoint(_worldId, ref jointDef);
        }

        SetCubeCount();
    }
}

class UiHelper(Game game)
{
    private readonly (string, Color)[] _commands = GetNavigationCommands();

    public void RenderNavigation(int? cubeCount = 0)
    {
        var space = 0;
        game.DebugTextSystem.Print($"Cubes: {cubeCount}",
            new Int2(x: GameConfig.DefaultDebugX, y: GameConfig.DefaultDebugY));
        space += GameConfig.HeaderSpacing;

        foreach (var (text, color) in _commands)
        {
            game.DebugTextSystem.Print(text, new Int2(x: GameConfig.DefaultDebugX, y: GameConfig.DefaultDebugY + space),
                color);
            space += GameConfig.DefaultSpacing;
        }
    }

    private static (string, Color)[] GetNavigationCommands() =>
    [
        ("X - Delete all cubes and shapes", Color.Red),
        ("M - Generate 2D squares", Color.White),
        ("R - Generate 2D rectangles", Color.White),
        ("C - Generate 2D circles", Color.White),
        ("T - Generate 2D triangles", Color.White),
        ("V - Generate 2D capsules", Color.White),
        ("J - Generate random shapes with constraint", Color.White),
        ("P - Generate random shapes", Color.White)
    ];
}

static class PhysicsHelper
{
    public static void CreateShapePhysics(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef? shapeDef = null)
    {
        var nonNullableShapeDef = shapeDef ?? CreateDefaultShapeDef();

        switch (shapeModel.Type)
        {
            case Primitive2DModelType.Square2D:
            case Primitive2DModelType.Rectangle2D:
                var box = b2MakeBox(shapeModel.Size.X / 2, shapeModel.Size.Y / 2);
                b2CreatePolygonShape(bodyId, ref nonNullableShapeDef, ref box);
                break;

            case Primitive2DModelType.Circle2D:
                var circle = new B2Circle(new B2Vec2(0.0f, 0.0f), shapeModel.Size.X);
                b2CreateCircleShape(bodyId, ref nonNullableShapeDef, ref circle);
                break;

            case Primitive2DModelType.Triangle2D:
                CreateTriangleShape(shapeModel, bodyId, nonNullableShapeDef);
                break;

            case Primitive2DModelType.Capsule:
                var capsule = new B2Capsule(
                    new(0, -shapeModel.Size.X / 2),
                    new(0, (shapeModel.Size.Y / 2) - shapeModel.Size.X / 2),
                    shapeModel.Size.X / 2);
                b2CreateCapsuleShape(bodyId, ref nonNullableShapeDef, ref capsule);
                break;
        }

        static void CreateTriangleShape(Shape2DModel shapeModel, B2BodyId bodyId, B2ShapeDef shapeDef)
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
    }

    public static B2ShapeDef CreateDefaultShapeDef()
    {
        var shapeDef = b2DefaultShapeDef();
        shapeDef.density = 2.0f;
        shapeDef.material.friction = 0.3f;

        return shapeDef;
    }

    public static void AddGround(B2WorldId worldId)
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
}