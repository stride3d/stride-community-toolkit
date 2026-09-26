using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace E06_Box2D.Helpers;

/// <summary>
/// The on-screen help and physics readout for the Box2D physics example, drawn by the shared
/// <see cref="DebugOverlay"/>: the keys first, the live numbers under them, and the rarer keys in a
/// collapsible section behind Z.
/// </summary>
public class UiHelper
{
    private int? _shapeCount;
    private int _totalCreated;
    private Box2DSimulation? _simulation;
    private string? _status;
    private Color _statusColor = Color.White;

    public UiHelper(Game game)
    {
        var overlay = DebugOverlay.GetOrCreate(game);

        // The callbacks run every frame the overlay is drawn, so the numbers stay live without
        // anything having to push them
        overlay.AddSection("Box2D", BuildLines);
        overlay.AddCollapsibleSection("Advanced", "More shapes", Keys.Z, AdvancedLines, collapsed: true);
    }

    /// <summary>
    /// Records what the overlay shows this frame. Call it every update; a status message set with
    /// <see cref="RenderStatusMessage"/> lasts until the next call.
    /// </summary>
    /// <param name="cubeCount">Current number of physics objects</param>
    /// <param name="totalShapesCreated">How many shapes have been spawned since the start</param>
    /// <param name="simulation">The physics simulation for additional stats</param>
    public void RenderNavigation(int? cubeCount = 0, int totalShapesCreated = 0, Box2DSimulation? simulation = null)
    {
        _shapeCount = cubeCount;
        _totalCreated = totalShapesCreated;
        _simulation = simulation;
        _status = null;
    }

    /// <summary>
    /// Shows a temporary status message under the readout, for the frame it is called in.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="color">Color of the message</param>
    public void RenderStatusMessage(string message, Color color = default)
    {
        _status = message;
        _statusColor = color == default ? Color.White : color;
    }

    private IReadOnlyList<TextElement> BuildLines()
    {
        List<TextElement> lines =
        [
            new("Left mouse", "Pick up and throw; empty space spawns", Color.Gold),
            new("X", "Delete all objects", Color.Gold),
            new("M", "Generate squares", Color.Gold),
            new("R", "Generate rectangles", Color.Gold),
            new("C", "Generate circles", Color.Gold),
            new("T", "Generate triangles", Color.Gold),
            new("V", "Generate capsules", Color.Gold),
            new("P", "Generate random shapes with mass", Color.Gold),
            new(""),
            new($"Objects {_shapeCount}, total created {_totalCreated}", Color.LightGreen),
        ];

        if (_simulation is { } simulation)
        {
            lines.Add(new($"Gravity {simulation.Gravity.Y:0.0}, time scale {simulation.TimeScale:0.00}", Color.LightGreen));
            lines.Add(new(simulation.Enabled ? "Simulation enabled" : "Simulation disabled", simulation.Enabled ? Color.LightGreen : Color.OrangeRed));
        }

        if (_status is not null) lines.Add(new(_status, _statusColor));

        return lines;
    }

    private static IReadOnlyList<TextElement> AdvancedLines() =>
    [
        new("J", "Generate shapes with joints", Color.Gold),
        new("G", "Generate demo shapes", Color.Gold),
        new("Space", "Toggle the physics simulation", Color.Gold),
    ];
}