using Stride.CommunityToolkit.Charts.Lines;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A curve plotted from a function, <c>y = f(x)</c>. It knows the function it came from, which is what lets a
/// view-driven chart re-sample it when the visible range changes, and what makes <see cref="SetFunction"/>
/// possible.
/// </summary>
/// <remarks>
/// Returned by <see cref="Chart.Plot"/>, which describes how samples are clipped and broken. Curves
/// plotted from points - <see cref="Chart.PlotParametric"/>, <see cref="Chart.AddLine"/> - are
/// <see cref="ChartLineSeries"/>, because there is no function to re-evaluate.
/// </remarks>
public sealed class ChartCurve : ChartSeries
{
    // The most samples a re-sample may use, so a deep zoom-out cannot make a rebuild arbitrarily expensive
    private const int MaxSamples = 8000;

    private readonly int _sampleCount;
    private readonly float _sampleDensity;
    private Func<float, float> _function;
    private List<Vector2[]> _runs = [];

    internal ChartCurve(Chart chart, string name, Color color, ChartSeriesStyle? style, Func<float, float> function, int samples)
        : base(chart, name, color, style)
    {
        _function = function;
        _sampleCount = samples;

        // Samples per chart unit at creation, so a wider range later is sampled at the same detail per
        // unit instead of stretching a fixed count across it
        _sampleDensity = samples / (chart.Options.Range.XMax - chart.Options.Range.XMin);
    }

    /// <summary>
    /// Swaps the function and re-samples just this curve, in place: it keeps its name, colour and legend
    /// row. This is how a curve is animated - a parameter changing every frame.
    /// </summary>
    /// <param name="f">The new function, sampled with the density this curve was created with.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="f"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">If the curve has been removed from its chart.</exception>
    public void SetFunction(Func<float, float> f)
    {
        ArgumentNullException.ThrowIfNull(f);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        _function = f;
        Rebuild();
    }

    /// <inheritdoc />
    internal override void Rebuild()
    {
        var r = Chart.Options.Range;

        // Never fewer samples than asked for; the cap bounds the cost at deep zoom-out
        var samples = Math.Clamp((int)(_sampleDensity * (r.XMax - r.XMin)), _sampleCount, MaxSamples);
        var points = PolylineSampling.Function(_function, r.XMin, r.XMax, samples);
        var branches = PolylineClipping.SplitAtJumps(points, (r.YMax - r.YMin) * 0.25f, extendEnds: true);

        var runs = new List<Vector2[]>();

        foreach (var branch in branches)
        {
            foreach (var run in Chart.Clip(branch))
            {
                runs.Add(ToPlane(run));
            }
        }

        _runs = runs;
        IsEmpty = runs.Count == 0;
    }

    /// <inheritdoc />
    internal override void Draw(ShapeBatch batch, in ChartView view)
    {
        if (IsEmpty)
            return;

        using var pen = Strokes(batch, in view, Chart.CurveLayer);

        foreach (var run in _runs)
        {
            pen.Draw(run);
        }
    }
}