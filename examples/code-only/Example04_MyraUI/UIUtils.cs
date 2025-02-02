using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace Example04_MyraUI;

/// <summary>
/// Utility class to create custom UI widgets.
/// </summary>
public static class UIUtils
{
    /// <summary>
    /// Creates a new <see cref="HorizontalProgressBar"/> with the given top position and filler color.
    /// </summary>
    /// <param name="top">The top position of the progress bar.</param>
    /// <param name="filler">The filler color of the progress bar.</param>
    /// <returns>A new <see cref="HorizontalProgressBar"/> instance.</returns>
    public static HorizontalProgressBar CreateHealthBar(int top, string filler)
    {
        return new HorizontalProgressBar
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Value = 100,
            Filler = new SolidBrush(filler),
            Left = 20,
            Top = top,
            Width = 300,
            Height = 20,
            Background = new SolidBrush("#202020FF")
        };
    }
}