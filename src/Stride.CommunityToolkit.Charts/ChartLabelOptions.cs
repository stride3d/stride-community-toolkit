using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// How the tick labels are drawn.
/// </summary>
/// <remarks>
/// <see cref="Mode"/>, <see cref="Height"/>, <see cref="FontSize"/> and <see cref="Color"/> are read when a
/// label is created, as the chart is built or when a wider range needs more labels; a change afterwards does
/// not reach the labels that exist. <see cref="Visible"/> and <see cref="Format"/> are applied the next time
/// the labels are laid out, which is the next change of range.
/// </remarks>
public sealed class ChartLabelOptions
{
    /// <summary>Whether tick labels are created. Applied on the next change of range. Defaults to <see langword="true"/>.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Whether labels scale with the chart or keep a pixel size. Read when a label is created. Defaults to <see cref="ChartLabelMode.World"/>.</summary>
    public ChartLabelMode Mode { get; set; } = ChartLabelMode.World;

    /// <summary>Height of the tick labels in chart units, used in <see cref="ChartLabelMode.World"/>. Read when a label is created. Defaults to <c>0.3</c>.</summary>
    public float Height { get; set; } = 0.3f;

    /// <summary>Font size of the tick labels in pixels, used in <see cref="ChartLabelMode.Screen"/>. Read when a label is created. Defaults to <c>16</c>.</summary>
    public float FontSize { get; set; } = 16f;

    /// <summary>Colour of the tick labels. Read when a label is created. Defaults to white.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Numeric format for the tick labels. Applied on the next change of range. Defaults to <c>"0.##"</c>.</summary>
    public string Format { get; set; } = "0.##";
}