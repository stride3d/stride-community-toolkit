using Stride.CommunityToolkit.Charts.Lines;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A line through given points - measured data, a hand-drawn shape, or a parametric curve already sampled.
/// Created by <see cref="Chart.AddLine"/> and <see cref="Chart.PlotParametric"/>. It remembers its points,
/// so a view-driven chart re-clips it when the visible range changes.
/// </summary>
/// <remarks>
/// A line whose points all share one depth is drawn as a stroke in that plane. On a 3D chart a line that
/// leaves its plane - a helix, a path through the depth - is a space curve, and is drawn as a ribbon mesh
/// instead; the difference shows only in how its glow is made. The shape batch can stroke a space curve,
/// but the charts do not use that path.
/// </remarks>
public sealed class ChartLineSeries : ChartSeries
{
    private readonly bool _closed;
    private readonly bool _clip;
    private readonly float _planeZ;
    private readonly ChartRibbon? _ribbon;
    private List<Vector2[]> _runs = [];
    private bool _drawClosed;

    /// <summary>The points, in chart units, as given.</summary>
    public IReadOnlyList<Vector3> Points { get; }

    internal ChartLineSeries(Chart chart, string name, Color color, ChartSeriesStyle? style, IReadOnlyList<Vector3> points, bool closed, bool clip)
        : base(chart, name, color, style)
    {
        Points = points;
        _closed = closed && points.Count > 1;
        _clip = clip;

        // A flat chart has no depth to speak of; a 3D one strokes the line only when it lies in one plane
        var planeZ = chart.Is3D ? SharedDepth(points) : 0f;
        _planeZ = planeZ ?? 0f;
        _ribbon = planeZ is null ? new ChartRibbon(chart, name) : null;
    }

    /// <inheritdoc />
    internal override void Rebuild()
    {
        // A closed shape is clipped as an open one that returns to its start, and drawn closed after all
        // only when nothing was cut, so the seam gets a proper join
        IReadOnlyList<Vector3> source = _closed ? [.. Points, Points[0]] : Points;

        var clipped = _clip ? Chart.Clip(source) : PolylineClipping.SplitAtNonFinite(source);

        _drawClosed = _closed && clipped.Count == 1 && clipped[0].Length == source.Count
            && clipped[0][0] == source[0] && clipped[0][^1] == source[^1];

        var runs = new List<IReadOnlyList<Vector3>>();

        if (_drawClosed)
        {
            runs.Add(Points);
        }
        else
        {
            foreach (var run in clipped)
            {
                runs.Add(run);
            }
        }

        IsEmpty = runs.Count == 0;

        if (_ribbon is not null)
        {
            _ribbon.Rebuild(runs, _drawClosed);

            return;
        }

        _runs = [.. runs.Select(ToPlane)];
    }

    /// <inheritdoc />
    internal override void Draw(ShapeBatch batch, in ChartView view)
    {
        if (IsEmpty)
            return;

        if (_ribbon is not null)
        {
            _ribbon.Draw(in view, Width, Color, Glow);

            return;
        }

        using var pen = Strokes(batch, in view, _planeZ + Chart.CurveLayer);

        foreach (var run in _runs)
        {
            pen.Draw(run, _drawClosed);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ribbon?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The one depth every finite point shares, or <see langword="null"/> when they differ.</summary>
    private static float? SharedDepth(IReadOnlyList<Vector3> points)
    {
        float? depth = null;

        foreach (var p in points)
        {
            if (!float.IsFinite(p.Z))
                continue;

            if (depth is null)
            {
                depth = p.Z;
            }
            else if (MathF.Abs(p.Z - depth.Value) > 1e-5f)
            {
                return null;
            }
        }

        return depth ?? 0f;
    }
}