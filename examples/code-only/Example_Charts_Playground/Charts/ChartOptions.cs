using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// How tick labels are drawn.
/// </summary>
public enum ChartLabelMode
{
    /// <summary>
    /// Labels are world-space text that scales with the chart, so it grows and shrinks with distance and zoom.
    /// The right choice when the chart sits in a 3D scene and is looked at from various distances.
    /// </summary>
    World,

    /// <summary>
    /// Labels are screen-space text of a fixed pixel size that follows its tick. The right choice for a flat 2D
    /// chart with an orthographic camera, where labels should stay readable at every zoom level.
    /// </summary>
    Screen,
}

/// <summary>
/// Ranges, ticks, grid, label and curve settings for a <see cref="Chart"/>. Distances are in the chart's own
/// units; scale the chart's root entity to change its size in the world.
/// </summary>
/// <remarks>
/// Start from a preset - <see cref="Light2D"/> for a flat, paper-like chart under an orthographic camera,
/// <see cref="Glow3D"/> for glowing lines in a lit 3D scene - and change what you need.
/// </remarks>
public sealed class ChartOptions
{
    /// <summary>The smallest <c>x</c> shown. Defaults to <c>-5</c>.</summary>
    public float XMin { get; set; } = -5f;

    /// <summary>The largest <c>x</c> shown. Defaults to <c>5</c>.</summary>
    public float XMax { get; set; } = 5f;

    /// <summary>The smallest <c>y</c> shown. Defaults to <c>-5</c>.</summary>
    public float YMin { get; set; } = -5f;

    /// <summary>The largest <c>y</c> shown. Defaults to <c>5</c>.</summary>
    public float YMax { get; set; } = 5f;

    /// <summary>Spacing between tick marks, major grid lines and labels on both axes. Defaults to <c>1</c>.</summary>
    public float TickStep { get; set; } = 1f;

    /// <summary>
    /// How many minor grid cells fit in one <see cref="TickStep"/>; <c>0</c> or <c>1</c> means no minor grid.
    /// Defaults to <c>0</c>.
    /// </summary>
    public int MinorDivisions { get; set; }

    /// <summary>Colour of the <c>x</c> axis. Defaults to red.</summary>
    public Color XAxisColor { get; set; } = Color.Red;

    /// <summary>Colour of the <c>y</c> axis. Defaults to lime green.</summary>
    public Color YAxisColor { get; set; } = Color.LimeGreen;

    /// <summary>Ribbon width of the axes. Defaults to <c>0.03</c>.</summary>
    public float AxisWidth { get; set; } = 0.03f;

    /// <summary>Length of each tick mark, centred on its axis. Defaults to <c>0.15</c>.</summary>
    public float TickLength { get; set; } = 0.15f;

    /// <summary>Ribbon width of the tick marks. Defaults to <c>0.02</c>.</summary>
    public float TickWidth { get; set; } = 0.02f;

    /// <summary>Colour of the major grid lines. Defaults to a dim grey so curves stand out against it.</summary>
    public Color GridColor { get; set; } = new(90, 90, 110);

    /// <summary>Ribbon width of the major grid lines. Defaults to <c>0.012</c>.</summary>
    public float GridWidth { get; set; } = 0.012f;

    /// <summary>Colour of the minor grid lines. Defaults to a fainter grey than <see cref="GridColor"/>.</summary>
    public Color MinorGridColor { get; set; } = new(60, 60, 75);

    /// <summary>Ribbon width of the minor grid lines. Defaults to <c>0.008</c>.</summary>
    public float MinorGridWidth { get; set; } = 0.008f;

    /// <summary>Whether the grid is shown when the chart is created. Defaults to <see langword="false"/>; toggle later with <see cref="Chart.GridVisible"/>.</summary>
    public bool GridVisible { get; set; }

    /// <summary>Whether tick labels are created. Defaults to <see langword="true"/>.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Whether labels scale with the chart or keep a pixel size. Defaults to <see cref="ChartLabelMode.World"/>.</summary>
    public ChartLabelMode LabelMode { get; set; } = ChartLabelMode.World;

    /// <summary>Height of the tick labels in chart units, used in <see cref="ChartLabelMode.World"/>. Defaults to <c>0.3</c>.</summary>
    public float LabelHeight { get; set; } = 0.3f;

    /// <summary>Font size of the tick labels in pixels, used in <see cref="ChartLabelMode.Screen"/>. Defaults to <c>16</c>.</summary>
    public float LabelFontSize { get; set; } = 16f;

    /// <summary>Colour of the tick labels. Defaults to white.</summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>Numeric format for the tick labels. Defaults to <c>"0.##"</c>.</summary>
    public string LabelFormat { get; set; } = "0.##";

    /// <summary>Ribbon width used for curves added without explicit options. Defaults to <c>0.06</c>.</summary>
    public float CurveWidth { get; set; } = 0.06f;

    /// <summary>Emissive intensity used for curves added without explicit options; above <c>1</c> glows when bloom is on. Defaults to <c>2.5</c>.</summary>
    public float CurveEmissiveIntensity { get; set; } = 2.5f;

    /// <summary>Colours handed out in turn to curves added without explicit options.</summary>
    public Color[] CurvePalette { get; set; } =
    [
        Color.Cyan, Color.Orange, Color.Magenta, Color.Yellow, Color.LightGreen, Color.HotPink, Color.DeepSkyBlue,
    ];

    /// <summary>
    /// Glowing lines on a dark, lit 3D scene: the defaults of every property, listed here so the two presets read side by side.
    /// </summary>
    public static ChartOptions Glow3D() => new();

    /// <summary>
    /// A flat, paper-like chart for an orthographic 2D camera on a light background - no glow, dark axes, a
    /// major and minor grid, and labels that keep their pixel size while zooming. Widths are chosen for the
    /// 2D controller's default orthographic size of 10 on a window around 720 pixels tall, with MSAA on.
    /// </summary>
    public static ChartOptions Light2D() => new()
    {
        XAxisColor = new Color(40, 40, 40),
        YAxisColor = new Color(40, 40, 40),
        AxisWidth = 0.035f,
        TickLength = 0.18f,
        TickWidth = 0.025f,
        GridColor = new Color(190, 190, 190),
        GridWidth = 0.02f,
        MinorGridColor = new Color(228, 228, 228),
        MinorGridWidth = 0.015f,
        MinorDivisions = 5,
        GridVisible = true,
        LabelMode = ChartLabelMode.Screen,
        LabelFontSize = 16f,
        LabelColor = new Color(60, 60, 60),
        CurveWidth = 0.045f,
        CurveEmissiveIntensity = 1f,
        CurvePalette =
        [
            new Color(45, 112, 179),   // blue
            new Color(199, 68, 64),    // red
            new Color(56, 140, 70),    // green
            new Color(96, 66, 166),    // purple
            new Color(250, 126, 25),   // orange
            new Color(0, 0, 0),        // black
        ],
    };
}
