using Stride.CommunityToolkit.Charts.Lines;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A shaded region on a chart: the area between two functions over a stretch of <c>x</c> - the picture of a
/// definite integral, of the gap between a measurement and a model, or simply a curve's footprint. Created
/// with <see cref="Chart.AddArea(Func{float, float}, float, float, float, Color?, string?, int)"/>.
/// </summary>
/// <remarks>
/// The region keeps its two functions and its <c>x</c> stretch, so a view-driven chart re-samples it when
/// the visible range changes. It is drawn as a run of convex columns at
/// <see cref="ChartSeriesOptions.AreaOpacity"/>, over the grid and behind the axes and curves.
/// </remarks>
public sealed class ChartAreaSeries : ChartSeries
{
    private readonly AreaSpec _spec;
    private List<List<(Vector3 Upper, Vector3 Lower)>> _runs = [];

    internal ChartAreaSeries(Chart chart, string name, Color color, in AreaSpec spec)
        : base(chart, name, color, style: null)
    {
        _spec = spec;
    }

    /// <summary>The first <c>x</c> of the shaded stretch, as asked for.</summary>
    public float From => _spec.From;

    /// <summary>The last <c>x</c> of the shaded stretch, as asked for.</summary>
    public float To => _spec.To;

    /// <summary>
    /// Re-samples the region for the chart's current ranges; the stretch is trimmed to whatever part of it
    /// is visible.
    /// </summary>
    internal override void Rebuild()
    {
        var o = Chart.Options;
        var from = MathF.Max(_spec.From, o.Range.XMin);
        var to = MathF.Min(_spec.To, o.Range.XMax);

        if (to <= from)
        {
            _runs = [];
            IsEmpty = true;

            return;
        }

        var upper = PolylineSampling.Function(_spec.Upper, from, to, _spec.Samples);
        var lower = PolylineSampling.Function(_spec.Lower, from, to, _spec.Samples);

        _runs = AreaColumns.Columns(upper, lower, o.Range.YMin, o.Range.YMax);
        IsEmpty = _runs.Count == 0;
    }

    /// <summary>Submits the fill, one convex piece per column, at the chart's area opacity.</summary>
    internal override void Draw(ShapeBatch batch, in ChartView view)
    {
        if (IsEmpty)
            return;

        // The batch may be the caller's, so its fill state is put back the way it was found
        var border = batch.BorderWidth;
        var fillColor = batch.Fill.Color;
        var fillAlpha = batch.Fill.Alpha;

        batch.BorderWidth = 0f;
        batch.Fill.Set(Color, Chart.Options.Series.AreaOpacity);

        var plane = view.PlaneAt(Chart.LayerStep * 0.5f);
        Span<Vector2> corners = stackalloc Vector2[4];

        foreach (var run in _runs)
        {
            for (var i = 0; i + 1 < run.Count; i++)
            {
                DrawColumn(batch, in view, in plane, run[i], run[i + 1], corners);
            }
        }

        batch.BorderWidth = border;
        batch.Fill.Set(fillColor, fillAlpha);
    }

    /// <summary>
    /// One strip between two columns: a quadrilateral while the edges keep their order, two triangles
    /// meeting at the crossing where they swap - each counter-clockwise, as the batch wants its corners.
    /// </summary>
    private static void DrawColumn(ShapeBatch batch, in ChartView view, in ChartPlane plane, (Vector3 Upper, Vector3 Lower) left, (Vector3 Upper, Vector3 Lower) right, Span<Vector2> corners)
    {
        var d0 = left.Upper.Y - left.Lower.Y;
        var d1 = right.Upper.Y - right.Lower.Y;

        if (d0 * d1 < 0f)
        {
            // The edges cross inside the strip: a bow tie, drawn as the two triangles either side
            var t = d0 / (d0 - d1);
            var crossing = Vector3.Lerp(left.Upper, right.Upper, t);

            Fill(batch, in view, in plane, [Bottom(left), crossing, Top(left)], corners);
            Fill(batch, in view, in plane, [crossing, Bottom(right), Top(right)], corners);

            return;
        }

        Fill(batch, in view, in plane, [Bottom(left), Bottom(right), Top(right), Top(left)], corners);
    }

    /// <summary>Submits one convex piece, leaving out corners that coincide so a zero-height end never makes a degenerate edge.</summary>
    private static void Fill(ShapeBatch batch, in ChartView view, in ChartPlane plane, ReadOnlySpan<Vector3> points, Span<Vector2> corners)
    {
        var n = 0;

        foreach (var p in points)
        {
            var corner = view.ToPlane(new Vector2(p.X, p.Y));

            if (n > 0 && Vector2.DistanceSquared(corner, corners[n - 1]) < 1e-10f)
                continue;

            corners[n++] = corner;
        }

        if (n > 2 && Vector2.DistanceSquared(corners[0], corners[n - 1]) < 1e-10f)
        {
            n--;
        }

        if (n < 3)
            return;

        // The fill's own colour is set on the batch; the colour argument is the outline's, which has no width here
        batch.DrawSolidPolygon(corners[..n], plane.Position, plane.AxisX, plane.AxisY, batch.Fill.Color!.Value);
    }

    private static Vector3 Top((Vector3 Upper, Vector3 Lower) column) => column.Upper.Y >= column.Lower.Y ? column.Upper : column.Lower;

    private static Vector3 Bottom((Vector3 Upper, Vector3 Lower) column) => column.Upper.Y >= column.Lower.Y ? column.Lower : column.Upper;
}