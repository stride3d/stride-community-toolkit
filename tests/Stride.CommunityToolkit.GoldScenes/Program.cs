using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.GoldScenes.Scenes;
using Stride.CommunityToolkit.Rendering;
using Stride.Engine;
using Stride.Games;

// The scenes build/gold-images.cs photographs and compares against tests/gold. Each one pins a
// feature area with a fixed layout and nothing random, so a changed capture means a changed
// renderer, never a changed scene. Run one by name:
//
//   dotnet run --project tests/Stride.CommunityToolkit.GoldScenes -- --scene shapes-2d
//
// The list of scenes and their capture frames is tests/gold/scenes.jsonc; a scene added here is
// added there too. The examples stay free to change; these do not, except to pin something new.

var name = args.Length >= 2 && args[0] == "--scene" ? args[1] : null;

IGoldScene? scene = name switch
{
    "shapes-2d" => new Shapes2DScene(),
    "shapes-3d" => new Shapes3DScene(),
    "text" => new TextScene(),
    "debug-shapes" => new DebugShapesScene(),
    "imgui" => new ImGuiScene(),
    "shapes-textured" => new TexturedShapesScene(),
    "render-texture" => new RenderTextureScene(),
    "shapes-screen" => new ScreenShapesScene(),
    "picking" => new PickingScene(),
    "particles" => new ParticlesScene(),
    "easing" => new EasingScene(),
    "easing-basics" => new EasingBasicsScene(),
    _ => null,
};

if (scene is null)
{
    Console.Error.WriteLine("Usage: --scene shapes-2d | shapes-3d | text | debug-shapes | imgui | shapes-textured | render-texture | shapes-screen | picking | particles | easing | easing-basics");
    return 2;
}

using var game = new Game();

game.Run(start: Start, update: Update);

return 0;

void Start(Scene rootScene)
{
    game.Window.Title = $"Gold scene: {name}";

    // Every pixel width, font size and overlay in the toolkit follows the display scale; pinned to
    // 100% so a capture is the same image on a 150% desktop and on a runner
    DisplayScale.GetOrCreate(game).Override = 1f;

    scene.Start(game, rootScene);
}

void Update(Scene rootScene, GameTime time) => scene.Update(game, rootScene, time);