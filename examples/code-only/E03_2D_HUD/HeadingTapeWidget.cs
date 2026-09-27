using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The compass strip: a window onto a 360-degree ruler that scrolls as the heading changes, with
/// the heading boxed under a centre marker. The tick labels are ten labels, reused for whichever
/// multiples of ten are in the window.
/// </summary>
public sealed class HeadingTapeWidget(ShipState ship) : HudWidget
{
    private const string Name = "heading";
    private const float DegreesShown = 80f;
    private const int MaxLabels = 10;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var colour = theme.For(HudRole.Navigation);
        var heading = ship.Flight.Heading;
        var center = new Vector2(bounds.Center.X, bounds.Top - 0.75f);
        var half = bounds.Width / 2f - 0.4f;
        var unitsPerDegree = 2f * half / DegreesShown;

        // The rule, then the ticks along it
        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Line(center - new Vector2(half, 0f), center + new Vector2(half, 0f), HudCanvas.Thin, colour);

        var label = 0;
        var firstTick = MathF.Floor((heading - DegreesShown / 2f) / 5f) * 5f;

        for (var degrees = firstTick; degrees <= heading + DegreesShown / 2f; degrees += 5f)
        {
            var x = center.X + (degrees - heading) * unitsPerDegree;

            if (MathF.Abs(x - center.X) > half) continue;

            var major = MathF.Abs(degrees % 10f) < 0.01f;

            canvas.Line(new Vector2(x, center.Y), new Vector2(x, center.Y + (major ? 0.3f : 0.15f)), HudCanvas.Thin, colour);

            if (!major || label == MaxLabels) continue;

            canvas.Text(new(Name, "tick", label), $"{Wrap(degrees):000}", new Vector2(x, center.Y + 0.52f), HudText.Small, colour);

            label++;
        }

        // The centre marker and the heading under it
        var marker = center - new Vector2(0f, 0.1f);
        var readout = center - new Vector2(0f, 0.78f);

        canvas.Line(marker, marker + new Vector2(-0.2f, -0.28f), HudCanvas.Thick, theme.Text);
        canvas.Line(marker, marker + new Vector2(0.2f, -0.28f), HudCanvas.Thick, theme.Text);

        canvas.Style(HudCanvas.Thin, 0.9f, theme.Ground);
        canvas.ChamferedPanel(readout, new Vector2(1.6f, 0.6f), 0.12f, colour);
        canvas.Text(new(Name, "value"), $"{Wrap(heading):000}", readout, HudText.Figure, theme.Text, theme.GlowFor(HudRole.Navigation));

        // What the autopilot holds, either side of the heading
        DrawTag(canvas, "track", "TRK", readout - new Vector2(4.4f, 0f), colour);
        DrawTag(canvas, "nav", "NAV 2", readout + new Vector2(4.4f, 0f), colour);
    }

    private static void DrawTag(HudCanvas canvas, string part, string text, Vector2 center, Color colour)
    {
        canvas.Style(HudCanvas.Thin, 0.6f, canvas.Theme.Ground);
        canvas.ChamferedPanel(center, new Vector2(1.9f, 0.5f), 0.1f, colour.WithAlpha(0.7f));
        canvas.Text(new(Name, part), text, center, HudText.Caption, colour);
    }

    private static float Wrap(float degrees) => (degrees % 360f + 360f) % 360f;
}