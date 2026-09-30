using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The last few seconds of two signals, each one stroke. The samples are taken at fixed moments of
/// the signal, not at fixed places on the screen, so the stroke keeps its shape and only slides:
/// it scrolls a little every frame, never steps and never shimmers at its peaks.
/// </summary>
public sealed class TraceWidget : HudWidget
{
    private const string Name = "trace";
    private const float SecondsShown = 6f;
    private const float SecondsPerSample = 1f / 24f;
    private const float Margin = 0.001f;

    // The samples inside the window, and one point on each edge
    private const int MaxPoints = (int)(SecondsShown / SecondsPerSample) + 3;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name);
        var plot = new HudRect(content.Left, content.Bottom, content.Width, content.Height - 0.3f);

        canvas.Text(new(Name, "caption"), "PMT", new Vector2(content.Left, content.Top - 0.08f), HudText.Caption.Left, theme.For(HudRole.Frame));

        // Baseline
        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Line(new Vector2(plot.Left, plot.Bottom), new Vector2(plot.Right, plot.Bottom), HudCanvas.Thin, theme.Dim(HudRole.Frame));

        Span<Vector2> points = stackalloc Vector2[MaxPoints];

        // The slower signal behind: thinner and dimmer
        var count = Sample(points, plot, canvas.Time, ShipState.Reference);

        canvas.Style(1f, 0f);
        canvas.Shapes.DrawPixelPolyline(points[..count], 1f, theme.For(HudRole.Commanded).WithAlpha(0.6f));

        // The signal in front, glowing like a phosphor trace
        count = Sample(points, plot, canvas.Time, ShipState.Signal);

        canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 4f, GlowColour = theme.Glow, Additive = true });
        canvas.Shapes.DrawPixelPolyline(points[..count], HudCanvas.Thin, theme.Text);
    }

    /// <summary>
    /// Fills the points of a stroke: one on the left edge, one for every sample moment inside the
    /// window, one on the right edge.
    /// </summary>
    /// <returns>How many points it filled.</returns>
    private static int Sample(Span<Vector2> points, HudRect plot, float now, Func<float, float> signal)
    {
        var start = now - SecondsShown;
        var count = 0;

        points[count++] = plot.At(0f, signal(start));

        // Sample moments are multiples of the step, so the same moments are sampled every frame
        for (var sample = (int)MathF.Floor(start / SecondsPerSample) + 1; sample * SecondsPerSample < now && count < points.Length - 1; sample++)
        {
            var moment = sample * SecondsPerSample;

            // A sample on an edge would double the edge's own point
            if (moment - start < Margin || now - moment < Margin) continue;

            points[count++] = plot.At((moment - start) / SecondsShown, signal(moment));
        }

        points[count++] = plot.At(1f, signal(now));

        return count;
    }
}