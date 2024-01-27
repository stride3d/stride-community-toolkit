//using DebugShapes;
using Example_2D_Playground;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering.Sprites;
using System.Reflection;
using System.Xml.Linq;

var boxSize = new Vector3(0.2f);
var rectangleSize = new Vector3(0.2f, 0.3f, 0);
int cubes = 0;
int debugX = 5;
int debugY = 30;
var bgImage = "JumpyJetBackground.jpg";
const string ShapeName = "Shape";

using var game = new Game();

//int currentNumPrimitives = 1024;
Simulation? _simulation = null;
CameraComponent? _camera = null;
Scene scene = new();
//ImmediateDebugRenderSystem? DebugDraw = null;
//List<Entity> cubesList = [];

List<Shape2DModel> shapes = [
    new() { Type = Primitive2DModelType.Square, Color = Color.Green, Size = (Vector2)boxSize },
    new() { Type = Primitive2DModelType.Rectangle, Color = Color.Orange, Size = (Vector2)rectangleSize },
    new() { Type = Primitive2DModelType.Circle, Color = Color.Red, Size = (Vector2)boxSize / 2 },
    //new() { Type = Primitive2DModelType.Capsule, Color = Color.Purple, Size = rectangleSize }
    new() { Type = Primitive2DModelType.Triangle, Color = Color.Purple, Size = (Vector2)boxSize }
    //new() { Type = Primitive2DModelType.Triangle, Color = Color.Purple, Size = (Vector2)rectangleSize }
];

Dictionary<Primitive2DModelType, Entity> templates = [];

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    scene = rootScene;

    game.Window.AllowUserResizing = true;
    game.Window.Title = "2D Example";

    //game.SetupBase2DScene();
    //game.SetupBase3DScene();

    game.AddGraphicsCompositor().AddCleanUIStage();
    //game.AddGraphicsCompositor().AddCleanUIStage().AddImmediateDebugRenderFeature();

    game.Add3DCamera().Add3DCameraController();
    //game.Add2DCamera().Add2DCameraController();

    game.AddDirectionalLight();
    game.AddAllDirectionLighting(intensity: 5f, true);
    game.AddSkybox();

    //game.Add2DGround();
    game.Add3DGround();
    //game.AddInfinite3DGround();

    game.AddGroundGizmo(new(0, 0, -7.5f), showAxisName: true);
    game.AddProfiler();
    //game.ShowColliders();

    //AddSpriteBatchRenderer(rootScene);

    _camera = game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

    // needed for Raycast
    _simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
    //simulation.FixedTimeStep = 1f / 90;

    //var processor = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>();

    //Add2DShapes(ShapeType.Square, 1);

    AddBackground(bgImage);

    //DebugDraw = new ImmediateDebugRenderSystem(game.Services, RenderGroup.Group1);
    //DebugDraw.PrimitiveColor = Color.Green;
    //DebugDraw.MaxPrimitives = (currentNumPrimitives * 2) + 8;
    //DebugDraw.MaxPrimitivesWithLifetime = (currentNumPrimitives * 2) + 8;
    //DebugDraw.Visible = true;

    //// keep DebugText visible in release builds too
    //game.DebugTextSystem.Visible = true;
    //game.Services.AddService(DebugDraw);
    //game.GameSystems.Add(DebugDraw);
}

void Update(Scene scene, GameTime time)
{
    //var gameSettings = game.Services.GetService<IGameSettingsService>();
    //simulation.ContinuousCollisionDetection = true;

    if (!game.Input.HasKeyboard) return;

    if (game.Input.IsMouseButtonDown(MouseButton.Left))
    {
        ProcessRaycast(MouseButton.Left, game.Input.MousePosition);
    }

    if (game.Input.IsKeyPressed(Keys.M))
    {
        Add2DShapes(Primitive2DModelType.Square, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.R))
    {
        Add2DShapes(Primitive2DModelType.Rectangle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.C))
    {
        Add2DShapes(Primitive2DModelType.Circle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.T))
    {
        Add2DShapes(Primitive2DModelType.Triangle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.P))
    {
        Add2DShapes(count: 30);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(w => w.Name == ShapeName).ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    //for (var i = 0; i < cubesList.Count; i++)
    //{
    //    var entity = cubesList[i];

    //    DebugDraw.DrawCube(entity.Transform.Position, boxSize, entity.Transform.Rotation, Color.Red, depthTest: false, solid: !true);
    //}

    RenderNavigation();
}

void RenderNavigation()
{
    var space = 0;
    game.DebugTextSystem.Print($"Cubes: {cubes}", new Int2(x: debugX, y: debugY));
    space += 30;
    game.DebugTextSystem.Print($"X - Delete all cubes and shapes", new Int2(x: debugX, y: debugY + space), Color.Red);
    //game.DebugTextSystem.Print($"N - generate 3D cubes", new Int2(x: debugX, y: debugY + space + 60));
    space += 20;
    game.DebugTextSystem.Print($"M - generate 2D squares", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"R - generate 2D rectangles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"C - generate 2D circles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"T - generate 2D triangles", new Int2(x: debugX, y: debugY + space));
    space += 20;
    game.DebugTextSystem.Print($"P - generate random 2D shapes", new Int2(x: debugX, y: debugY + space));
}

void ProcessRaycast(MouseButton mouseButton, Vector2 mousePosition)
{
    var hitResult = _camera!.RaycastMouse(_simulation!, mousePosition);

    if (hitResult.Succeeded && hitResult.Collider.Entity.Name == ShapeName && mouseButton == MouseButton.Left)
    {
        var rigidBody = hitResult.Collider.Entity.Get<RigidbodyComponent>();

        if (rigidBody == null) return;

        var direction = new Vector3(0, 20, 0);

        rigidBody.ApplyImpulse(direction * 10);
        rigidBody.LinearVelocity = direction * 1;
    }
}

void AddBackground(string fileName)
{
    var entity = new Entity("Background");

    var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    var filePath = Path.Combine(directory, fileName);

    using var input = File.OpenRead(filePath);
    var texture = Texture.Load(game.GraphicsDevice, input);

    var spriteComponent = new SpriteComponent
    {
        SpriteProvider = new SpriteFromTexture { Texture = texture },
    };

    entity.Add(spriteComponent);
    entity.Transform.Position.Z = -100;
    entity.Transform.Position.Y = 0f;

    entity.Scene = scene;
}

void Add2DShapes(Primitive2DModelType? type = null, int count = 5)
{
    var entity = new Entity();

    for (int i = 1; i <= count; i++)
    {
        var shapeModel = GetShape(type);

        if (shapeModel == null) return;

        if (type == null || i == 1)
        {
            entity = game.Create2DPrimitive(shapeModel.Type, new() { Size = shapeModel.Size, Material = game.CreateMaterial(shapeModel.Color) });
        }
        else
        {
            entity = entity.Clone();
        }

        entity.Name = ShapeName;
        entity.Transform.Position = GetRandomPosition();
        entity.Scene = scene;

        AddAngularAndLinearFactor(shapeModel.Type, entity);
    }

    static void AddAngularAndLinearFactor(Primitive2DModelType? type, Entity entity)
    {
        if (type != Primitive2DModelType.Triangle) return;

        var rigidBody = entity.Get<RigidbodyComponent>();
        rigidBody.AngularFactor = new Vector3(0, 0, 1);
        rigidBody.LinearFactor = new Vector3(1, 1, 0);

        // seems doing nothing
        //rigidBody.CcdMotionThreshold = 10000;
        //rigidBody.CcdSweptSphereRadius = 10000;
    }
}

Shape2DModel? GetShape(Primitive2DModelType? type = null)
{
    if (type == null)
    {
        int randomIndex = Random.Shared.Next(shapes.Count);

        return shapes[randomIndex];
    }

    return shapes.Find(x => x.Type == type);
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Count(w => w.Name == ShapeName);

static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), 3 + Random.Shared.Next(0, 7), 0);