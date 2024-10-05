using Example10_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

UIManager? _uiManager = null;
CubesGenerator? _cubesGenerator = null;

const int _cubesCount = 100;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    // Add debugging aids: entity names, positions
    game.AddEntityDebugRenderer();

    game.AddSkybox();
    game.AddProfiler();

    _cubesGenerator = new CubesGenerator(game, scene);

    var font = game.Content.Load<SpriteFont>("StrideDefaultFont");

    // Create and display the UI components on screen
    CreateAndAddUI(scene, font);

    // Add an example 3D capsule entity to the scene for visual reference
    AddSampleCapsule(scene);
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
    var totalCubes = _cubesGenerator?.Generate(_cubesCount, PrimitiveModelType.Sphere);

    _uiManager?.UpdateTextBlock($"Total Cubes: {totalCubes ?? 0}");
}