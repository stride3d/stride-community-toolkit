using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Lines;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Globalization;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// A 2D chart in the XY plane: axes, tick marks, tick labels, an optional major and minor grid, and any
/// number of plotted curves - all children of one <see cref="Root"/> entity, so the whole chart can be
/// placed, rotated and scaled in the world as a single object.
/// </summary>
/// <remarks>
/// Every line is a ribbon mesh from <see cref="PolylineMeshBuilder"/>, so it has real thickness. Labels are
/// either world text that scales with the chart or screen text of a fixed pixel size, chosen by
/// <see cref="ChartOptions.LabelMode"/>. Create a chart with <see cref="Create"/>, add its <see cref="Root"/>
/// to a scene, then call <see cref="Plot"/> for each curve.
/// </remarks>
public sealed class Chart
{
    // Every ribbon lies in the chart plane, so coplanar ones z-fight where they cross and flicker dark
    // fringes. Each layer is nudged along Z by this much: grids behind the axes, ticks and curves in front.
    private const float LayerStep = 0.005f;

    private readonly Game _game;
    private readonly List<ModelComponent> _gridModels = [];
    private int _plotCount;

    /// <summary>The entity every part of the chart is parented to. Add it to a scene and move it to place the chart.</summary>
    public Entity Root { get; }

    /// <summary>The settings the chart was created with.</summary>
    public ChartOptions Options { get; }

    /// <summary>Shows or hides the major and minor grid. Cheap to toggle every frame; the meshes are built once.</summary>
    public bool GridVisible
    {
        get => _gridModels.Count > 0 && _gridModels[0].Enabled;
        set
        {
            foreach (var model in _gridModels)
                model.Enabled = value;
        }
    }

    private Chart(Game game, ChartOptions options, string name)
    {
        _game = game;
        Options = options;
        Root = new Entity(name);

        BuildAxes();
        BuildTicks();
        BuildGrids();

        if (options.ShowLabels)
        {
            BuildLabels();
        }
    }

    /// <summary>
    /// Builds a chart. The <see cref="Root"/> is not added to a scene; do that where you want it.
    /// </summary>
    /// <param name="game">The game the chart is drawn in.</param>
    /// <param name="options">Ranges, ticks, grid, labels and curve defaults; <see langword="null"/> for <see cref="ChartOptions.Glow3D"/>.</param>
    /// <param name="name">The root entity's name, or <c>"Chart"</c>.</param>
    /// <returns>The chart, ready for <see cref="Plot"/> calls.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="game"/> is <see langword="null"/>.</exception>
    public static Chart Create(Game game, ChartOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        options ??= ChartOptions.Glow3D();

        if (options.ShowLabels)
        {
            // Text never appears without its renderer; registering twice is harmless
            if (options.LabelMode == ChartLabelMode.Screen)
                game.AddEntityTextRenderer();
            else
                game.AddWorldTextRenderer();
        }

        return new Chart(game, options, name ?? "Chart");
    }

    /// <summary>
    /// Plots <c>y = f(x)</c> across the chart's <c>x</c> range.
    /// </summary>
    /// <param name="f">The function to plot.</param>
    /// <param name="options">Width, colour and glow; <see langword="null"/> for the chart's curve defaults and the next palette colour.</param>
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
    /// <param name="options">Width, colour and glow; <see langword="null"/> for the chart's curve defaults and the next palette colour.</param>
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
    /// <param name="options">Width, colour and glow; <see langword="null"/> for the chart's curve defaults and the next palette colour.</param>
    /// <param name="name">The line entity's name.</param>
    /// <returns>The line entity, already parented to <see cref="Root"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="points"/> is <see langword="null"/>.</exception>
    public Entity AddLine(IReadOnlyList<Vector3> points, PolylineOptions? options = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        var palette = Options.CurvePalette;

        options ??= new PolylineOptions
        {
            Width = Options.CurveWidth,
            EmissiveIntensity = Options.CurveEmissiveIntensity,
            Color = palette.Length > 0 ? palette[_plotCount % palette.Length] : Color.White,
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
        foreach (var x in TickValues(o.XMin, o.XMax, o.TickStep))
        {
            xTicks.Add((new Vector3(x, axisY - half, 0f), new Vector3(x, axisY + half, 0f)));
        }

        var yTicks = new List<(Vector3, Vector3)>();
        foreach (var y in TickValues(o.YMin, o.YMax, o.TickStep))
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

    private void BuildGrids()
    {
        var o = Options;

        // Minor grid sits behind the major grid, and skips the lines the major grid already draws
        if (o.MinorDivisions > 1)
        {
            var minorStep = o.TickStep / o.MinorDivisions;
            AddGrid(GridLines(minorStep, skipMultiplesOf: o.TickStep), o.MinorGridWidth, o.MinorGridColor, "Minor grid", -2f * LayerStep);
        }

        AddGrid(GridLines(o.TickStep, skipMultiplesOf: null), o.GridWidth, o.GridColor, "Grid", -LayerStep);
    }

    private List<(Vector3, Vector3)> GridLines(float step, float? skipMultiplesOf)
    {
        var o = Options;
        var lines = new List<(Vector3, Vector3)>();

        foreach (var x in TickValues(o.XMin, o.XMax, step))
        {
            if (skipMultiplesOf is { } major && IsMultiple(x, major))
                continue;

            lines.Add((new Vector3(x, o.YMin, 0f), new Vector3(x, o.YMax, 0f)));
        }

        foreach (var y in TickValues(o.YMin, o.YMax, step))
        {
            if (skipMultiplesOf is { } major && IsMultiple(y, major))
                continue;

            lines.Add((new Vector3(o.XMin, y, 0f), new Vector3(o.XMax, y, 0f)));
        }

        return lines;
    }

    private void AddGrid(List<(Vector3, Vector3)> lines, float width, Color color, string name, float z)
    {
        if (lines.Count == 0)
            return;

        var grid = _game.CreateSegments(lines, new PolylineOptions { Width = width, Color = color }, name);
        grid.Transform.Position = new Vector3(0f, 0f, z);
        Root.AddChild(grid);

        var model = grid.Get<ModelComponent>()!;
        model.Enabled = Options.GridVisible;
        _gridModels.Add(model);
    }

    private void BuildLabels()
    {
        var o = Options;
        var axisY = Math.Clamp(0f, o.YMin, o.YMax);
        var axisX = Math.Clamp(0f, o.XMin, o.XMax);
        var gap = o.TickLength * 0.5f + (o.LabelMode == ChartLabelMode.World ? o.LabelHeight * 0.25f : 0f);

        foreach (var x in TickValues(o.XMin, o.XMax, o.TickStep))
        {
            // The origin is labelled once, by the y axis, so the two "0"s do not overlap
            if (IsZero(x) && IsZero(axisY))
                continue;

            AddLabel(x, new Vector3(x, axisY - gap, 0f), TextAnchor.TopCenter);
        }

        foreach (var y in TickValues(o.YMin, o.YMax, o.TickStep))
        {
            AddLabel(y, new Vector3(axisX - gap, y, 0f), TextAnchor.MiddleRight);
        }
    }

    private void AddLabel(float value, Vector3 position, TextAnchor anchor)
    {
        var o = Options;
        var text = value.ToString(o.LabelFormat, CultureInfo.InvariantCulture);
        var label = new Entity($"Label {text}");

        if (o.LabelMode == ChartLabelMode.Screen)
        {
            // A few pixels of breathing room beyond the tick, in the direction the anchor pushes the text
            var offset = anchor == TextAnchor.TopCenter ? new Vector2(0f, 4f) : new Vector2(-4f, 0f);

            label.Add(new EntityTextComponent
            {
                Text = text,
                FontSize = o.LabelFontSize,
                TextColor = o.LabelColor,
                Anchor = anchor,
                Offset = offset,
            });
        }
        else
        {
            label.Add(new WorldTextComponent
            {
                Text = text,
                Height = o.LabelHeight,
                TextColor = o.LabelColor,
                Anchor = anchor,
                Billboard = true,
                KeepUpright = true,
            });
        }

        label.Transform.Position = position;
        Root.AddChild(label);
    }

    /// <summary>
    /// Every multiple of <paramref name="step"/> within [<paramref name="min"/>, <paramref name="max"/>],
    /// computed from integer multiples so accumulated float error cannot drop the last one.
    /// </summary>
    private static IEnumerable<float> TickValues(float min, float max, float step)
    {
        if (step <= 0f)
            yield break;

        var first = (int)MathF.Ceiling(min / step - 1e-4f);
        var last = (int)MathF.Floor(max / step + 1e-4f);

        for (var i = first; i <= last; i++)
        {
            yield return i * step;
        }
    }

    private static bool IsMultiple(float value, float step)
    {
        var ratio = value / step;
        return MathF.Abs(ratio - MathF.Round(ratio)) < 1e-4f;
    }

    private static bool IsZero(float value) => MathF.Abs(value) < 1e-5f;
}
