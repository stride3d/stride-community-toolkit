using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace E06_Box2D.Helpers;

/// <summary>
/// The on-screen help and physics readout for the Box2D physics example, drawn by the shared
/// <see cref="TextOverlay"/>: the keys first, the live numbers under them, and the rarer keys in a
/// collapsible section behind Z.
/// </summary>
public class UiHelper
{
    private readonly Box2DSimulation _simulation;
    private int _shapeCount;
    private string? _status;
    private Color _statusColor = Color.White;

    public UiHelper(Game game, Box2DSimulation simulation)
    {
        _simulation = simulation;

        var overlay = TextOverlay.GetOrCreate(game);

        // The callbacks run every frame the overlay is drawn, so the numbers stay live without
        // anything having to push them
        overlay.AddSection("Box2D", BuildOverlayLines);
        overlay.AddCollapsibleSection("Advanced", "More shapes", Keys.Z, AdvancedLines, collapsed: true);
    }

    /// <summary>
    /// Records what the overlay shows this frame. Call it every update; a status message set with
    /// <see cref="SetStatus"/> lasts until the next call.
    /// </summary>
    /// <param name="shapeCount">Current number of physics objects</param>
    public void SetReadout(int shapeCount)
    {
        _shapeCount = shapeCount;
        _status = null;
    }

    /// <summary>
    /// Shows a temporary status message under the readout, for the frame it is called in.
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="color">Color of the message</param>
    public void SetStatus(string message, Color color)
    {
        _status = message;
        _statusColor = color;
    }

    private IReadOnlyList<TextElement> BuildOverlayLines()
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
            new($"Objects {_shapeCount}", Color.LightGreen),
            new($"Gravity {_simulation.Gravity.Y:0.0}, time scale {_simulation.Settings.TimeScale:0.00}", Color.LightGreen),
            new(_simulation.Enabled ? "Simulation enabled" : "Simulation disabled", _simulation.Enabled ? Color.LightGreen : Color.OrangeRed),
        ];

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