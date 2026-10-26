using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The chart's legend: one colour swatch and name per series, stacked in the top left corner. The names
/// are text entities rebuilt whenever the series change; the swatches are drawn every frame, and the whole
/// block is laid out in pixels each frame too, so it keeps its size on screen at any zoom or distance.
/// </summary>
/// <param name="game">The game the names' text renderer is registered with.</param>
/// <param name="chart">The chart whose series the legend lists.</param>
internal sealed class ChartLegend(IGame game, Chart chart) : IDisposable
{
    // The layout, in pixels: the inset from the chart's corner, the swatch, the gap before the name, and
    // the row pitch as a multiple of the text height
    private const float Padding = 16f;
    private const float SwatchLength = 28f;
    private const float SwatchWidth = 2f;
    private const float Gap = 8f;
    private const float RowPitch = 1.6f;

    private readonly List<Row> _rows = [];
    private bool _visible = true;

    /// <summary>Shows or hides the legend without rebuilding it.</summary>
    internal void SetVisible(bool visible)
    {
        _visible = visible;
        Apply();
    }

    /// <summary>
    /// Tears the names down and builds them again from the chart's current series list. Positions are
    /// left to <see cref="Draw"/>, which lays the rows out every frame.
    /// </summary>
    internal void Rebuild()
    {
        Teardown();

        if (chart.Series.Count == 0)
            return;

        var o = chart.Options;

        ChartText.EnsureRenderer(game, o.Labels.Mode);

        foreach (var series in chart.Series)
        {
            var label = new ChartText(o.Labels, $"Legend {series.Name}");
            label.Set(series.Name, TextAnchor.MiddleLeft, Vector2.Zero);
            chart.Root.AddChild(label.Entity);
            _rows.Add(new Row(series, label));
        }

        Apply();
    }

    /// <summary>
    /// Lays the rows out for this frame's view and submits the swatches. The layout is measured in pixels
    /// at the chart's top left corner and converted once, so under a perspective camera the legend is
    /// sized for the depth of that corner.
    /// </summary>
    internal void Draw(ShapeBatch batch, in ChartView view)
    {
        if (!_visible || _rows.Count == 0)
            return;

        var o = chart.Options;
        var z = 3f * Chart.LayerStep;
        var corner = new Vector3(o.Range.XMin, o.Range.YMax, z);
        var unitsPerPixel = view.UnitsPerPixel(corner);

        // World text is already in chart units; screen text is a font size in pixels
        var textHeight = o.Labels.Mode == ChartLabelMode.Screen ? o.Labels.FontSize * unitsPerPixel : o.Labels.Height;
        var rowStep = textHeight * RowPitch;
        var x = o.Range.XMin + Padding * unitsPerPixel;
        var top = o.Range.YMax - Padding * unitsPerPixel - textHeight * 0.5f;

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var y = top - i * rowStep;
            var start = new Vector3(x, y, z);
            var end = new Vector3(x + SwatchLength * unitsPerPixel, y, z);

            if (row.Series is ChartMarkerSeries markers)
            {
                // A scatter series is represented by its own glyph, at the swatch's midpoint
                markers.DrawGlyph(batch, in view, (start + end) * 0.5f);
            }
            else
            {
                batch.DrawPixelLine(view.ToWorld(start), view.ToWorld(end), SwatchWidth, row.Series.Color);
            }

            row.Label.Position = new Vector3(end.X + Gap * unitsPerPixel, y, z);
        }
    }

    /// <summary>Removes the names; there is nothing else to free.</summary>
    private void Teardown()
    {
        foreach (var row in _rows)
        {
            row.Label.Dispose();
        }

        _rows.Clear();
    }

    /// <inheritdoc cref="Teardown" />
    public void Dispose() => Teardown();

    private void Apply()
    {
        foreach (var row in _rows)
        {
            row.Label.Visible = _visible;
        }
    }

    private sealed record Row(ChartSeries Series, ChartText Label);
}