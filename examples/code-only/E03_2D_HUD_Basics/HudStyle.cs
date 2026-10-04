using Stride.Core.Mathematics;

namespace E03_2D_HUD_Basics;

/// <summary>
/// The sizes and colours of the HUD, in one place. Sizes are in pixels on a 100% display.
/// </summary>
/// <remarks>
/// These were loose values at the top of <c>Program.cs</c>. As properties of one object they can be
/// handed to the HUD, and a second HUD can be given another look by passing another style.
/// </remarks>
public sealed class HudStyle
{
    public float Margin { get; init; } = 24f;

    public Vector2 PanelSize { get; init; } = new(300f, 100f);

    public Color PanelFill { get; init; } = new(12, 18, 32);

    public Color PanelEdge { get; init; } = new(120, 200, 255);

    public Vector2 BarSize { get; init; } = new(252f, 20f);

    public Color TrackEdge { get; init; } = new(90, 110, 140);

    public Color Healthy { get; init; } = new(110, 200, 110);

    public Color Hurt { get; init; } = new(235, 100, 80);

    /// <summary>The health at and below which the bar turns red and the warning panel comes on.</summary>
    public float LowHealth { get; init; } = 0.3f;

    public float DialRadius { get; init; } = 40f;

    public float DialWidth { get; init; } = 10f;

    public Color DialTrack { get; init; } = new(60, 70, 90);

    public Color Energy { get; init; } = new(240, 150, 60);

    public Vector2 WarningSize { get; init; } = new(160f, 40f);
}