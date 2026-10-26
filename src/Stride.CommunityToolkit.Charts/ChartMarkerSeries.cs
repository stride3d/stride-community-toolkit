using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// Scatter markers: one small × per data point, drawn every frame in pixels so they keep their size at any
/// zoom or distance. Created with <see cref="Chart.AddMarkers"/>. Points outside the chart's ranges are
/// simply not drawn until panning or zooming brings them back.
/// </summary>
public sealed class ChartMarkerSeries : ChartSeries
{
    private readonly float _size;

    /// <summary>The data points, in chart units, as given.</summary>
    public IReadOnlyList<Vector3> Points { get; }

    internal ChartMarkerSeries(Chart chart, string name, Color color, ChartSeriesStyle style, IReadOnlyList<Vector3> points, float size)
        : base(chart, name, color, style)
    {
        Points = points;
        _size = size;
        IsEmpty = points.Count == 0;
    }

    /// <summary>Nothing to do: the glyphs are filtered against the ranges as they are drawn.</summary>
    internal override void Rebuild() { }

    /// <summary>Submits a glyph for every point inside the chart's ranges.</summary>
    internal override void Draw(ShapeBatch batch, in ChartView view)
    {
        var o = Chart.Options;

        // Just above the curves' layer, so the depth test keeps markers on top of a curve they sit on
        var lift = 2.5f * Chart.LayerStep;

        foreach (var p in Points)
        {
            if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z))
                continue;

            if (p.X < o.Range.XMin || p.X > o.Range.XMax || p.Y < o.Range.YMin || p.Y > o.Range.YMax)
                continue;

            if (Chart.Is3D && (p.Z < o.Range.ZMin || p.Z > o.Range.ZMax))
                continue;

            DrawGlyph(batch, in view, new Vector3(p.X, p.Y, p.Z + lift));
        }
    }

    /// <summary>
    /// Submits one × centred on <paramref name="local"/>, sized in pixels at that point - what the legend
    /// draws as this series' swatch too.
    /// </summary>
    internal void DrawGlyph(ShapeBatch batch, in ChartView view, Vector3 local)
    {
        var half = view.ToUnits(_size, local) * 0.5f;
        var width = Width;

        batch.DrawPixelLine(view.ToWorld(local + new Vector3(-half, -half, 0f)), view.ToWorld(local + new Vector3(half, half, 0f)), width, Color);
        batch.DrawPixelLine(view.ToWorld(local + new Vector3(-half, half, 0f)), view.ToWorld(local + new Vector3(half, -half, 0f)), width, Color);
    }
}