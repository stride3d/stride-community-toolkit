using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using System.Globalization;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The chart's scaffolding: axes and tick marks, drawn every frame as pixel-measured shapes so they keep
/// their width and length at any zoom, and the tick labels and titles, which are text entities laid out
/// for the current ranges and laid out again when the ranges change.
/// </summary>
/// <remarks>
/// The labels come from a pool: a range change re-uses the entities it already has, retexts and moves
/// them, and hides whatever is left over, so a view-driven chart panning across the screen creates
/// nothing per frame once the pool has grown to the most labels it has needed. The chart title is the
/// one text with a size of its own, so it has a slot of its own.
/// </remarks>
/// <param name="game">The game the labels' text renderer is registered with.</param>
/// <param name="chart">The chart whose ranges and options the scaffolding is laid out for.</param>
internal sealed class ChartScaffold(IGame game, Chart chart) : IDisposable
{
    private readonly List<ChartText> _pool = [];
    private ChartText? _title;
    private int _used;

    /// <summary>Lays out the tick labels (when enabled) and titles for the current ranges.</summary>
    internal void Build()
    {
        _used = 0;

        if (chart.Options.Labels.Visible)
        {
            BuildLabels();
        }

        BuildTitles();

        for (var i = _used; i < _pool.Count; i++)
        {
            _pool[i].Visible = false;
        }
    }

    /// <summary>Removes every label entity from the chart.</summary>
    public void Dispose()
    {
        foreach (var text in _pool)
        {
            text.Dispose();
        }

        _pool.Clear();
        _used = 0;

        _title?.Dispose();
        _title = null;
    }

    /// <summary>
    /// Submits the axes and tick marks for the current ranges. Widths and tick lengths are in pixels;
    /// the lengths are converted at each tick, so a perspective view gets ticks that read the same size
    /// near and far.
    /// </summary>
    internal void Draw(ShapeBatch batch, in ChartView view)
    {
        var o = chart.Options;

        // Each axis sits on the other coordinates' zero, or on the nearest edge when zero is out of range
        var axisY = Math.Clamp(0f, o.Range.YMin, o.Range.YMax);
        var axisX = Math.Clamp(0f, o.Range.XMin, o.Range.XMax);
        var axisZ = chart.Is3D ? Math.Clamp(0f, o.Range.ZMin, o.Range.ZMax) : 0f;

        batch.DrawPixelLine(view.ToWorld(new Vector3(o.Range.XMin, axisY, axisZ)), view.ToWorld(new Vector3(o.Range.XMax, axisY, axisZ)), o.Axes.Width, o.Axes.XColor);
        batch.DrawPixelLine(view.ToWorld(new Vector3(axisX, o.Range.YMin, axisZ)), view.ToWorld(new Vector3(axisX, o.Range.YMax, axisZ)), o.Axes.Width, o.Axes.YColor);

        if (chart.Is3D)
        {
            batch.DrawPixelLine(view.ToWorld(new Vector3(axisX, axisY, o.Range.ZMin)), view.ToWorld(new Vector3(axisX, axisY, o.Range.ZMax)), o.Axes.Width, o.Axes.ZColor);
        }

        // Ticks are centred across their axis; drawn after it, they blend on top of it
        foreach (var x in TickValues(o.Range.XMin, o.Range.XMax, o.Range.TickStep))
        {
            var at = new Vector3(x, axisY, axisZ);
            var half = view.ToUnits(o.Axes.TickLength, at) * 0.5f;
            batch.DrawPixelLine(view.ToWorld(at with { Y = axisY - half }), view.ToWorld(at with { Y = axisY + half }), o.Axes.TickWidth, o.Axes.XColor);
        }

        foreach (var y in TickValues(o.Range.YMin, o.Range.YMax, o.Range.TickStep))
        {
            var at = new Vector3(axisX, y, axisZ);
            var half = view.ToUnits(o.Axes.TickLength, at) * 0.5f;
            batch.DrawPixelLine(view.ToWorld(at with { X = axisX - half }), view.ToWorld(at with { X = axisX + half }), o.Axes.TickWidth, o.Axes.YColor);
        }

        if (!chart.Is3D)
            return;

        // Z ticks are little X-direction dashes along the Z axis
        foreach (var z in TickValues(o.Range.ZMin, o.Range.ZMax, o.Range.TickStep))
        {
            var at = new Vector3(axisX, axisY, z);
            var half = view.ToUnits(o.Axes.TickLength, at) * 0.5f;
            batch.DrawPixelLine(view.ToWorld(at with { X = axisX - half }), view.ToWorld(at with { X = axisX + half }), o.Axes.TickWidth, o.Axes.ZColor);
        }
    }

    private void BuildLabels()
    {
        var o = chart.Options;
        var axisY = Math.Clamp(0f, o.Range.YMin, o.Range.YMax);
        var axisX = Math.Clamp(0f, o.Range.XMin, o.Range.XMax);
        var axisZ = chart.Is3D ? Math.Clamp(0f, o.Range.ZMin, o.Range.ZMax) : 0f;

        // Half the tick plus a few pixels of breathing room, in the direction the anchor pushes the text:
        // below an x tick, left of a y or z tick
        var gap = o.Axes.TickLength * 0.5f + 4f;
        var below = new Vector2(0f, gap);
        var left = new Vector2(-gap, 0f);

        foreach (var x in TickValues(o.Range.XMin, o.Range.XMax, o.Range.TickStep))
        {
            // The origin is labelled once, by the y axis, so the two "0"s do not overlap
            if (IsZero(x) && IsZero(axisY))
                continue;

            Place(Format(x), new Vector3(x, axisY, axisZ), Anchor.TopCenter, below);
        }

        foreach (var y in TickValues(o.Range.YMin, o.Range.YMax, o.Range.TickStep))
        {
            Place(Format(y), new Vector3(axisX, y, axisZ), Anchor.MiddleRight, left);
        }

        if (!chart.Is3D)
            return;

        foreach (var z in TickValues(o.Range.ZMin, o.Range.ZMax, o.Range.TickStep))
        {
            // The origin is already labelled by the y axis
            if (IsZero(z) && IsZero(axisY))
                continue;

            Place(Format(z), new Vector3(axisX, axisY, z), Anchor.MiddleRight, left);
        }
    }

    /// <summary>
    /// Lays out the chart and axis titles, in the chart's label style: the chart title inside the top
    /// edge (on a view-driven chart that edge is the window's, so anything above it would be off screen),
    /// axis titles at the ends of their axes the way maths textbooks letter them.
    /// </summary>
    private void BuildTitles()
    {
        var o = chart.Options;
        var axisY = Math.Clamp(0f, o.Range.YMin, o.Range.YMax);
        var axisX = Math.Clamp(0f, o.Range.XMin, o.Range.XMax);

        if (o.Title.Text is { Length: > 0 } title)
        {
            if (_title is null)
            {
                ChartText.EnsureRenderer(game, o.Labels.Mode);
                _title = new ChartText(o.Labels, "Chart title", o.Title.FontSize, o.Title.Height);
                chart.Root.AddChild(_title.Entity);
            }

            _title.Set(title, Anchor.TopCenter, new Vector2(0f, 10f));
            _title.Position = new Vector3((o.Range.XMin + o.Range.XMax) * 0.5f, o.Range.YMax, 0f);
            _title.Visible = true;
        }
        else if (_title is not null)
        {
            _title.Visible = false;
        }

        if (o.Axes.XTitle is { Length: > 0 } xTitle)
        {
            Place(xTitle, new Vector3(o.Range.XMax, axisY, 0f), Anchor.TopRight, new Vector2(-4f, 10f));
        }

        if (o.Axes.YTitle is { Length: > 0 } yTitle)
        {
            Place(yTitle, new Vector3(axisX, o.Range.YMax, 0f), Anchor.TopLeft, new Vector2(10f, 4f));
        }

        if (chart.Is3D && o.Axes.ZTitle is { Length: > 0 } zTitle)
        {
            Place(zTitle, new Vector3(axisX, axisY, o.Range.ZMax), Anchor.TopLeft, new Vector2(10f, 4f));
        }
    }

    /// <summary>Takes the next text from the pool - or grows it - and sets it up.</summary>
    private void Place(string text, Vector3 position, Anchor anchor, Vector2 pixelOffset)
    {
        if (_used == _pool.Count)
        {
            var o = chart.Options;

            ChartText.EnsureRenderer(game, o.Labels.Mode);

            var created = new ChartText(o.Labels, $"Label {_pool.Count}");
            chart.Root.AddChild(created.Entity);
            _pool.Add(created);
        }

        var entry = _pool[_used++];
        entry.Set(text, anchor, pixelOffset);
        entry.Position = position;
        entry.Visible = true;
    }

    private string Format(float value) => value.ToString(chart.Options.Labels.Format, CultureInfo.InvariantCulture);

    private static TickRange TickValues(float min, float max, float step) => TickRange.Of(min, max, step);

    private static bool IsZero(float value) => MathF.Abs(value) < 1e-5f;
}