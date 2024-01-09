using DebugShapes;
using Example01_Basic2DScene;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Sprites;
using System.Reflection;
using System.Xml.Linq;

using var game = new Game();

var boxSize = new Vector3(0.2f);
var rectangleSize = new Vector3(0.2f, 0.3f, 0);
int cubes = 0;
int debugX = 5;
int debugY = 30;
int currentNumPrimitives = 1024;
Simulation? simulation = null;
CameraComponent? _camera = null;
Scene scene = new();
ImmediateDebugRenderSystem? DebugDraw = null;
List<Entity> cubesList = [];

List<ShapeModel> shapes = [
    new() { Type = ShapeType.Square, Color = Color.Green, Size = boxSize },
    new() { Type = ShapeType.Rectangle, Color = Color.Orange, Size = rectangleSize },
    new() { Type = ShapeType.Circle, Color = Color.Red, Size = boxSize },
    //new() { Type = ShapeType.Triangle, Color = Color.Purple, Size = rectangleSize }
];

game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    scene = rootScene;

    game.Window.AllowUserResizing = true;
    game.Window.Title = "2D Example";

    //game.SetupBase2DScene();
    //game.SetupBase3DScene();

    //game.AddGraphicsCompositor().AddCleanUIStage();
    game.AddGraphicsCompositor().AddCleanUIStage()
    .AddImmediateDebugRenderFeature();

    game.Add3DCamera().AddInteractiveCameraScript();
    //game.Add2DCamera().AddInteractiveCameraScript();

    //game.AddDirectionalLight();
    game.AddAllDirectionLighting(intensity: 50f, true);
    game.AddSkybox();

    // Make sure you also update 2D Ground collider if you are testing this
    //game.Add2DGround();
    game.Add3DGround();

    game.AddGroundGizmo(new(0, 0, -7.5f), showAxisName: true);
    game.AddProfiler();

    //AddSpriteBatchRenderer(rootScene);

    _camera = game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

    CreateShapeModels();

    //var gameSettings = game.Services.GetService<IGameSettingsService>();

    simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
    var processor = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>();

    //simulation.FixedTimeStep = 1f / 90;
    //simulation.ContinuousCollisionDetection = true;

    //Add2DShapes(ShapeType.Square, 1);

    AddBackground();

    DebugDraw = new ImmediateDebugRenderSystem(game.Services, RenderGroup.Group1);
    DebugDraw.PrimitiveColor = Color.Green;
    DebugDraw.MaxPrimitives = (currentNumPrimitives * 2) + 8;
    DebugDraw.MaxPrimitivesWithLifetime = (currentNumPrimitives * 2) + 8;
    DebugDraw.Visible = true;

    // keep DebugText visible in release builds too
    game.DebugTextSystem.Visible = true;
    game.Services.AddService(DebugDraw);
    game.GameSystems.Add(DebugDraw);

    Add3DBoxes(1);
}

void CreateShapeModels()
{
    foreach (var item in shapes)
    {
        item.Model =
        [
            new Mesh
            {
                Draw = CreateMeshDraw(item).ToMeshDraw(game.GraphicsDevice),
                MaterialIndex = 0
            },
            CreateMaterial(game, item.Color)
        ];
    }
}

MeshBuilder CreateMeshDraw(ShapeModel model)
{
    return model.Type switch
    {
        ShapeType.Square or ShapeType.Rectangle => GiveMeShape(model.Size, model.Color),
        ShapeType.Circle => GiveMeCircle(model.Size, 10, model.Color),
        ShapeType.Triangle => GiveMeShape(model.Size, model.Color),
        _ => GiveMeShape(model.Size, model.Color),
    };
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

    if (game.Input.IsKeyPressed(Keys.N))
    {
        Add3DBoxes(10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.M))
    {
        Add2DShapes(ShapeType.Square, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.R))
    {
        Add2DShapes(ShapeType.Rectangle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.C))
    {
        Add2DShapes(ShapeType.Circle, 10);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyPressed(Keys.T))
    {
        Add2DShapes(count: 30);

        SetCubeCount(scene);
    }
    else if (game.Input.IsKeyReleased(Keys.X))
    {
        foreach (var entity in scene.Entities.Where(w => w.Name == "Cube").ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    for (var i = 0; i < cubesList.Count; i++)
    {
        var entity = cubesList[i];

        DebugDraw.DrawCube(entity.Transform.Position, boxSize, entity.Transform.Rotation, Color.Red, depthTest: false, solid: !true);

    }


    RenderNavigation();
}

void ProcessRaycast(MouseButton mouseButton, Vector2 mousePosition)
{
    var hitResult = _camera!.RaycastMouse(simulation!, mousePosition);

    if (hitResult.Succeeded && hitResult.Collider.Entity.Name == "Cube")
    {
        if (mouseButton == MouseButton.Left)
        {
            var rigidBody = hitResult.Collider.Entity.Get<RigidbodyComponent>();

            if (rigidBody != null)
            {
                //var worldPosition = _camera.ScreenToWorldPoint(new Vector3(mousePosition.X, mousePosition.Y, 0));

                // Calculate a target position and apply force or set velocity
                //var direction = worldPosition - rigidBody.Position;
                //direction.Normalize();

                // Apply a force towards the target position
                // or set the velocity directly (more abrupt and less physically realistic)

                var direction = new Vector3(0, 20, 0);

                rigidBody.ApplyImpulse(direction * 10);
                // or
                rigidBody.LinearVelocity = direction * 1;
            }


            Console.WriteLine("Left click");
        }
    }
}

MeshBuilder GiveMeShape(Vector3 size, Color shapeColor)
{
    var meshBuilder = new MeshBuilder();

    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector2>();
    var color = meshBuilder.WithColor<Color>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector2(0, 0));
    meshBuilder.SetElement(color, shapeColor);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector2(0, size.Y));
    meshBuilder.SetElement(color, shapeColor);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector2(size.X, size.Y));
    meshBuilder.SetElement(color, shapeColor);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector2(size.X, 0));
    meshBuilder.SetElement(color, shapeColor);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(1);
    meshBuilder.AddIndex(2);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(3);

    return meshBuilder;
}

MeshBuilder GiveMeCircle(Vector3 size, int segments, Color shapeColor)
{
    var meshBuilder = new MeshBuilder();

    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector2>();
    var color = meshBuilder.WithColor<Color>();

    // Calculate radius based on the size (assuming size.X is diameter)
    float radius = size.X / 2;

    // Add center vertex
    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector2(0, 0));
    meshBuilder.SetElement(color, shapeColor);

    // Add vertices for the circumference
    for (int i = 0; i <= segments; i++)
    {
        // Angle for each segment
        float angle = MathUtil.TwoPi * i / segments;

        // Calculate vertex position on circumference
        float x = radius * MathF.Cos(angle);
        float y = radius * MathF.Sin(angle);

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector2(x, y));
        meshBuilder.SetElement(color, shapeColor);
    }

    // Create triangles
    for (int i = 1; i <= segments; i++)
    {
        meshBuilder.AddIndex(0); // Center vertex
        meshBuilder.AddIndex(i + 1);
        meshBuilder.AddIndex(i);
    }

    return meshBuilder;
}

// Image by brgfx, Free license, Attribution is required: https://www.freepik.com/free-vector/nature-roadside-background-scene_40169781.htm#query=2d%20game%20background&position=13&from_view=keyword&track=ais&uuid=dde78bdc-b045-4f91-b1b8-50f13aef87dc
void AddBackground()
{
    var entity = new Entity("Background");

    var directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    var filePath = Path.Combine(directory, "background.jpg");
    using var input = File.OpenRead(filePath);
    var texture = Texture.Load(game.GraphicsDevice, input);

    var spriteComponent = new SpriteComponent
    {
        SpriteProvider = new SpriteFromTexture { Texture = texture },
    };

    entity.Add(spriteComponent);
    entity.Transform.Position.Z = -500;
    entity.Transform.Position.Y = 12.4f;

    entity.Scene = scene;
}

void Add3DBoxes(int count = 5)
{
    for (int i = 0; i < count; i++)
    {
        //var entity = game.CreatePrimitive(PrimitiveModelType.Cube, size: boxSize, material: game.CreateMaterial(Color.Gold));
        var entity = game.CreatePrimitive(PrimitiveModelType.Cube, size: boxSize);

        entity.Name = "Cube";
        entity.Transform.Position = GetRandomPosition();
        entity.Scene = scene;

        var rigidBody = entity.Get<RigidbodyComponent>();

        //rigidBody.Restitution = 0;
        //rigidBody.Friction = 1;
        //rigidBody.RollingFriction = 0.1f;

        rigidBody.AngularFactor = new Vector3(0, 0, 1);
        rigidBody.LinearFactor = new Vector3(1, 1, 0);

        Vector3 pivot = new Vector3(0, 0, 0);
        Vector3 axis = Vector3.UnitZ;

        //var constrain = Simulation.CreateHingeConstraint(rigidBody, pivot, axis, useReferenceFrameA: false);

        //simulation.AddConstraint(constrain);

        cubesList.Add(entity);
    }
}

void Add2DShapes(ShapeType? type = null, int count = 5)
{
    for (int i = 1; i <= count; i++)
    {
        ShapeModel? shapeModel;

        if (type == null)
        {
            int randomIndex = Random.Shared.Next(shapes.Count);

            shapeModel = shapes[randomIndex];
        }
        else
        {
            shapeModel = shapes.Find(x => x.Type == type);

            if (shapeModel == null) return;
        }

        Create2DShape(shapeModel.Type);
    }
}

void Create2DShape(ShapeType type)
{
    var shapeModel = shapes.FirstOrDefault(x => x.Type == type);

    if (shapeModel == null) return;

    var entity = new Entity
    {
        Scene = scene,
        Name = "Cube",
        Transform = {
                Position = GetRandomPosition(),
                //Rotation = Quaternion.RotationYawPitchRoll(MathUtil.DegreesToRadians(180), 0, 0)
            }
    };

    entity.Add(new ModelComponent { Model = shapeModel.Model });

    //entity.Add(new StaticColliderComponent());

    var rigidBody = new RigidbodyComponent()
    {
        //IsKinematic = false,

        //Restitution = 0,
        //Friction = 1,
        //RollingFriction = 0.1f,

        //CcdMotionThreshold = 100,
        //CcdSweptSphereRadius = 100,
        //Mass = 1000000,
        //LinearDamping = 0.8f,
        //AngularDamping = 1.4f,
        //ColliderShapes = { new BoxColliderShapeDesc() { Size = new Vector3(1), Is2D = true } },
        ColliderShape = (type) switch
        {
            ShapeType.Square => GetBoxColliderShape(shapeModel.Size),
            ShapeType.Rectangle => GetBoxColliderShape(shapeModel.Size),
            ShapeType.Circle => new SphereColliderShape(true, shapeModel.Size.X / 2),
            _ => throw new NotImplementedException(),
        }
    };

    entity.Add(rigidBody);

    rigidBody.AngularFactor = new Vector3(0, 0, 1);
    rigidBody.LinearFactor = new Vector3(1, 1, 0);
}

// Another issue
static void AddSpriteBatchRenderer(Scene rootScene)
{
    var entity = new Entity("SpriteBatchRendererEntity", new(1, 1, 1))
    {
        new SpriteBatchRenderer()
    };

    entity.Scene = rootScene;
}

void RenderNavigation()
{
    game.DebugTextSystem.Print($"Cubes: {cubes}", new Int2(x: debugX, y: debugY));
    game.DebugTextSystem.Print($"X - Delete all cubes and shapes", new Int2(x: debugX, y: debugY + 30), Color.Red);
    game.DebugTextSystem.Print($"N - generate 3D cubes", new Int2(x: debugX, y: debugY + 60));
    game.DebugTextSystem.Print($"M - generate 2D squares", new Int2(x: debugX, y: debugY + 80));
    game.DebugTextSystem.Print($"C - generate 2D circles", new Int2(x: debugX, y: debugY + 100));
    game.DebugTextSystem.Print($"R - generate 2D rectangles", new Int2(x: debugX, y: debugY + 120));
    game.DebugTextSystem.Print($"T - generate random 2D shapes", new Int2(x: debugX, y: debugY + 140));
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Where(w => w.Name == "Cube").Count();

static BoxColliderShapeX4 GetBoxColliderShape(Vector3 size)
    => new(true, new Vector3(size.X, size.Y, 0))
    {
        LocalOffset = new Vector3(size.X / 2, size.Y / 2, 0)
    };

static Material CreateMaterial(Game game, Color color)
{
    var colorVertexStream = new ComputeVertexStreamColor { Stream = new ColorVertexStreamDefinition() };
    var computeColor = new ComputeBinaryColor(new ComputeColor(color), colorVertexStream, BinaryOperator.Multiply);

    return Material.New(game.GraphicsDevice, new MaterialDescriptor
    {
        Attributes = new MaterialAttributes
        {
            // Best colours
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeVertexStreamColor()
            },
            // Better colours
            //Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color)),
            // Worst colours
            //Diffuse = new MaterialDiffuseMapFeature(computeColor),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0)),
            SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.05f)),
            Emissive = new MaterialEmissiveMapFeature(computeColor),
        }
    });
}

static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), 3 + Random.Shared.Next(0, 7), 0);

public enum ShapeType
{
    Square,
    Rectangle,
    Circle,
    Triangle
}

public class ShapeModel
{
    public required ShapeType Type { get; set; }
    public required Color Color { get; set; }
    public required Vector3 Size { get; set; }
    public Model? Model { get; set; }
}