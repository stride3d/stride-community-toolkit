using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Everything a <see cref="Chart"/> is built from, in eight groups: what it shows (<see cref="Range"/>),
/// how it is drawn (<see cref="Axes"/>, <see cref="Grid"/>, <see cref="Labels"/>, <see cref="Legend"/>,
/// <see cref="Title"/>, <see cref="Cursor"/>) and the defaults new series take (<see cref="Series"/>).
/// </summary>
/// <remarks>
/// <para>
/// Start from a preset - <see cref="Light2D"/> for a flat, paper-like chart under an orthographic camera,
/// <see cref="Glow3D"/> for glowing lines in a lit 3D scene - and change what you need:
/// </para>
/// <code>
/// var options = ChartOptions.Light2D();
/// options.Range.XMin = -8f;
/// options.Range.XMax = 8f;
/// options.Title.Text = "Trajectories";
/// </code>
/// <para>
/// The options stay live after the chart is built: <see cref="Chart.Update(Stride.Engine.CameraComponent)"/> applies whatever changed
/// since the last frame, so a range, a visibility, a colour or a glow is changed by writing the option,
/// the same way it was set up. <see cref="Series"/> holds the defaults a series takes when added, and a
/// series whose style left a value unset keeps reading the default, so those are live too. Whether the
/// chart is 3D (a Z spread) is read once, when it is built.
/// </para>
/// <para>
/// Ranges are in the chart's own units; scale the chart's root entity to change its size in the world.
/// Every width, length and glow is in pixels on a 100% display, and stays that size at any zoom or distance.
/// </para>
/// </remarks>
public sealed class ChartOptions
{
    /// <summary>What the chart shows: the bounds of each axis and the spacing of its ticks.</summary>
    public ChartRangeOptions Range { get; set; } = new();

    /// <summary>How the axes and their tick marks are drawn, and what the axes are called.</summary>
    public ChartAxesOptions Axes { get; set; } = new();

    /// <summary>How the grid is drawn, and which coordinate planes carry one.</summary>
    public ChartGridOptions Grid { get; set; } = new();

    /// <summary>How the tick labels are drawn.</summary>
    public ChartLabelOptions Labels { get; set; } = new();

    /// <summary>The legend that names each series.</summary>
    public ChartLegendOptions Legend { get; set; } = new();

    /// <summary>The chart's own title, just inside its top edge.</summary>
    public ChartTitleOptions Title { get; set; } = new();

    /// <summary>The defaults a series takes when it is added without a style of its own.</summary>
    public ChartSeriesOptions Series { get; set; } = new();

    /// <summary>The mouse readout, on while <see cref="ChartCursorOptions.Visible"/> is.</summary>
    public ChartCursorOptions Cursor { get; set; } = new();

    /// <summary>
    /// Glowing lines on a dark, lit 3D scene: the defaults of every group, named here so the two presets read
    /// side by side. Give <see cref="ChartRangeOptions.ZMin"/> and <see cref="ChartRangeOptions.ZMax"/> a
    /// spread to turn the chart 3D.
    /// </summary>
    public static ChartOptions Glow3D() => new();

    /// <summary>
    /// A flat, paper-like chart for an orthographic 2D camera on a light background - no glow, dark axes, a
    /// major and minor grid, slightly heavier curves, and labels that keep their pixel size while zooming.
    /// </summary>
    public static ChartOptions Light2D() => new()
    {
        Range = new ChartRangeOptions { MinorDivisions = 5 },
        Axes = new ChartAxesOptions
        {
            XColor = new Color(40, 40, 40),
            YColor = new Color(40, 40, 40),
            Width = 1.5f,
            TickLength = 8f,
            TickWidth = 1f,
        },
        Cursor = new ChartCursorOptions { Glow = 0f },
        Grid = new ChartGridOptions
        {
            Visible = true,
            Color = new Color(190, 190, 190),
            Width = 1.5f,
            MinorColor = new Color(228, 228, 228),
            MinorWidth = 1f,
        },
        Labels = new ChartLabelOptions
        {
            Mode = ChartLabelMode.Screen,
            FontSize = 16f,
            Color = new Color(60, 60, 60),
        },
        Series = new ChartSeriesOptions
        {
            CurveWidth = 3f,
            Glow = 0f,
            AdditiveGlow = false,
            Palette =
            [
                new Color(45, 112, 179),   // blue
                new Color(199, 68, 64),    // red
                new Color(56, 140, 70),    // green
                new Color(96, 66, 166),    // purple
                new Color(250, 126, 25),   // orange
                new Color(0, 0, 0),        // black
            ],
        },
    };
}