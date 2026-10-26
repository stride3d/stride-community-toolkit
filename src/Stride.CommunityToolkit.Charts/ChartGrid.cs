using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The chart's grid: pixel-width lines on every tick value, drawn each frame on whichever coordinate planes
/// <see cref="ChartGridOptions.Planes"/> asks for, the minor grid first and the major on top. Nothing is
/// built or kept, so every grid option is live, and a view-driven chart's grid covers the screen simply
/// because its ranges do.
/// </summary>
/// <param name="chart">The chart whose ranges and grid options are read each frame.</param>
internal sealed class ChartGrid(Chart chart)
{
    /// <summary>Submits the grid lines for this frame, or nothing while the grid is hidden.</summary>
    internal void Draw(ShapeBatch batch, in ChartView view)
    {
        var o = chart.Options;

        if (!o.Grid.Visible)
            return;

        var planes = chart.Is3D ? o.Grid.Planes : o.Grid.Planes & ChartGridPlanes.XY;
        var step = o.Range.TickStep;

        // A span over constants is read from the assembly's data, so the loop allocates nothing per frame
        ReadOnlySpan<ChartGridPlanes> all = [ChartGridPlanes.XY, ChartGridPlanes.XZ, ChartGridPlanes.YZ];

        foreach (var plane in all)
        {
            if ((planes & plane) == 0)
                continue;

            if (o.Range.MinorDivisions > 1)
            {
                DrawLines(batch, in view, plane, new Weight(step / o.Range.MinorDivisions, step, o.Grid.MinorColor, o.Grid.MinorWidth, -2f * Chart.LayerStep));
            }

            DrawLines(batch, in view, plane, new Weight(step, 0f, o.Grid.Color, o.Grid.Width, -Chart.LayerStep));
        }
    }

    /// <summary>
    /// The lines of one weight on one plane: at every multiple of the weight's step along each of the
    /// plane's two axes, spanning the ranges of those axes.
    /// </summary>
    private void DrawLines(ShapeBatch batch, in ChartView view, ChartGridPlanes plane, in Weight w)
    {
        var r = chart.Options.Range;

        // The plane sits on the third coordinate's zero, or its nearest edge, nudged behind the axes
        var anchor = new Vector3(
            Math.Clamp(0f, r.XMin, r.XMax) + w.Offset,
            Math.Clamp(0f, r.YMin, r.YMax) + w.Offset,
            (chart.Is3D ? Math.Clamp(0f, r.ZMin, r.ZMax) : 0f) + w.Offset);

        // Each case: first the lines across one spanned axis, then across the other, at the anchor on the
        // third
        switch (plane)
        {
            case ChartGridPlanes.XZ:
                foreach (var x in w.Ticks(r.XMin, r.XMax))
                {
                    Line(batch, in view, new Vector3(x, anchor.Y, r.ZMin), new Vector3(x, anchor.Y, r.ZMax), in w);
                }

                foreach (var z in w.Ticks(r.ZMin, r.ZMax))
                {
                    Line(batch, in view, new Vector3(r.XMin, anchor.Y, z), new Vector3(r.XMax, anchor.Y, z), in w);
                }

                break;

            case ChartGridPlanes.YZ:
                foreach (var y in w.Ticks(r.YMin, r.YMax))
                {
                    Line(batch, in view, new Vector3(anchor.X, y, r.ZMin), new Vector3(anchor.X, y, r.ZMax), in w);
                }

                foreach (var z in w.Ticks(r.ZMin, r.ZMax))
                {
                    Line(batch, in view, new Vector3(anchor.X, r.YMin, z), new Vector3(anchor.X, r.YMax, z), in w);
                }

                break;

            default:
                foreach (var x in w.Ticks(r.XMin, r.XMax))
                {
                    Line(batch, in view, new Vector3(x, r.YMin, anchor.Z), new Vector3(x, r.YMax, anchor.Z), in w);
                }

                foreach (var y in w.Ticks(r.YMin, r.YMax))
                {
                    Line(batch, in view, new Vector3(r.XMin, y, anchor.Z), new Vector3(r.XMax, y, anchor.Z), in w);
                }

                break;
        }
    }

    private static void Line(ShapeBatch batch, in ChartView view, Vector3 from, Vector3 to, in Weight w)
        => batch.DrawPixelLine(view.ToWorld(from), view.ToWorld(to), w.Width, w.Color);

    /// <summary>
    /// One weight of grid line: its spacing, the spacing of the other weight whose lines it leaves out
    /// (0 for none), its colour and pixel width, and how far behind the axes it sits.
    /// </summary>
    private readonly record struct Weight(float Step, float Skip, Color Color, float Width, float Offset)
    {
        /// <summary>The tick values of this weight's step, leaving out those that are also multiples of <see cref="Skip"/>; walked without allocating.</summary>
        internal TickRange Ticks(float min, float max) => TickRange.Of(min, max, Step, Skip);
    }
}