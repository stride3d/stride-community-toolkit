using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Ranges, ticks, grid and label settings for a <see cref="Chart"/>. Distances are in the chart's own
/// units; scale the chart's root entity to change its size in the world.
/// </summary>
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

    /// <summary>Spacing between tick marks, grid lines and labels on both axes. Defaults to <c>1</c>.</summary>
    public float TickStep { get; set; } = 1f;

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

    /// <summary>Colour of the grid lines. Defaults to a dim grey so curves stand out against it.</summary>
    public Color GridColor { get; set; } = new(90, 90, 110);

    /// <summary>Ribbon width of the grid lines. Defaults to <c>0.012</c>.</summary>
    public float GridWidth { get; set; } = 0.012f;

    /// <summary>Whether the grid is shown when the chart is created. Defaults to <see langword="false"/>; toggle later with <see cref="Chart.GridVisible"/>.</summary>
    public bool GridVisible { get; set; }

    /// <summary>Whether tick labels are created. Defaults to <see langword="true"/>.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>Height of the tick labels in chart units. Defaults to <c>0.3</c>.</summary>
    public float LabelHeight { get; set; } = 0.3f;

    /// <summary>Colour of the tick labels. Defaults to white.</summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>Numeric format for the tick labels. Defaults to <c>"0.##"</c>.</summary>
    public string LabelFormat { get; set; } = "0.##";
}