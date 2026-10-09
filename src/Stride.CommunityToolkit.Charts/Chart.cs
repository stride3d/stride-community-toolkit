using Stride.CommunityToolkit.Charts.Lines;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A chart in the XY plane (optionally with a Z extent): axes, tick marks, tick labels, titles, an optional
/// major and minor grid, a legend, and any number of plotted curves - all children of one <see cref="Root"/>
/// entity, so the whole chart can be placed, rotated and scaled in the world as a single object.
/// </summary>
/// <remarks>
/// <para>
/// A chart is three things: <see cref="Options"/>, which stay live after it is built; series, added with
/// <see cref="Plot"/> and its siblings and taken off with <see cref="Remove"/>; and <see cref="Update(CameraComponent)"/>,
/// called once a frame with the camera, which applies whatever changed in the options and drives the
/// parts that depend on the view. Construct a chart, add its <see cref="Root"/> to a scene, plot, and call
/// <see cref="Update(CameraComponent)"/> from your update loop. <see cref="Dispose"/> frees everything the chart owns.
/// </para>
/// <para>
/// Inside, the chart is a façade over three parts, each with one responsibility: <see cref="ChartScaffold"/>
/// (axes, ticks, labels, titles), <see cref="ChartGrid"/> and <see cref="ChartLegend"/>. Nearly everything is
/// drawn every frame by <see cref="Update(CameraComponent)"/> as pixel-measured strokes in a shape batch - the
/// grid, the axes and ticks, the curves themselves, markers, legend swatches, the cursor - so all of it
/// keeps its width at any zoom or distance, and every width in the options is a width in pixels. The one
/// exception is a 3D curve that leaves the chart plane, which is drawn as a ribbon mesh.
/// </para>
/// </remarks>
public sealed class Chart : IDisposable
{
    // Everything lies in the chart plane, so coplanar things z-fight where they cross under the depth test.
    // Each layer is nudged along Z by this much: grids behind the axes, area fills and curves in front.
    internal const float LayerStep = 0.005f;

    // Where the curves sit: above the axes at 0 and the area fills at half a step
    internal const float CurveLayer = 2f * LayerStep;

    private readonly List<ChartSeries> _series = [];
    private readonly ChartScaffold _scaffold;
    private readonly ChartLegend _legend;
    private readonly ChartGrid _grid;
    private ChartViewFollower? _follower;
    private ChartCursor? _cursor;
    private ShapeBatch? _ownedBatch;

    // What Update last applied from Options, so a frame with nothing changed costs a few compares; what is
    // drawn afresh each frame - the grid, the strokes - reads the options directly and needs no snapshot
    private RangeSnapshot _appliedRange;
    private bool _appliedFollow;
    private bool _appliedLegendVisible;
    private bool _isDisposed;

    /// <summary>The entity every part of the chart is parented to. Add it to a scene and move it to place the chart.</summary>
    public Entity Root { get; }

    /// <summary>
    /// The chart's settings, live: change one and the next <see cref="Update(CameraComponent)"/> applies it. See
    /// <see cref="ChartOptions"/> for the one exception, the series defaults.
    /// </summary>
    public ChartOptions Options { get; }

    /// <summary>
    /// Whether the chart has a real Z extent - axes, clipping and grids gain the third dimension. Decided
    /// once, when the chart is built, from whether <see cref="ChartRangeOptions.ZMax"/> was above
    /// <see cref="ChartRangeOptions.ZMin"/>.
    /// </summary>
    public bool Is3D { get; }

    /// <summary>The curves on the chart, in the order they were added.</summary>
    public IReadOnlyList<ChartSeries> Series => _series;

    /// <summary>
    /// The point of the chart plane under the mouse, in chart units, while <see cref="ChartCursorOptions.Visible"/>
    /// is on and the mouse is over the chart; otherwise <see langword="null"/>. The value to read a curve
    /// against, or to snap something to. Refreshed by <see cref="Update(CameraComponent)"/>.
    /// </summary>
    public Vector3? CursorPosition { get; private set; }

    /// <summary>The game the chart draws in; its device, input and services.</summary>
    internal Game Game { get; }

    /// <summary>
    /// Builds a chart. The <see cref="Root"/> is not added to a scene; do that where you want it.
    /// </summary>
    /// <param name="game">The game the chart is drawn in.</param>
    /// <param name="options">What the chart shows and how it is drawn; <see langword="null"/> for <see cref="ChartOptions.Glow3D"/>.</param>
    /// <param name="name">The root entity's name, or <c>"Chart"</c>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> is <see langword="null"/>.</exception>
    public Chart(Game game, ChartOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= ChartOptions.Glow3D();

        if (options.Labels.Visible)
        {
            ChartText.EnsureRenderer(game, options.Labels.Mode);
        }

        Game = game;
        Options = options;
        Is3D = options.Range.ZMax > options.Range.ZMin;
        Root = new Entity(name ?? "Chart");

        _scaffold = new ChartScaffold(game, this);
        _legend = new ChartLegend(game, this);
        _grid = new ChartGrid(this);

        // Built for the options as given; Update then only has to apply what changes afterwards
        _appliedRange = RangeSnapshot.Of(options.Range);
        _appliedLegendVisible = options.Legend.Visible;

        _scaffold.Build();
        _legend.SetVisible(options.Legend.Visible);
    }

    /// <summary>
    /// Plots <c>y = f(x)</c> across the chart's <c>x</c> range. Samples outside the <c>y</c> range are clipped
    /// to the chart edge, samples that are not finite (a function outside its domain) break the curve, and so
    /// does a zero-crossing jump larger than a quarter of the chart's height between two samples - the
    /// asymptotes of <c>tan(x)</c> or <c>1/x</c> - where the branches are instead extended to the chart edge.
    /// </summary>
    /// <param name="f">The function to plot.</param>
    /// <param name="color">The curve's colour; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="samples">How many points to sample; more is smoother.</param>
    /// <param name="style">Width and glow where they should differ from the chart's defaults.</param>
    /// <returns>The curve, already on the chart; keep it to animate it with <see cref="ChartCurve.SetFunction"/> or to <see cref="Remove"/> it later.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="f"/> is <see langword="null"/>.</exception>
    public ChartCurve Plot(Func<float, float> f, Color? color = null, string? name = null, int samples = 200, ChartSeriesStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(f);

        var seriesName = name ?? $"Plot {_series.Count + 1}";

        return Register(new ChartCurve(this, seriesName, ResolveColor(color, style), style, f, samples));
    }

    /// <summary>
    /// Plots a parametric curve <c>p(t)</c>, clipped to the chart's ranges.
    /// </summary>
    /// <param name="p">The curve; its <c>z</c> is kept, so the curve may leave the chart plane.</param>
    /// <param name="from">The first <c>t</c>.</param>
    /// <param name="to">The last <c>t</c>.</param>
    /// <param name="color">The curve's colour; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="samples">How many points to sample; more is smoother.</param>
    /// <param name="closed">Whether to join the last point back to the first - a circle, an ellipse, any loop.</param>
    /// <param name="style">Width and glow where they should differ from the chart's defaults.</param>
    /// <returns>The series, already on the chart; keep it to <see cref="Remove"/> the curve later.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="p"/> is <see langword="null"/>.</exception>
    public ChartLineSeries PlotParametric(Func<float, Vector3> p, float from, float to, Color? color = null, string? name = null, int samples = 200, bool closed = false, ChartSeriesStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(p);

        var points = PolylineSampling.Parametric(p, from, to, samples);

        return AddLine(points, color, name ?? $"Plot {_series.Count + 1}", closed, clip: true, style);
    }

    /// <summary>
    /// Adds a line through arbitrary points - measured data, a trajectory, a hand-drawn shape.
    /// </summary>
    /// <param name="points">The points, in chart units.</param>
    /// <param name="color">The line's colour; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="closed">Whether to join the last point back to the first.</param>
    /// <param name="clip">
    /// Whether to cut the line to the chart's ranges. <see langword="true"/> (the default) also breaks the line at
    /// points that are not finite; <see langword="false"/> only does the latter and lets the line leave the chart.
    /// </param>
    /// <param name="style">Width and glow where they should differ from the chart's defaults.</param>
    /// <returns>The series, already on the chart; keep it to <see cref="Remove"/> the line later.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    public ChartLineSeries AddLine(IReadOnlyList<Vector3> points, Color? color = null, string? name = null, bool closed = false, bool clip = true, ChartSeriesStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        var seriesName = name ?? $"Line {_series.Count + 1}";

        return Register(new ChartLineSeries(this, seriesName, ResolveColor(color, style), style, points, closed, clip));
    }

    /// <summary>
    /// Adds an empty trajectory: a curve that grows one point at a time - the path of a moving body, drawn
    /// while it moves. Feed it from your update loop with <see cref="ChartTrajectory.Add"/>; points are
    /// clipped to the chart's ranges the same way <see cref="Plot"/> clips a function.
    /// </summary>
    /// <param name="capacity">The most points the trail can hold.</param>
    /// <param name="color">The trail's colour; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="rollOver">What a full trail does with the next point: <see langword="false"/> ignores it, <see langword="true"/> drops the oldest - an oscilloscope trace.</param>
    /// <param name="style">Width and glow where they should differ from the chart's defaults.</param>
    /// <returns>The trajectory, already on the chart and empty; it is also in <see cref="Series"/> and removed like any other curve.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="capacity"/> is less than two.</exception>
    public ChartTrajectory AddTrajectory(int capacity = 1000, Color? color = null, string? name = null, bool rollOver = false, ChartSeriesStyle? style = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);

        var seriesName = name ?? $"Trajectory {_series.Count + 1}";

        return Register(new ChartTrajectory(this, seriesName, ResolveColor(color, style), style, capacity, rollOver));
    }

    /// <summary>
    /// Adds scatter markers: one small × per point, drawn every frame in pixels, so they keep their size at
    /// any zoom or distance. Points outside the chart's ranges are not drawn until the range brings them
    /// back.
    /// </summary>
    /// <param name="points">The data points, in chart units.</param>
    /// <param name="color">The markers' colour; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="size">The glyph's size in pixels; <see langword="null"/> takes <see cref="ChartSeriesOptions.MarkerSize"/>.</param>
    /// <param name="width">The glyph's stroke width in pixels; <see langword="null"/> takes <see cref="ChartSeriesOptions.MarkerWidth"/>.</param>
    /// <returns>The series, already on the chart; remove it like any other.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="size"/> or <paramref name="width"/> is not positive.</exception>
    public ChartMarkerSeries AddMarkers(IReadOnlyList<Vector3> points, Color? color = null, string? name = null, float? size = null, float? width = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        var markerSize = size ?? Options.Series.MarkerSize;
        var markerWidth = width ?? Options.Series.MarkerWidth;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(markerSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(markerWidth);

        // The glyph's stroke width is the series' width, pinned so the chart's curve width does not reach it
        var style = new ChartSeriesStyle { Width = markerWidth };
        var seriesName = name ?? $"Markers {_series.Count + 1}";

        return Register(new ChartMarkerSeries(this, seriesName, ResolveColor(color, style), style, points, markerSize));
    }

    /// <summary>
    /// Shades the region between <c>y = f(x)</c> and a horizontal baseline over a stretch of <c>x</c> - the
    /// picture of a definite integral. The fill is translucent and drawn behind the curves.
    /// </summary>
    /// <param name="f">The function bounding the region.</param>
    /// <param name="from">The first <c>x</c> of the stretch.</param>
    /// <param name="to">The last <c>x</c> of the stretch.</param>
    /// <param name="baseline">The <c>y</c> the region is measured from. Defaults to <c>0</c>, the x axis.</param>
    /// <param name="color">The fill colour, drawn at <see cref="ChartSeriesOptions.AreaOpacity"/>; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="samples">How many columns the region is built from; more follows a wiggly curve more closely.</param>
    /// <returns>The region, already on the chart; remove it like any other series.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="f"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="to"/> is not greater than <paramref name="from"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="samples"/> is less than two.</exception>
    public ChartAreaSeries AddArea(Func<float, float> f, float from, float to, float baseline = 0f, Color? color = null, string? name = null, int samples = 200)
    {
        ArgumentNullException.ThrowIfNull(f);

        return AddArea(f, _ => baseline, from, to, color, name, samples);
    }

    /// <summary>
    /// Shades the region between two functions over a stretch of <c>x</c> - the gap between a measurement
    /// and a model, or between two bounds.
    /// </summary>
    /// <param name="upper">One function bounding the region.</param>
    /// <param name="lower">The other; the two may cross, and the region simply narrows to nothing there.</param>
    /// <param name="from">The first <c>x</c> of the stretch.</param>
    /// <param name="to">The last <c>x</c> of the stretch.</param>
    /// <param name="color">The fill colour, drawn at <see cref="ChartSeriesOptions.AreaOpacity"/>; <see langword="null"/> takes the next palette colour.</param>
    /// <param name="name">The series name.</param>
    /// <param name="samples">How many columns the region is built from.</param>
    /// <returns>The region, already on the chart; remove it like any other series.</returns>
    /// <exception cref="ArgumentNullException">If either function is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="to"/> is not greater than <paramref name="from"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="samples"/> is less than two.</exception>
    public ChartAreaSeries AddArea(Func<float, float> upper, Func<float, float> lower, float from, float to, Color? color = null, string? name = null, int samples = 200)
    {
        ArgumentNullException.ThrowIfNull(upper);
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentOutOfRangeException.ThrowIfLessThan(samples, 2);

        if (to <= from)
        {
            throw new ArgumentException("The stretch must have a positive width.", nameof(to));
        }

        // The series' colour is the solid one, so the legend stays legible; the fill takes it at
        // ChartSeriesOptions.AreaOpacity when it is drawn
        var seriesName = name ?? $"Area {_series.Count + 1}";

        return Register(new ChartAreaSeries(this, seriesName, ResolveColor(color, style: null), new AreaSpec(upper, lower, from, to, samples)));
    }

    /// <summary>
    /// Takes a series off the chart and frees whatever it owned. Does nothing if the series
    /// is not on this chart.
    /// </summary>
    /// <param name="series">A series returned by one of the chart's <c>Plot</c> or <c>Add</c> methods.</param>
    /// <returns><see langword="true"/> if the series was on the chart and has been removed.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="series"/> is <see langword="null"/>.</exception>
    public bool Remove(ChartSeries series)
    {
        ArgumentNullException.ThrowIfNull(series);

        if (!_series.Remove(series))
            return false;

        series.Dispose();
        _legend.Rebuild();

        return true;
    }

    /// <summary>
    /// Takes every series off the chart. Axes, ticks, grid and labels stay.
    /// </summary>
    public void Clear()
    {
        foreach (var series in _series)
        {
            series.Dispose();
        }

        _series.Clear();
        _legend.Rebuild();
    }

    /// <summary>
    /// The chart's frame: applies whatever changed in <see cref="Options"/> since the last call, and drives
    /// the parts that depend on the camera - re-targeting the range to the view when
    /// <see cref="ChartRangeOptions.FollowCamera"/> is on, and the mouse readout when
    /// <see cref="ChartCursorOptions.Visible"/> is. Call it once a frame from your update loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A changed range lays out the labels, titles and legend again and re-plots every series. Every
    /// stroke and fill is read from the options and submitted afresh each frame.
    /// </para>
    /// <para>
    /// The camera is a parameter rather than something the chart finds for itself, because a scene can hold
    /// several: pass the one that looks at this chart, and two charts can follow two different cameras.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera looking at this chart.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="camera"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="ChartRangeOptions"/> has no positive width or height.</exception>
    /// <exception cref="ObjectDisposedException">If the chart has been disposed.</exception>
    public void Update(CameraComponent camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        UpdateCore(camera, shapes: null);
    }

    /// <summary>
    /// <see cref="Update(CameraComponent)"/>, drawing the furniture into a batch of your own instead of the
    /// chart's - one draw call for a chart and the HUD around it, and your choice of depth behaviour. The
    /// chart never creates a batch of its own when it is updated this way.
    /// </summary>
    /// <param name="camera">The camera looking at this chart.</param>
    /// <param name="shapes">The batch to submit every stroke and fill of the chart into.</param>
    /// <exception cref="ArgumentNullException">If either argument is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">If <see cref="ChartRangeOptions"/> has no positive width or height.</exception>
    /// <exception cref="ObjectDisposedException">If the chart has been disposed.</exception>
    public void Update(CameraComponent camera, ShapeBatch shapes)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(shapes);

        UpdateCore(camera, shapes);
    }

    private void UpdateCore(CameraComponent camera, ShapeBatch? shapes)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var o = Options;

        if (o.Range.FollowCamera != _appliedFollow)
        {
            _appliedFollow = o.Range.FollowCamera;
            _follower = _appliedFollow ? new ChartViewFollower(Game, this) : null;
        }

        // Writes the range when the view moved; the range diff below is what acts on it
        _follower?.Update(camera);

        ApplyRange();

        if (o.Legend.Visible != _appliedLegendVisible)
        {
            _appliedLegendVisible = o.Legend.Visible;
            _legend.SetVisible(_appliedLegendVisible);
        }

        // Everything is submitted afresh each frame, in the chart's own depth-tested batch unless the
        // caller brought one. The batch draws in submission order, so this is back to front: grid, area
        // fills, axes, curves, then markers over the curves they sit on, then the legend and the cursor
        var batch = shapes ?? (_ownedBatch ??= Game.AddShapeBatch(depthTest: true));
        var pixelScale = batch.AutoScale ? DisplayScale.GetOrCreate(Game).Value : 1f;
        var view = new ChartView(Root, camera, Game.GraphicsDevice.Presenter.BackBuffer.Height, pixelScale);

        UpdateCursor(camera, in view);

        _grid.Draw(batch, in view);
        DrawSeries(batch, in view, s => s is ChartAreaSeries);
        _scaffold.Draw(batch, in view);
        DrawSeries(batch, in view, s => s is not ChartAreaSeries and not ChartMarkerSeries);
        DrawSeries(batch, in view, s => s is ChartMarkerSeries);

        _legend.Draw(batch, in view);
        _cursor?.Draw(batch, in view);
    }

    /// <summary>Submits the series a layer is made of, in the order they were added.</summary>
    private void DrawSeries(ShapeBatch batch, in ChartView view, Func<ChartSeries, bool> layer)
    {
        foreach (var series in _series)
        {
            if (layer(series))
            {
                series.Draw(batch, in view);
            }
        }
    }

    /// <summary>
    /// Applies a changed range: lays out the labels, titles and legend again and re-plots every series.
    /// </summary>
    private void ApplyRange()
    {
        var r = Options.Range;
        var snapshot = RangeSnapshot.Of(r);

        if (snapshot == _appliedRange)
            return;

        if (!(r.XMax > r.XMin) || !(r.YMax > r.YMin))
        {
            throw new InvalidOperationException("Options.Range must have positive width and height.");
        }

        _appliedRange = snapshot;

        _scaffold.Build();
        _legend.Rebuild();

        foreach (var series in _series)
        {
            series.Rebuild();
        }
    }

    /// <summary>
    /// Moves the readout under the mouse while it is wanted, building it on first use; hides it otherwise.
    /// </summary>
    private void UpdateCursor(CameraComponent camera, in ChartView view)
    {
        if (!Options.Cursor.Visible)
        {
            _cursor?.Hide();
            CursorPosition = null;
            return;
        }

        _cursor ??= new ChartCursor(Game, this);
        _cursor.Update(camera, Game.Input.MousePosition, in view);
        CursorPosition = _cursor.Position;
    }

    /// <summary>
    /// Moves <paramref name="camera"/> so the chart's ranges exactly fill the window, with a little
    /// breathing room. An orthographic camera is centred and gets its size set; a perspective camera keeps
    /// its viewing direction and backs away until the chart's box fits the frustum. One-shot: the chart
    /// never steers the camera afterwards.
    /// </summary>
    /// <param name="camera">The camera to frame.</param>
    /// <param name="padding">Extra room as a fraction of the chart per side. Defaults to <c>0.05</c>.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="camera"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="padding"/> is negative.</exception>
    public void FrameCamera(CameraComponent camera, float padding = 0.05f)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentOutOfRangeException.ThrowIfNegative(padding);

        var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
        var aspect = (float)backBuffer.Width / backBuffer.Height;

        // The chart's box in world space: the local ranges through the root's world matrix
        Root.Transform.UpdateWorldMatrix();
        var localMin = new Vector3(Options.Range.XMin, Options.Range.YMin, Is3D ? Options.Range.ZMin : 0f);
        var localMax = new Vector3(Options.Range.XMax, Options.Range.YMax, Is3D ? Options.Range.ZMax : 0f);
        var box = ChartFraming.TransformBox(localMin, localMax, in Root.Transform.WorldMatrix);
        var centre = (box.Minimum + box.Maximum) * 0.5f;
        var size = Vector3.Max(box.Maximum - box.Minimum, new Vector3(1e-3f));

        var transform = camera.Entity.Transform;

        if (camera.Projection == CameraProjectionMode.Orthographic)
        {
            // Centre the view and size it; the camera keeps its depth along Z
            transform.Position = new Vector3(centre.X, centre.Y, transform.Position.Z);
            camera.OrthographicSize = ChartFraming.OrthographicSize(size.X, size.Y, aspect, padding);
            return;
        }

        // Keep the viewing direction and back away along it until every corner of the box fits the frustum
        transform.UpdateWorldMatrix();
        var forward = transform.WorldMatrix.Forward;

        if (forward.LengthSquared() < MathUtil.ZeroTolerance)
        {
            forward = -Vector3.UnitZ;
        }

        forward.Normalize();

        var right = transform.WorldMatrix.Right;
        right.Normalize();
        var up = transform.WorldMatrix.Up;
        up.Normalize();

        var fov = MathUtil.DegreesToRadians(camera.VerticalFieldOfView);
        var distance = ChartFraming.PerspectiveDistance(in box, right, up, forward, aspect, fov, padding);
        transform.Position = centre - forward * distance;
    }

    /// <summary>
    /// Frees everything the chart owns: series, scaffolding, legend, cursor and its own shape batch.
    /// Remove <see cref="Root"/> from its scene first.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        Clear();

        _cursor?.Dispose();
        _cursor = null;
        _scaffold.Dispose();
        _legend.Dispose();

        // The owned batch was registered with the renderer by AddShapeBatch; taken out again so an empty
        // batch is not left in the visibility group for the rest of the game
        if (_ownedBatch is not null)
        {
            Game.RemoveShapeBatch(_ownedBatch);
            _ownedBatch = null;
        }
    }

    /// <summary>The range fields <see cref="Update(CameraComponent)"/> compares to know whether a rebuild is due.</summary>
    private readonly record struct RangeSnapshot(float XMin, float XMax, float YMin, float YMax, float ZMin, float ZMax, float TickStep, int MinorDivisions)
    {
        internal static RangeSnapshot Of(ChartRangeOptions r)
            => new(r.XMin, r.XMax, r.YMin, r.YMax, r.ZMin, r.ZMax, r.TickStep, r.MinorDivisions);
    }

    /// <summary>Clips a run of points to the chart's ranges - a box for a 3D chart, a rectangle otherwise.</summary>
    internal List<Vector3[]> Clip(IReadOnlyList<Vector3> points)
        => Is3D
            ? PolylineClipping.Clip(points, Options.Range.XMin, Options.Range.XMax, Options.Range.YMin, Options.Range.YMax, Options.Range.ZMin, Options.Range.ZMax)
            : PolylineClipping.Clip(points, Options.Range.XMin, Options.Range.XMax, Options.Range.YMin, Options.Range.YMax);

    /// <summary>
    /// The colour a series is drawn in: the argument, then the style, then the next colour of the palette in
    /// turn, so a chart plotted without any colours still reads.
    /// </summary>
    private Color ResolveColor(Color? color, ChartSeriesStyle? style)
    {
        var palette = Options.Series.Palette;
        var next = palette.Count > 0 ? palette[_series.Count % palette.Count] : Color.White;

        return color ?? style?.Color ?? next;
    }

    /// <summary>Adds a series to the chart, builds it for the current ranges and refreshes the legend.</summary>
    private T Register<T>(T series) where T : ChartSeries
    {
        _series.Add(series);
        series.Rebuild();
        _legend.Rebuild();

        return series;
    }
}