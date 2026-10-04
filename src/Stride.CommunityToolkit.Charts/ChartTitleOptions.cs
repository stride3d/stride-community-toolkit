namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The chart's own title, centred just inside its top edge, in the label style.
/// </summary>
public sealed class ChartTitleOptions
{
    /// <summary>The title text. <see langword="null"/> or empty for none.</summary>
    public string? Text { get; set; }

    /// <summary>Font size in pixels, used in <see cref="ChartLabelMode.Screen"/>. Defaults to <c>22</c>.</summary>
    public float FontSize { get; set; } = 22f;

    /// <summary>Height in chart units, used in <see cref="ChartLabelMode.World"/>. Defaults to <c>0.5</c>.</summary>
    public float Height { get; set; } = 0.5f;
}