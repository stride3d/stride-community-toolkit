using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

const string EntityName = "PrimitiveModelGroup";

var size1 = new Vector3(0.5f);
var size2 = new Vector3(0.25f, 0.5f, 0.25f);
DebugTextPrinter? instructions = null;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    InitializeDebugTextPrinter();
    Add3DPrimitives(scene);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R))
    {
        ResetTheScene(scene);
        Add3DPrimitives(scene);
    }

    DisplayInstructions();
}

void Add3DPrimitives(Scene scene)
{
    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube,
        new() { Size = size1 });
    cube.Transform.Position = new Vector3(-4f, 0.5f, 0);
    cube.Add(new DebugRenderComponentScript());
    cube.Add(new CollidableGizmoScript()
    {
        Color = new Color4(0.4f, 0.843f, 0, 0.9f),
        Visible = false
    });
    cube.Name = EntityName;
    cube.Scene = scene;

    var cuboid = game.Create3DPrimitive(PrimitiveModelType.Cube,
    new() { Size = size2 });
    cuboid.Transform.Position = new Vector3(-2.2f, 0.5f, -4f);
    cuboid.Name = EntityName;
    cuboid.Scene = scene;

    var cone = game.Create3DPrimitive(PrimitiveModelType.Cone,
        new() { Size = new(0.5f, 3, 0) });
    cone.Transform.Position = new Vector3(0, 2, 0);
    cone.Name = EntityName;
    cone.Scene = scene;

    var capsule = game.Create3DPrimitive(PrimitiveModelType.Capsule,
        new() { Size = size2 });
    capsule.Transform.Position = new Vector3(0.01f, 6, 0);
    capsule.Name = EntityName;
    capsule.Scene = scene;

    var sphere = game.Create3DPrimitive(PrimitiveModelType.Sphere,
        new() { Size = size2 });
    sphere.Transform.Position = new Vector3(0, 8, 0);
    sphere.Name = EntityName;
    sphere.Scene = scene;

    var cylinder = game.Create3DPrimitive(PrimitiveModelType.Cylinder,
        new() { Size = size2 });
    cylinder.Transform.Position = new Vector3(0.5f, 10, 0);
    cylinder.Name = EntityName;
    cylinder.Scene = scene;

    var teapot = game.Create3DPrimitive(PrimitiveModelType.Teapot);
    teapot.Transform.Position = new Vector3(3, 4f, 0);
    teapot.Name = EntityName;
    teapot.Scene = scene;

    var torus = game.Create3DPrimitive(PrimitiveModelType.Torus);
    torus.Transform.Position = new Vector3(0, 12, 0);
    torus.Name = EntityName;
    torus.Scene = scene;

    var triangularPrism1 = game.Create3DPrimitive(PrimitiveModelType.TriangularPrism);
    triangularPrism1.Transform.Position = new Vector3(-8.0f, 2, -3.0f);
    triangularPrism1.Transform.Rotation = Quaternion.RotationY(75);
    triangularPrism1.Name = EntityName;
    triangularPrism1.Scene = scene;

    var triangularPrism2 = game.Create3DPrimitive(PrimitiveModelType.TriangularPrism);
    triangularPrism2.Transform.Position = new Vector3(-3.5f, 0.5f, 1);
    triangularPrism2.Name = EntityName;
    triangularPrism2.Scene = scene;
}

static void ResetTheScene(Scene scene)
{
    var entities = scene.Entities.Where(e => e.Name == EntityName).ToList();

    foreach (var entity in entities)
    {
        entity.Scene = null;
    }
}

void DisplayInstructions() => instructions?.Print();

void InitializeDebugTextPrinter()
{
    var screenSize = new Int2(game.GraphicsDevice.Presenter.BackBuffer.Width, game.GraphicsDevice.Presenter.BackBuffer.Height);

    instructions = new DebugTextPrinter()
    {
        DebugTextSystem = game.DebugTextSystem,
        TextSize = new(205, 17 * 4),
        ScreenSize = screenSize,
        Instructions = [
            new("INSTRUCTIONS"),
            new("Press P to see collidables"),
            new("Press F11 to see debug meshes"),
            new("Press R to reset the scene", Color.Yellow),
        ]
    };

    instructions.Initialize(DisplayPosition.BottomLeft);
}