using Stride.CommunityToolkit.Charts.Lines;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A curve that grows one point at a time - the path of a moving body, drawn while it moves. Created with
/// <see cref="Chart.AddTrajectory"/>; feed it with <see cref="Add"/> every frame.
/// </summary>
/// <remarks>
/// Points are clipped to the chart's ranges the way <see cref="Chart.Plot"/> clips a function: a segment
/// that leaves the chart ends exactly on the edge, and when the path comes back the trail resumes from the
/// edge as a separate run. On a flat chart the trail is a stroke, resubmitted each frame from the recorded
/// points; on a 3D chart it may leave the chart plane, so it is a ribbon mesh grown in place. Call
/// <see cref="Add"/> from the game thread.
/// </remarks>
public sealed class ChartTrajectory : ChartSeries
{
    private readonly TrailBuffer? _trail;
    private readonly GrowingPolyline? _ribbon;
    private readonly Entity? _entity;
    private Vector2[] _scratch = [];
    private Vector3? _previous;
    private bool _tipOnLine;

    /// <summary>How many points the trail holds.</summary>
    public int Count => _trail?.Count ?? _ribbon!.Count;

    /// <summary>The most points the trail can hold, set by <see cref="Chart.AddTrajectory"/>.</summary>
    public int Capacity => _trail?.Capacity ?? _ribbon!.Capacity;

    internal ChartTrajectory(Chart chart, string name, Color color, ChartSeriesStyle? style, int capacity, bool rollOver)
        : base(chart, name, color, style)
    {
        if (chart.Is3D)
        {
            // Width 1: the view scales it to the pixel width each frame. Bounds grow with the points, since
            // a view-driven chart can widen its ranges after the trail starts
            var options = new PolylineOptions { Width = 1f, Color = color, EmissiveIntensity = Glow > 0f ? ChartRibbon.GlowEmissive : 1f };

            _ribbon = new GrowingPolyline(chart.Game, capacity, options) { RollOver = rollOver };
            _entity = new Entity(name) { PolylineMeshBuilder.CreateModel(chart.Game, _ribbon.Mesh, options) };
            _entity.Transform.Position = new Vector3(0f, 0f, Chart.CurveLayer);
            chart.Root.AddChild(_entity);
        }
        else
        {
            _trail = new TrailBuffer(capacity) { RollOver = rollOver };
        }

        IsEmpty = true;
    }

    /// <summary>
    /// Appends the body's current position to the trail. Positions outside the chart's ranges are clipped -
    /// the trail runs to the edge, pauses, and resumes where the path re-enters. A position that is not
    /// finite breaks the trail.
    /// </summary>
    /// <param name="point">The position, in chart units.</param>
    public void Add(Vector3 point)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z))
        {
            Break();
            return;
        }

        var previous = _previous;
        _previous = point;

        if (previous is null)
        {
            // No segment to draw yet; a lone inside point becomes the start of the first one
            if (IsInside(point))
            {
                Record(point);
                _tipOnLine = true;
            }

            return;
        }

        var box = ClipBox();

        if (!PolylineClipping.ClipSegment(previous.Value, point, in box, out var t0, out var t1))
        {
            BreakRun();
            _tipOnLine = false;
            return;
        }

        if (t0 > 0f)
        {
            // Re-entering: resume from the edge as a new run
            BreakRun();
            Record(Vector3.Lerp(previous.Value, point, t0));
        }
        else if (!_tipOnLine)
        {
            Record(previous.Value);
        }

        Record(Vector3.Lerp(previous.Value, point, t1));

        if (t1 < 1f)
        {
            // Leaving: the trail ends on the edge until the path comes back
            BreakRun();
            _tipOnLine = false;
        }
        else
        {
            _tipOnLine = true;
        }
    }

    /// <summary>
    /// Lifts the pen: the next <see cref="Add"/> starts a new run instead of connecting to the last position.
    /// </summary>
    public void Break()
    {
        BreakRun();
        _previous = null;
        _tipOnLine = false;
    }

    /// <summary>
    /// Removes every point, keeping the buffers - restart a throw, rewind a simulation.
    /// </summary>
    public void Clear()
    {
        _trail?.Clear();
        _ribbon?.Clear();
        _previous = null;
        _tipOnLine = false;
        IsEmpty = true;
    }

    /// <summary>Nothing to rebuild: the recorded geometry is kept, and the ranges are read as points arrive.</summary>
    internal override void Rebuild()
    {
    }

    /// <inheritdoc />
    internal override void Draw(ShapeBatch batch, in ChartView view)
    {
        if (_ribbon is not null)
        {
            // The ribbon's width is in chart units; one pixel's worth at the chart's centre, times the width
            var r = Chart.Options.Range;
            var centre = new Vector3((r.XMin + r.XMax) * 0.5f, (r.YMin + r.YMax) * 0.5f, (r.ZMin + r.ZMax) * 0.5f);
            _ribbon.SetWidthScale(view.ToUnits(Width, centre));

            return;
        }

        var items = _trail!.Items;

        if (items.Length < 2)
            return;

        if (_scratch.Length < items.Length)
        {
            _scratch = new Vector2[Math.Max(items.Length, _trail.Capacity)];
        }

        using var pen = Strokes(batch, in view, Chart.CurveLayer);
        var n = 0;

        foreach (ref readonly var item in items)
        {
            if (TrailBuffer.IsBreak(in item))
            {
                pen.Draw(_scratch.AsSpan(0, n));
                n = 0;
                continue;
            }

            _scratch[n++] = new Vector2(item.X, item.Y);
        }

        pen.Draw(_scratch.AsSpan(0, n));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ribbon?.Dispose();

            if (_entity is not null)
            {
                _entity.Transform.Parent = null;
            }
        }

        base.Dispose(disposing);
    }

    private void Record(Vector3 point)
    {
        _trail?.Add(point);
        _ribbon?.Add(point);
        IsEmpty = false;
    }

    private void BreakRun()
    {
        _trail?.Break();
        _ribbon?.Break();
    }

    /// <summary>The chart's ranges as a clip box; a flat chart just leaves Z unbounded.</summary>
    private BoundingBox ClipBox()
    {
        var o = Chart.Options;

        return new BoundingBox(
            new Vector3(o.Range.XMin, o.Range.YMin, o.Range.ZMax > o.Range.ZMin ? o.Range.ZMin : -PolylineClipping.UnboundedZ),
            new Vector3(o.Range.XMax, o.Range.YMax, o.Range.ZMax > o.Range.ZMin ? o.Range.ZMax : PolylineClipping.UnboundedZ));
    }

    private bool IsInside(Vector3 point)
    {
        var o = Chart.Options;

        return point.X >= o.Range.XMin && point.X <= o.Range.XMax && point.Y >= o.Range.YMin && point.Y <= o.Range.YMax
            && (o.Range.ZMax <= o.Range.ZMin || (point.Z >= o.Range.ZMin && point.Z <= o.Range.ZMax));
    }
}