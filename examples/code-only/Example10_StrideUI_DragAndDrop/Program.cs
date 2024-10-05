using Example10_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

UIManager? _uiManager = null;
CubesGenerator? _cubesGenerator = null;

const int CubesCount = 100;
const int RemovalThresholdY = -30;
const string TotalCubes = "Total Cubes: ";

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    // Add debugging aids: entity names, positions
    game.AddEntityDebugRenderer(new()
    {
        ShowFontBackground = true
    });

    game.AddSkybox();
    game.AddProfiler();

    _cubesGenerator = new CubesGenerator(game, scene);

    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");

    // Create and display the UI components on screen
    CreateAndAddUI(scene, font);

    // Add an example 3D capsule entity to the scene for visual reference
    AddSampleCapsule(scene);
}

void Update(Scene scene, GameTime time)
{
    foreach (var entity in scene.Entities)
    {
        if (entity.Transform.Position.Y < RemovalThresholdY)
        {
            entity.Scene = null;

            _cubesGenerator?.SubtractTotalCubes(1);

            _uiManager?.UpdateTextBlock($"{TotalCubes} {_cubesGenerator?.TotalCubes ?? 0}");
        }
    }
}

void CreateAndAddUI(Scene scene, SpriteFont font)
{
    _uiManager = new UIManager(font, GenerateRandomCubes);

    _uiManager.Entity.Scene = scene;
}

void AddSampleCapsule(Scene scene)
{
    var entity = game.Create3DPrimitive(PrimitiveModelType.Capsule);

    entity.Transform.Position = new Vector3(0, 8, 0);

    entity.Scene = scene;
}

void GenerateRandomCubes()
{
    var totalCubes = _cubesGenerator?.Generate(CubesCount, PrimitiveModelType.Sphere);

    _uiManager?.UpdateTextBlock($"{TotalCubes} {totalCubes ?? 0}");
}