# Charts - plotting in a scene, step by step

`Stride.CommunityToolkit.Charts` draws a chart in your scene: axes, grid, labels, a legend, and any
number of curves, points, fills and trails, all as pixel-width strokes in a [ShapeBatch](shape-batch.md)
so they keep their weight at any zoom or distance. A chart is three things:

- **`Options`** - everything about how it looks and what range it shows. Live: change a value, the
  next frame applies it.
- **Series** - what is plotted. Added with `Plot`, `PlotParametric`, `AddLine`, `AddMarkers`, `AddArea`,
  `AddTrajectory`; taken off with `Remove` or `Clear`.
- **`Update(camera)`** - call it once a frame. Nothing draws without it.

The package is preview and not published yet: reference the project
`src/Stride.CommunityToolkit.Charts` from your own. The two examples this page draws on are
[Charts 2D](../code-only/examples/charts-2d.md) and [Charts 3D](../code-only/examples/charts-3d.md).

## 1. An empty chart with a grid

A 2D compositor, a 2D camera with the pan-and-zoom controller, one chart added to the scene, and
`Update` in the loop:

```csharp
using Stride.CommunityToolkit.Charts;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

using var game = new Game();

Chart? chart = null;
CameraComponent? camera = null;

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.Add2DGraphicsCompositor(clearColor: new Color(250, 250, 250));
    camera = game.Add2DCamera().Get<CameraComponent>();
    game.Add2DCameraController();

    var options = ChartOptions.Light2D();

    options.Range.XMin = -5f;
    options.Range.XMax = 5f;
    options.Range.YMin = -4f;
    options.Range.YMax = 4f;

    chart = new Chart(game, options);
    chart.Root.Scene = scene;
}

void Update(Scene scene, GameTime time)
{
    if (chart is not null && camera is not null)
    {
        chart.Update(camera);
    }
}
```

`ChartOptions.Light2D()` is the paper look: dark axes, a grey major and minor grid, no glow, labels
that keep their pixel size. `ChartOptions.Glow3D()` is the other preset, glowing lines on a dark
ground, and the default when you pass nothing. Both are ordinary option objects; change whatever you
like after picking one.

The grid, ticks and labels come from `Range`:

```csharp
options.Range.TickStep = 0.5f;       // a tick, a label and a major grid line every 0.5
options.Range.MinorDivisions = 5;    // five minor cells per major one; 0 or 1 for none

options.Grid.Visible = true;         // Light2D has it on, Glow3D off
options.Grid.Color = new Color(190, 190, 190);
options.Grid.Width = 1.5f;           // pixels, like every width on a chart
options.Grid.MinorColor = new Color(228, 228, 228);
```

Titles and axis names go in `Title` and `Axes`; the axes' colours, width and tick length are there too:

```csharp
options.Title.Text = "Charts 2D";
options.Axes.XTitle = "x";
options.Axes.YTitle = "y";
options.Axes.Width = 1.5f;
options.Axes.TickLength = 8f;
```

`Labels.Mode` chooses between `Screen` text, a font size in pixels that never changes with zoom, and
`World` text, a height in chart units that scales with the chart. `Light2D` uses `Screen`, `Glow3D`
uses `World`; `Labels.Format` is the number format of every tick.

## 2. Lines

`Plot` takes a function of `x` and samples it across the chart's `x` range. Colours come from
`Options.Series.Palette` in turn unless you pass one:

```csharp
chart.Plot(x => 2f * MathF.Sin(x), name: "sin");
chart.Plot(x => 0.15f * x * x - 3f, name: "parabola");
chart.Plot(MathF.Log, name: "ln");                          // starts where its domain does
chart.Plot(MathF.Tan, name: "tan", samples: 600);           // cut into branches at the asymptotes
chart.Plot(MathF.Cos, color: new Color(180, 180, 180), name: "cos");
```

Values that are not finite break the curve, values outside the `y` range are clipped to the edge,
and a jump between two samples larger than a quarter of the chart's height is treated as an
asymptote rather than joined. More `samples` follow a wiggly function more closely.

Width and glow are per series through a style; whatever the style leaves unset comes from
`Options.Series`, live:

```csharp
chart.Plot(MathF.Sin, style: new ChartSeriesStyle { Width = 1.25f });        // half the default 2.5 px
chart.Plot(MathF.Cos, style: new ChartSeriesStyle { Glow = 0f });            // no halo, whatever the chart's glow

options.Series.CurveWidth = 3f;   // every series without a Width of its own
```

Every series you add is returned, and it is what you `Remove` later. The name is what the legend
shows; `Options.Legend.Visible` turns the legend off.

## 3. Shapes and point data

A parametric curve is a function of `t` returning a point; `closed` joins the last point back to the
first, which is how a circle or any loop is drawn:

```csharp
chart.PlotParametric(
    t => new Vector3(1.5f * MathF.Cos(t), 1.5f * MathF.Sin(t), 0f), 0f, MathUtil.TwoPi,
    name: "circle", samples: 96, closed: true);
```

Measured data or a hand-drawn shape is a list of points:

```csharp
var points = new List<Vector3> { new(-4, -1, 0), new(-2, 2, 0), new(1, 0.5f, 0), new(3, 3, 0) };

chart.AddLine(points, name: "measured");                    // clipped to the ranges
chart.AddLine(points, name: "outline", closed: true);       // a polygon
chart.AddLine(points, name: "free", clip: false);           // may leave the chart
```

Markers are one × per point, sized in pixels, so they stay the same size at any zoom:

```csharp
chart.AddMarkers(points, name: "samples");                  // Options.Series.MarkerSize, MarkerWidth
chart.AddMarkers(points, size: 12f, width: 2f, color: new Color(96, 66, 166));
```

A filled region is the area between a function and a baseline, or between two functions, over a
stretch of `x`. It is translucent at `Options.Series.AreaOpacity` and drawn behind the curves, so
the grid shows through it:

```csharp
chart.AddArea(x => 2f * MathF.Sin(x), from: 0f, to: MathF.PI, name: "integral");
chart.AddArea(x => 2f * MathF.Sin(x), x => MathF.Cos(x), from: -2f, to: 2f, name: "between");

options.Series.AreaOpacity = 0.4f;
```

## 4. Animation

Everything is live, so animation is writing a value each frame. To animate a curve, keep what
`Plot` returned and swap its function; it keeps its name, colour and legend row:

```csharp
ChartCurve? wave = null;
var elapsed = 0f;

// in Start
wave = chart.Plot(x => MathF.Sin(x), color: new Color(0, 158, 150), name: "wave");

// in Update
elapsed += (float)time.Elapsed.TotalSeconds;
var k = 1.75f + 1.25f * MathF.Sin(elapsed * 0.9f);
wave.SetFunction(x => 1.5f * MathF.Sin(k * x));
```

A trajectory is a curve that grows one point at a time - the path of something moving. Add a point
per frame; points outside the ranges are clipped, so the trail runs to the edge and resumes where
the path comes back:

```csharp
var trail = chart.AddTrajectory(capacity: 900, name: "throw");

// in Update, after moving the body
trail.Add(new Vector3(position.X, position.Y, 0f));

trail.Clear();       // start again, keeping the buffers
trail.Break();       // lift the pen: the next Add starts a new run
```

With `rollOver: true` a full trail drops its oldest point instead of ignoring the new one, which
gives an oscilloscope trace. `Count` and `Capacity` say how full it is.

Options animate the same way. Panning is writing the range, glow is writing a number:

```csharp
chart.Options.Range.XMin += speed * dt;      // the axes, grid, labels and curves follow
chart.Options.Range.XMax += speed * dt;

chart.Options.Series.Glow = 6f + 4f * MathF.Sin(elapsed);
chart.Options.Grid.Visible = !chart.Options.Grid.Visible;
```

The cheapest animation of all is letting the user do it. With `Range.FollowCamera` the chart's
ranges become whatever the camera sees, and tick steps are re-picked as you zoom, so the 2D
controller's drag and wheel pan and zoom an endless chart:

```csharp
chart.Options.Range.FollowCamera = true;
```

A cursor readout follows the mouse; `Chart.CursorPosition` is the same point in chart units, for
your own code to read a value against:

```csharp
chart.Options.Cursor.Visible = true;
chart.Options.Cursor.Format = "0.00";

if (chart.CursorPosition is { } p)
{
    // p.X, p.Y in chart units
}
```

## 5. 3D

A chart is 3D when its `Z` range has a spread. It gains a `Z` axis, its clipping becomes a box, and
its grid can cover the floor and the back wall as well:

```csharp
game.AddGraphicsCompositor(clearColor: new Color(16, 18, 28));
camera = game.Add3DCamera().Get<CameraComponent>();
game.Add3DCameraController();
game.AddDirectionalLight();

var options = ChartOptions.Glow3D();

options.Range.ZMin = -3f;
options.Range.ZMax = 3f;
options.Grid.Planes = ChartGridPlanes.XY | ChartGridPlanes.XZ;
options.Grid.Visible = true;
options.Axes.ZTitle = "z";

chart = new Chart(game, options);
chart.Root.Transform.Position = new Vector3(0f, 3f, 0f);
chart.Root.Scene = scene;

chart.FrameCamera(camera, padding: 0.12f);    // back the camera off until the whole chart fits
```

Curves at `z = 0` draw exactly as they do flat; the third dimension is for a parametric curve or a
trajectory that uses `z`:

```csharp
chart.PlotParametric(
    t => new Vector3(2.5f * MathF.Cos(t), 2.5f * MathF.Sin(t), t / MathUtil.TwoPi - 2f),
    0f, 5f * MathUtil.TwoPi, name: "helix", samples: 400);
```

The `Glow3D` preset's halo is `Series.Glow` pixels wide at `Series.GlowStrength` of the stroke's
brightness, added to the scene (`Series.AdditiveGlow`). Widen it for a showcase, set it to `0` for
none. A curve that leaves the chart plane, such as the helix or a 3D trail, is a mesh rather than a
stroke, so its glow is an emissive tint under bloom. Everything else is a stroke.

## 6. Sharing the batch, and cleaning up

The chart draws into a depth-tested `ShapeBatch` it creates for itself. To draw your own shapes
into the same batch, create one and pass it; what you submit after `Update` sits over the curves:

```csharp
var batch = game.AddShapeBatch(depthTest: true);

// in Update
chart.Update(camera, batch);
batch.DrawDisc(new Vector3(ball.X, ball.Y, 0.05f), Vector3.UnitZ, 0.12f, new Color(40, 40, 40));
```

`chart.Remove(series)` takes one series off, `chart.Clear()` all of them, and `chart.Dispose()`
frees everything the chart owns, including its batch. Remove `chart.Root` from the scene first.

## Options at a glance

| Group | What it holds | Notes |
|---|---|---|
| `Range` | `XMin`..`ZMax`, `TickStep`, `MinorDivisions`, `FollowCamera` | A `Z` spread makes the chart 3D, decided when it is built |
| `Axes` | `XColor`..`ZColor`, `Width`, `TickLength`, `TickWidth`, `XTitle`..`ZTitle` | Pixels |
| `Grid` | `Visible`, `Planes`, `Color`, `Width`, `MinorColor`, `MinorWidth` | Pixels; `Planes` matters in 3D only |
| `Labels` | `Visible`, `Mode`, `FontSize`, `Height`, `Color`, `Format` | `Screen` uses `FontSize`, `World` uses `Height` |
| `Legend` | `Visible` | Top left, one row per series |
| `Title` | `Text`, `FontSize`, `Height` | Above the top edge |
| `Series` | `CurveWidth`, `Glow`, `GlowStrength`, `AdditiveGlow`, `MarkerSize`, `MarkerWidth`, `AreaOpacity`, `Palette` | Defaults for series without a style; live for those |
| `Cursor` | `Visible`, `Format`, `Radius`, `Glow` | Mouse readout; `Chart.CursorPosition` |

Every width, length, size and glow is in pixels on a 100% display and scales with the display
scale. Ranges are in chart units; scale `chart.Root` to change the chart's size in the world.
