using Example10_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

UIManager? _uiManager = null;
PrimitiveGenerator? _shapeGenerator = null;

const int ShapeCount = 100;
const int RemovalThresholdY = -30;
const string TotalCubes = "Total Shapes: ";

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    // Add debugging aids: entity names, positions
    game.AddEntityDebugSceneRenderer(new()
    {
        EnableBackground = true
    });

    game.AddSkybox();
    game.AddProfiler();

    _shapeGenerator = new PrimitiveGenerator(game, scene);

    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");

    // Create and display the UI components on screen
    CreateAndAddUI(scene, font);

    // Add an example 3D capsule entity to the scene for visual reference
    AddExampleShape(scene);
}

void Update(Scene scene, GameTime time)
{
    foreach (var entity in scene.Entities)
    {
        if (entity.Transform.Position.Y < RemovalThresholdY)
        {
            entity.Scene = null;

            _shapeGenerator?.SubtractTotalCubes(1);

            _uiManager?.UpdateTextBlock($"{TotalCubes} {_shapeGenerator?.TotalShapes ?? 0}");
        }
    }
}

void CreateAndAddUI(Scene scene, SpriteFont font)
{
    _uiManager = new UIManager(font, GenerateRandomSpheres);

    _uiManager.Entity.Scene = scene;
}

void AddExampleShape(Scene scene)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;
}

void GenerateRandomSpheres()
{
    var totalShapes = _shapeGenerator?.Generate(ShapeCount, PrimitiveModelType.Sphere);

    _uiManager?.UpdateTextBlock($"{TotalCubes} {totalShapes ?? 0}");
}