using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Lines;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Globalization;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A 2D chart in the XY plane: axes, tick marks, tick labels, an optional grid, and any number of plotted
/// curves - all children of one <see cref="Root"/> entity, so the whole chart can be placed, rotated and
/// scaled in the world as a single object.
/// </summary>
/// <remarks>
/// Every line is a ribbon mesh from <see cref="PolylineMeshBuilder"/>, so it has real thickness, and the
/// labels are <see cref="WorldTextComponent"/>s, so they face the camera as it orbits. Create a chart with
/// <see cref="Create"/>, add its <see cref="Root"/> to a scene, then call <see cref="Plot"/> for each curve.
/// </remarks>
public sealed class Chart
{
    private static readonly Color[] Palette =
    [
        Color.Cyan, Color.Orange, Color.Magenta, Color.Yellow, Color.LightGreen, Color.HotPink, Color.DeepSkyBlue,
    ];

    // Every ribbon lies in the chart plane, so coplanar ones z-fight where they cross and flicker dark
    // fringes. Each layer is nudged along Z by this much: grid behind the axes, ticks and curves in front.
    private const float LayerStep = 0.005f;

    private readonly Game _game;
    private readonly ModelComponent? _gridModel;
    private int _plotCount;

    /// <summary>The entity every part of the chart is parented to. Add it to a scene and move it to place the chart.</summary>
    public Entity Root { get; }

    /// <summary>The settings the chart was created with.</summary>
    public ChartOptions Options { get; }

    /// <summary>Shows or hides the grid. Cheap to toggle every frame; the mesh is built once.</summary>
    public bool GridVisible
    {
        get => _gridModel?.Enabled ?? false;
        set
        {
            if (_gridModel != null)
                _gridModel.Enabled = value;
        }
    }

    private Chart(Game game, ChartOptions options, string name)
    {
        _game = game;
        Options = options;
        Root = new Entity(name);

        BuildAxes();
        BuildTicks();
        _gridModel = BuildGrid();

        if (options.ShowLabels)
        {
            BuildLabels();
        }
    }

    /// <summary>
    /// Builds a chart. The <see cref="Root"/> is not added to a scene; do that where you want it.
    /// </summary>
    /// <param name="game">The game the chart is drawn in.</param>
    /// <param name="options">Ranges, ticks, grid and labels; <see langword="null"/> for the defaults.</param>
    /// <param name="name">The root entity's name, or <c>"Chart"</c>.</param>
    /// <returns>The chart, ready for <see cref="Plot"/> calls.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> is <see langword="null"/>.</exception>
    public static Chart Create(Game game, ChartOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= new ChartOptions();

        if (options.ShowLabels)
        {
            // World text never appears without its renderer; registering twice is harmless
            game.AddWorldTextRenderer();
        }

        return new Chart(game, options, name ?? "Chart");
    }

    /// <summary>
    /// Plots <c>y = f(x)</c> across the chart's <c>x</c> range.
    /// </summary>
    /// <param name="f">The function to plot.</param>
    /// <param name="options">Width, colour and glow; <see langword="null"/> for a glowing line in the next palette colour.</param>
    /// <param name="samples">How many points to sample; more is smoother.</param>
    /// <param name="name">The curve entity's name.</param>
    /// <returns>The curve entity, already parented to <see cref="Root"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="f"/> is <see langword="null"/>.</exception>
    public Entity Plot(Func<float, float> f, PolylineOptions? options = null, int samples = 200, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(f);

        var points = PolylineSampling.Function(f, Options.XMin, Options.XMax, samples);

        return AddLine(points, options, name ?? $"Plot {_plotCount + 1}");
    }

    /// <summary>
    /// Plots a parametric curve <c>p(t)</c>.
    /// </summary>
    /// <param name="p">The curve; its <c>z</c> is kept, so the curve may leave the chart plane.</param>
    /// <param name="from">The first <c>t</c>.</param>
    /// <param name="to">The last <c>t</c>.</param>
    /// <param name="options">Width, colour and glow; <see langword="null"/> for a glowing line in the next palette colour.</param>
    /// <param name="samples">How many points to sample; more is smoother.</param>
    /// <param name="name">The curve entity's name.</param>
    /// <returns>The curve entity, already parented to <see cref="Root"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="p"/> is <see langword="null"/>.</exception>
    public Entity PlotParametric(Func<float, Vector3> p, float from, float to, PolylineOptions? options = null, int samples = 200, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(p);

        var points = PolylineSampling.Parametric(p, from, to, samples);

        return AddLine(points, options, name ?? $"Plot {_plotCount + 1}");
    }

    /// <summary>
    /// Adds a line through arbitrary points - measured data, a trajectory, a hand-drawn shape.
    /// </summary>
    /// <param name="points">The points, in chart units. At least two.</param>
    /// <param name="options">Width, colour and glow; <see langword="null"/> for a glowing line in the next palette colour.</param>
    /// <param name="name">The line entity's name.</param>
    /// <returns>The line entity, already parented to <see cref="Root"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    public Entity AddLine(IReadOnlyList<Vector3> points, PolylineOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        options ??= new PolylineOptions
        {
            Width = 0.06f,
            EmissiveIntensity = 2.5f,
            Color = Palette[_plotCount % Palette.Length],
        };

        _plotCount++;

        var line = _game.CreatePolyline(points, options, name ?? $"Line {_plotCount}");
        line.Transform.Position = new Vector3(0f, 0f, 2f * LayerStep);
        Root.AddChild(line);

        return line;
    }

    private void BuildAxes()
    {
        var o = Options;

        // Each axis sits on the other coordinate's zero, or on the nearest edge when zero is out of range
        var axisY = Math.Clamp(0f, o.YMin, o.YMax);
        var axisX = Math.Clamp(0f, o.XMin, o.XMax);

        Root.AddChild(_game.CreatePolyline(
            [new Vector3(o.XMin, axisY, 0f), new Vector3(o.XMax, axisY, 0f)],
            new PolylineOptions { Width = o.AxisWidth, Color = o.XAxisColor },
            "X axis"));

        Root.AddChild(_game.CreatePolyline(
            [new Vector3(axisX, o.YMin, 0f), new Vector3(axisX, o.YMax, 0f)],
            new PolylineOptions { Width = o.AxisWidth, Color = o.YAxisColor },
            "Y axis"));
    }

    private void BuildTicks()
    {
        var o = Options;
        var axisY = Math.Clamp(0f, o.YMin, o.YMax);
        var axisX = Math.Clamp(0f, o.XMin, o.XMax);
        var half = o.TickLength * 0.5f;

        var xTicks = new List<(Vector3, Vector3)>();
        foreach (var x in TickValues(o.XMin, o.XMax))
        {
            xTicks.Add((new Vector3(x, axisY - half, 0f), new Vector3(x, axisY + half, 0f)));
        }

        var yTicks = new List<(Vector3, Vector3)>();
        foreach (var y in TickValues(o.YMin, o.YMax))
        {
            yTicks.Add((new Vector3(axisX - half, y, 0f), new Vector3(axisX + half, y, 0f)));
        }

        if (xTicks.Count > 0)
        {
            var ticks = _game.CreateSegments(xTicks, new PolylineOptions { Width = o.TickWidth, Color = o.XAxisColor }, "X ticks");
            ticks.Transform.Position = new Vector3(0f, 0f, LayerStep);
            Root.AddChild(ticks);
        }

        if (yTicks.Count > 0)
        {
            var ticks = _game.CreateSegments(yTicks, new PolylineOptions { Width = o.TickWidth, Color = o.YAxisColor }, "Y ticks");
            ticks.Transform.Position = new Vector3(0f, 0f, LayerStep);
            Root.AddChild(ticks);
        }
    }

    private ModelComponent? BuildGrid()
    {
        var o = Options;
        var lines = new List<(Vector3, Vector3)>();

        foreach (var x in TickValues(o.XMin, o.XMax))
        {
            lines.Add((new Vector3(x, o.YMin, 0f), new Vector3(x, o.YMax, 0f)));
        }

        foreach (var y in TickValues(o.YMin, o.YMax))
        {
            lines.Add((new Vector3(o.XMin, y, 0f), new Vector3(o.XMax, y, 0f)));
        }

        if (lines.Count == 0)
        {
            return null;
        }

        var grid = _game.CreateSegments(lines, new PolylineOptions { Width = o.GridWidth, Color = o.GridColor }, "Grid");
        grid.Transform.Position = new Vector3(0f, 0f, -LayerStep);
        Root.AddChild(grid);

        var model = grid.Get<ModelComponent>()!;
        model.Enabled = o.GridVisible;

        return model;
    }

    private void BuildLabels()
    {
        var o = Options;
        var axisY = Math.Clamp(0f, o.YMin, o.YMax);
        var axisX = Math.Clamp(0f, o.XMin, o.XMax);
        var gap = o.TickLength * 0.5f + o.LabelHeight * 0.25f;

        foreach (var x in TickValues(o.XMin, o.XMax))
        {
            // The origin is labelled once, by the y axis, so the two "0"s do not overlap
            if (IsZero(x) && IsZero(axisY))
                continue;

            AddLabel(x, new Vector3(x, axisY - gap, 0f), TextAnchor.TopCenter);
        }

        foreach (var y in TickValues(o.YMin, o.YMax))
        {
            AddLabel(y, new Vector3(axisX - gap, y, 0f), TextAnchor.MiddleRight);
        }
    }

    private void AddLabel(float value, Vector3 position, TextAnchor anchor)
    {
        var o = Options;

        var label = new Entity($"Label {value.ToString(o.LabelFormat, CultureInfo.InvariantCulture)}")
        {
            new WorldTextComponent
            {
                Text = value.ToString(o.LabelFormat, CultureInfo.InvariantCulture),
                Height = o.LabelHeight,
                TextColor = o.LabelColor,
                Anchor = anchor,
                Billboard = true,
                KeepUpright = true,
            },
        };

        label.Transform.Position = position;
        Root.AddChild(label);
    }

    /// <summary>
    /// Every multiple of <see cref="ChartOptions.TickStep"/> within [<paramref name="min"/>, <paramref name="max"/>],
    /// computed from integer multiples so accumulated float error cannot drop the last tick.
    /// </summary>
    private IEnumerable<float> TickValues(float min, float max)
    {
        var step = Options.TickStep;

        if (step <= 0f)
            yield break;

        var first = (int)MathF.Ceiling(min / step - 1e-4f);
        var last = (int)MathF.Floor(max / step + 1e-4f);

        for (var i = first; i <= last; i++)
        {
            yield return i * step;
        }
    }

    private static bool IsZero(float value) => MathF.Abs(value) < 1e-5f;
}