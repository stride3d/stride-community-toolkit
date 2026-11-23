using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// One series on a <see cref="Chart"/>: its name, its colour, and the width and glow it is drawn with. Take
/// it off the chart with <see cref="Chart.Remove"/>, or <see cref="Chart.Clear"/> for every series at once.
/// </summary>
/// <remarks>
/// <para>
/// Each kind of series knows how to rebuild itself for the chart's current ranges - a
/// <see cref="ChartCurve"/> re-samples its function, a <see cref="ChartLineSeries"/> re-clips its points,
/// a <see cref="ChartAreaSeries"/> re-samples its bounds - and how to draw itself every frame into the
/// chart's shape batch: curves, lines, trajectories and markers as pixel-measured strokes that keep their
/// width at any zoom or distance, areas as translucent fills. The chart asks every
/// series to rebuild when the range changes, which is how a view-driven chart follows the camera, and to
/// draw on every <see cref="Chart.Update(Stride.Engine.CameraComponent)"/>.
/// </para>
/// <para>
/// <see cref="Width"/> and <see cref="Glow"/> come from the style the series was added with, or, when
/// the style left them unset, from the chart's <see cref="ChartSeriesOptions"/> - live, so changing the
/// chart's defaults changes every series that has none of its own.
/// </para>
/// </remarks>
public abstract class ChartSeries : IDisposable
{
    private readonly float? _width;
    private readonly float? _glow;

    /// <summary>The name given when the series was added; what the legend shows.</summary>
    public string Name { get; }

    /// <summary>The series' colour - what the legend shows next to <see cref="Name"/>.</summary>
    public Color Color { get; }

    /// <summary>The stroke width in pixels: the style's, or <see cref="ChartSeriesOptions.CurveWidth"/>.</summary>
    public float Width => _width ?? Chart.Options.Series.CurveWidth;

    /// <summary>The glow halo in pixels, 0 for none: the style's, or <see cref="ChartSeriesOptions.Glow"/>.</summary>
    public float Glow => _glow ?? Chart.Options.Series.Glow;

    /// <summary>
    /// <see langword="true"/> when nothing of the series falls inside the chart's current ranges - every
    /// sample out of range or not finite - so there is nothing to draw. Refreshed whenever the series is
    /// rebuilt.
    /// </summary>
    public bool IsEmpty { get; private protected set; }

    /// <summary>Whether <see cref="Dispose()"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The chart the series is on.</summary>
    internal Chart Chart { get; }

    private protected ChartSeries(Chart chart, string name, Color color, ChartSeriesStyle? style)
    {
        Chart = chart;
        Name = name;
        Color = color;
        _width = style?.Width;
        _glow = style?.Glow;
    }

    /// <summary>
    /// Rebuilds the series for the chart's current ranges. Called once when the series is added and again
    /// whenever the range changes.
    /// </summary>
    internal abstract void Rebuild();

    /// <summary>Submits the series to the batch for this frame.</summary>
    internal abstract void Draw(ShapeBatch batch, in ChartView view);

    /// <summary>
    /// A pen for this frame's strokes in the plane at a chart-local depth, carrying the series' width,
    /// colour and glow. Dispose it when the series' runs are submitted.
    /// </summary>
    private protected StrokePen Strokes(ShapeBatch batch, in ChartView view, float z)
        => new(batch, in view, z, this);

    /// <summary>A clipped run as plane coordinates: its <c>x</c> and <c>y</c>, the depth being the plane's.</summary>
    private protected static Vector2[] ToPlane(IReadOnlyList<Vector3> run)
    {
        var flat = new Vector2[run.Count];

        for (var i = 0; i < flat.Length; i++)
        {
            flat[i] = new Vector2(run[i].X, run[i].Y);
        }

        return flat;
    }

    /// <summary>
    /// Frees whatever the series holds beyond its points. Strokes hold nothing, so for most series this
    /// only marks the series disposed; a 3D space curve releases its mesh.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;

        IsDisposed = true;

        Release();
    }

    /// <summary>Frees what a series holds beyond its points; the base holds nothing. Runs once, from the first <see cref="Dispose"/>.</summary>
    private protected virtual void Release() { }
}