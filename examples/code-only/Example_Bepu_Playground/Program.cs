using Example.Common;
using Stride.BepuPhysics;
using Stride.BepuPhysics._2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering;

const float Depth = 1;
const string ShapeName = "BepuCube";

var boxSize = new Vector3(0.2f, 0.2f, Depth);
var rectangleSize = new Vector3(0.2f, 0.3f, Depth);
var groundSize = new Vector3(20, 0.5f, 20);
int cubes = 0;
int debugX = 5;
int debugY = 30;
var bgImage = "JumpyJetBackground.jpg";

Model? model = null;
Simulation? _simulation = null;
CameraComponent? _camera = null;
Scene scene = new();
BepuConfiguration? _bepuConfig = null;
int _simulationIndex = 0;
float _maxDistance = 100;

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

using var game = new Game();

game.Run(start: (Action<Scene>?)((Scene rootScene) =>
{
    scene = rootScene;

    game.Window.AllowUserResizing = true;
    game.Window.Title = "2D Example";

    game.SetupBase3DSceneWithBepu();
    //game.SetupBase2DSceneWithBepu();
    game.AddProfiler();
    game.AddAllDirectionLighting(intensity: 5f, true);
    //game.ShowColliders();

    _camera = game.SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Get<CameraComponent>() != null)?.Get<CameraComponent>();

    _simulation = game.SceneSystem.SceneInstance.GetProcessor<PhysicsProcessor>()?.Simulation;
    //simulation.FixedTimeStep = 1f / 90;

    _bepuConfig = game.Services.GetService<BepuConfiguration>();
    //bepuConfig.BepuSimulations[0].SolverSubStep = 5;
    //bepuConfig.BepuSimulations[0].FixedTimeStep = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 150);
    //bepuConfig.BepuSimulations[0] = new BepuSimulation() { SolverSubStep = 1 };

    var simulation2DEntity = new Entity("Simulation2D")
    {
        new Simulation2DComponent()
    };
    simulation2DEntity.Scene = rootScene;
    simulation2DEntity.AddGizmo(game.GraphicsDevice, showAxisName: true);

}), update: Update);

void Update(Scene scene, GameTime time)
{
    if (!game.Input.HasKeyboard) return;

    if (game.Input.IsMouseButtonDown(MouseButton.Left))
    {
        ProcessRaycast(MouseButton.Left, game.Input.MousePosition);
    }

    if (game.Input.IsKeyDown(Keys.LeftShift))
    {
        if (game.Input.IsKeyPressed(Keys.M))
        {
            Add3DShapes(PrimitiveModelType.Cube, 10);

            SetCubeCount(scene);
        }
        else if (game.Input.IsKeyPressed(Keys.R))
        {
            Add3DShapes(PrimitiveModelType.RectangularPrism, 10);

            SetCubeCount(scene);
        }
        else if (game.Input.IsKeyPressed(Keys.C))
        {
            Add3DShapes(PrimitiveModelType.Cylinder, 10);

            SetCubeCount(scene);
        }
        else if (game.Input.IsKeyPressed(Keys.T))
        {
            Add3DShapes(PrimitiveModelType.TriangularPrism, 10);

            SetCubeCount(scene);
        }
        else if (game.Input.IsKeyPressed(Keys.P))
        {
            Add3DShapes(count: 30);

            SetCubeCount(scene);
        }

        RenderNavigation();

        return;
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
        foreach (var entity in scene.Entities.Where(w => w.Name == "BepuCube" || w.Name == "Cube").ToList())
        {
            entity.Remove();
        }

        SetCubeCount(scene);
    }

    RenderNavigation();
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
    game.DebugTextSystem.Print($"P - Generate random 2D shapes", new Int2(x: debugX, y: debugY + space));
}

void ProcessRaycast(MouseButton mouseButton, Vector2 screenPosition)
{
    if (_bepuConfig == null) return;

    if (mouseButton == MouseButton.Left)
    {
        var invertedMatrix = Matrix.Invert(_camera.ViewProjectionMatrix);

        Vector3 position;
        position.X = screenPosition.X * 2f - 1f;
        position.Y = 1f - screenPosition.Y * 2f;
        position.Z = 0f;

        var vectorNear = Vector3.Transform(position, invertedMatrix);
        vectorNear /= vectorNear.W;

        position.Z = 1f;

        var vectorFar = Vector3.Transform(position, invertedMatrix);
        vectorFar /= vectorFar.W;

        var buffer = System.Buffers.ArrayPool<HitInfo>.Shared.Rent(100);

        _bepuConfig.BepuSimulations[_simulationIndex].RaycastPenetrating(vectorNear.XYZ(), vectorFar.XYZ() - vectorNear.XYZ(), _maxDistance, buffer, out var hits);

        if (hits.Length > 0)
        {
            var space = 0;

            for (int j = 0; j < hits.Length; j++)
            {
                var hitInfo = hits[j];

                if (hitInfo.Container.Entity.Name == ShapeName)
                {
                    game.DebugTextSystem.Print($"Hit! Distance : {hitInfo.Distance}  |  normal : {hitInfo.Normal}  |  Entity : {hitInfo.Container.Entity}", new Int2(x: debugX, y: 200 + space));

                    space += 20;

                    var body2D = hitInfo.Container.Entity.Get<BodyComponent>();

                    if (body2D == null) continue;

                    var direction = new Vector3(0, 20, 0);

                    body2D.ApplyImpulse(direction * 10, new());
                    body2D.LinearVelocity = direction * 1;
                }
            }
        }
        else
        {
            game.DebugTextSystem.Print("No raycast hit", new Int2(x: debugX, y: 200));
        }
    }
}

void Add2DShapes(Primitive2DModelType? type = null, int count = 5)
{
    //var entity = new Entity();

    for (int i = 1; i <= count; i++)
    {
        var shapeModel = Get2DShape(type);

        if (shapeModel == null) return;

        var entity = game.Create2DPrimitiveWithBepu(shapeModel.Type,
            new()
            {
                Size = shapeModel.Size,
                Material = game.CreateMaterial(shapeModel.Color)
            });

        //if (type == null || i == 1)
        //{
        //    entity = game.Create2DPrimitiveWithBepu(shapeModel.Type, new() { Size = shapeModel.Size, Material = game.CreateMaterial(shapeModel.Color) });
        //}
        //else
        //{
        //    entity = entity.Clone();
        //}

        entity.Name = ShapeName;
        entity.Transform.Position = GetRandomPosition();
        entity.Scene = scene;
    }
}

void Add3DShapes(PrimitiveModelType? type = null, int count = 5)
{
    //var entity = new Entity();

    for (int i = 1; i <= count; i++)
    {
        var shapeModel = Get3DShape(type);

        if (shapeModel == null) return;

        var entity = game.Create3DPrimitiveWithBepu(shapeModel.Type,
            new()
            {
                Size = shapeModel.Size,
                Material = game.CreateMaterial(shapeModel.Color)
            });

        //if (type == null || i == 1)
        //{
        //    entity = game.Create2DPrimitiveWithBepu(shapeModel.Type, new() { Size = shapeModel.Size, Material = game.CreateMaterial(shapeModel.Color) });
        //}
        //else
        //{
        //    entity = entity.Clone();
        //}

        entity.Name = ShapeName;
        entity.Transform.Position = Get3DRandomPosition();
        entity.Scene = scene;
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

Shape3DModel? Get3DShape(PrimitiveModelType? type = null)
{
    if (type == null)
    {
        int randomIndex = Random.Shared.Next(_3DShapes.Count);

        return _3DShapes[randomIndex];
    }

    return _3DShapes.Find(x => x.Type == type);
}

void SetCubeCount(Scene scene) => cubes = scene.Entities.Where(w => w.Name == "BepuCube" || w.Name == "Cube").Count();

static Vector3 GetRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), 0);

static Vector3 Get3DRandomPosition() => new(Random.Shared.Next(-5, 5), Random.Shared.Next(10, 30), Random.Shared.Next(-5, 5));