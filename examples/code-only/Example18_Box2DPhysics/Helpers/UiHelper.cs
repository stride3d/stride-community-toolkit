using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics.Helpers;

public class UiHelper(Game game)
{
    private readonly (string, Color)[] _commands = GetNavigationCommands();

    public void RenderNavigation(int? cubeCount = 0)
    {
        var space = 0;
        game.DebugTextSystem.Print($"Cubes: {cubeCount}",
            new Int2(x: GameConfig.DefaultDebugX, y: GameConfig.DefaultDebugY));
        space += GameConfig.HeaderSpacing;

        foreach (var (text, color) in _commands)
        {
            game.DebugTextSystem.Print(text, new Int2(x: GameConfig.DefaultDebugX, y: GameConfig.DefaultDebugY + space),
                color);
            space += GameConfig.DefaultSpacing;
        }
    }

    private static (string, Color)[] GetNavigationCommands() =>
    [
        ("X - Delete all cubes and shapes", Color.Red),
        ("M - Generate 2D squares", Color.White),
        ("R - Generate 2D rectangles", Color.White),
        ("C - Generate 2D circles", Color.White),
        ("T - Generate 2D triangles", Color.White),
        ("V - Generate 2D capsules", Color.White),
        ("J - Generate random shapes with constraint", Color.White),
        ("P - Generate random shapes", Color.White)
    ];
}