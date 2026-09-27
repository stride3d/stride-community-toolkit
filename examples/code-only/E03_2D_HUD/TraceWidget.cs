using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The last few seconds of two signals, each one stroke. Every point is the signal at the moment
/// its x stands for, so the trace scrolls a little every frame and never steps.
/// </summary>
public sealed class TraceWidget : HudWidget
{
    private const string Name = "trace";
    private const int Points = 96;
    private const float SecondsShown = 6f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name);
        var plot = new HudRect(content.Left, content.Bottom, content.Width, content.Height - 0.3f);

        canvas.Text(new(Name, "caption"), "PMT", new Vector2(content.Left, content.Top - 0.08f), HudText.Caption.Left, theme.For(HudRole.Frame));

        // Baseline
        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Line(new Vector2(plot.Left, plot.Bottom), new Vector2(plot.Right, plot.Bottom), HudCanvas.Thin, theme.Dim(HudRole.Frame));

        Span<Vector2> points = stackalloc Vector2[Points];

        // The slower signal behind: thinner and dimmer
        Sample(points, plot, canvas.Time, ShipState.Reference);

        canvas.Style(1f, 0f);
        canvas.Shapes.DrawPixelPolyline(points, 1f, theme.For(HudRole.Commanded).WithAlpha(0.6f));

        // The signal in front, glowing like a phosphor trace
        Sample(points, plot, canvas.Time, ShipState.Signal);

        canvas.Style(HudCanvas.Thin, 0f, glow: 4f, glowColour: theme.Glow, additive: true);
        canvas.Shapes.DrawPixelPolyline(points, HudCanvas.Thin, theme.Text);
    }

    private static void Sample(Span<Vector2> points, HudRect plot, float now, Func<float, float> signal)
    {
        for (var i = 0; i < points.Length; i++)
        {
            var along = i / (points.Length - 1f);

            points[i] = plot.At(along, signal(now - (1f - along) * SecondsShown));
        }
    }
}