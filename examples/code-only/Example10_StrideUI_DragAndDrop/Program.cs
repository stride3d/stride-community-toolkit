using Example10_StrideUI_DragAndDrop;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

SpriteFont? _font;
UIManager? _uiManager = null;
CubesGenerator? _cubesGenerator = null;

int _cubesCount = 100;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    // Setup the base 3D scene with default settings
    game.SetupBase3DScene();

    // Add visual aids and debug rendering
    game.AddEntityDebugRenderer();

    game.AddSkybox();
    game.AddProfiler();

    _cubesGenerator = new CubesGenerator(game, scene);

    LoadRequiredFont();

    CreateAndAddUI(scene);

    AddSampleCapsule(scene);
}

void LoadRequiredFont()
{
    _font = game.Content.Load<SpriteFont>("StrideDefaultFont");
}

void CreateAndAddUI(Scene scene)
{
    _uiManager = new UIManager(_font, GenerateRandomCubes);

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
    var totalCubes = _cubesGenerator?.GenerateRandomCubes(_cubesCount);

    _uiManager?.UpdateTextBlock($"Total Cubes: {totalCubes ?? 0}");
}