using Example18_Box2DPhysics.Box2DPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example18_Box2DPhysics.Helpers;

/// <summary>
/// Handles UI display and navigation instructions for the Box2D physics example
/// </summary>
public class UiHelper
{
    private readonly Game _game;
    private readonly NavigationCommand[] _commands;
    private readonly PhysicsInfo _physicsInfo;

    public UiHelper(Game game)
    {
        _game = game;
        _commands = GetNavigationCommands();
        _physicsInfo = new PhysicsInfo();
    }

    /// <summary>
    /// Renders the main navigation UI and physics information
    /// </summary>
    /// <param name="cubeCount">Current number of physics objects</param>
    /// <param name="simulation">The physics simulation for additional stats</param>
    public void RenderNavigation(int? cubeCount = 0, int totalShapesCreated = 0, Box2DSimulation? simulation = null)
    {
        var yOffset = GameConfig.DefaultDebugY;

        // Display title
        _game.DebugTextSystem.Print("Box2D Physics Example",
            new Int2(GameConfig.DefaultDebugX, yOffset), Color.Yellow);
        yOffset += GameConfig.HeaderSpacing;

        // Display physics stats
        RenderPhysicsStats(yOffset, cubeCount, totalShapesCreated, simulation);
        yOffset += GameConfig.HeaderSpacing + (4 * GameConfig.DefaultSpacing);

        // Display controls
        RenderControls(yOffset + 10);
    }

    /// <summary>
    /// Renders physics-related statistics
    /// </summary>
    private void RenderPhysicsStats(int yOffset, int? cubeCount, int totalShapesCreated, Box2DSimulation? simulation)
    {
        _game.DebugTextSystem.Print("Physics Stats:",
            new Int2(GameConfig.DefaultDebugX, yOffset), Color.Cyan);
        yOffset += GameConfig.DefaultSpacing;

        _game.DebugTextSystem.Print($"Objects: {cubeCount}",
            new Int2(GameConfig.DefaultDebugX, yOffset), Color.White);
        yOffset += GameConfig.DefaultSpacing;

        _game.DebugTextSystem.Print($"Total Created: {totalShapesCreated}",
            new Int2(GameConfig.DefaultDebugX, yOffset), Color.White);
        yOffset += GameConfig.DefaultSpacing;

        if (simulation != null)
        {
            _game.DebugTextSystem.Print($"Gravity: {simulation.Gravity}",
                new Int2(GameConfig.DefaultDebugX, yOffset), Color.White);
            yOffset += GameConfig.DefaultSpacing;

            _game.DebugTextSystem.Print($"Time Scale: {simulation.TimeScale:F2}",
                new Int2(GameConfig.DefaultDebugX, yOffset), Color.White);
            yOffset += GameConfig.DefaultSpacing;

            var enabledText = simulation.Enabled ? "Enabled" : "Disabled";
            var enabledColor = simulation.Enabled ? Color.Green : Color.Red;
            _game.DebugTextSystem.Print($"Simulation: {enabledText}",
                new Int2(GameConfig.DefaultDebugX, yOffset), enabledColor);
        }
    }

    /// <summary>
    /// Renders the control instructions
    /// </summary>
    private void RenderControls(int yOffset)
    {
        _game.DebugTextSystem.Print("Controls:",
            new Int2(GameConfig.DefaultDebugX, yOffset), Color.Cyan);
        yOffset += GameConfig.DefaultSpacing;

        foreach (var command in _commands)
        {
            _game.DebugTextSystem.Print(command.Text,
                new Int2(GameConfig.DefaultDebugX, yOffset), command.Color);
            yOffset += GameConfig.DefaultSpacing;
        }
    }

    /// <summary>
    /// Renders temporary status messages
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="color">Color of the message</param>
    public void RenderStatusMessage(string message, Color color = default)
    {
        if (color == default) color = Color.White;

        _game.DebugTextSystem.Print(message,
            new Int2(GameConfig.DefaultDebugX, GameConfig.DefaultDebugY - 50), color);
    }

    /// <summary>
    /// Renders physics debugging information at a specific position
    /// </summary>
    /// <param name="position">Screen position</param>
    /// <param name="info">Information to display</param>
    public void RenderDebugInfo(Int2 position, string info)
    {
        _game.DebugTextSystem.Print(info, position, Color.Yellow);
    }

    private static NavigationCommand[] GetNavigationCommands()
    {
        return new NavigationCommand[]
        {
            new("Left Click - Apply impulse to object", Color.LightGreen),
            new("X - Delete all objects", Color.Red),
            new("", Color.Transparent), // Spacer
            new("Shape Generation:", Color.Yellow),
            new("M - Generate squares", Color.White),
            new("R - Generate rectangles", Color.White),
            new("C - Generate circles", Color.White),
            new("T - Generate triangles", Color.White),
            new("V - Generate capsules", Color.White),
            new("P - Generate random shapes (mass)", Color.White),
            new("", Color.Transparent), // Spacer
            new("Advanced:", Color.Yellow),
            new("J - Generate shapes with joints", Color.White),
            new("G - Generate demo shapes", Color.White),
            new("Space - Toggle physics simulation", Color.White)
        };
    }

    /// <summary>
    /// Represents a navigation command with text and color
    /// </summary>
    private record NavigationCommand(string Text, Color Color);

    /// <summary>
    /// Tracks physics-related information for display
    /// </summary>
    private class PhysicsInfo
    {
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public string LastAction { get; set; } = "None";
        public int TotalObjectsCreated { get; set; } = 0;
        public int TotalObjectsDestroyed { get; set; } = 0;
    }
}