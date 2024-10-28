using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Games;
using Stride.CommunityToolkit.ImGuiDebug;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;

using var game = new Game();

game.Run(start: Start);

void Start(Scene scene)
{
    // Setup the base 3D scene with default lighting, camera, etc.
    game.SetupBase3DScene();

    // Add debugging aids: entity names, positions
    game.AddEntityDebugSceneRenderer(new()
    {
        ShowFontBackground = true
    });

    game.AddSkybox();
    game.AddProfiler();

    new ImGuiSystem(game.Services, game.GraphicsDeviceManager);
    new HierarchyView(game.Services);
    new PerfMonitor(game.Services);
    Inspector.FindFreeInspector(game.Services).Target = game.SceneSystem.SceneInstance;

    // makes the profiling much easier to read.
    game.SetMaxFPS(60);
}
