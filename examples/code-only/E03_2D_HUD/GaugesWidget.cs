using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// Three ring gauges in a row: power, temperature and thrust. Each is a dim track, a bright arc
/// that grows clockwise from the top, a tick ring and the figure in the middle. Temperature turns
/// amber when it runs hot.
/// </summary>
public sealed class GaugesWidget(ShipState ship) : HudWidget
{
    private const string Name = "gauges";
    private const float Band = 0.2f;
    private const float Hot = 0.8f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var content = canvas.Panel(bounds, Name);
        var systems = ship.Systems;
        var colour = canvas.Theme.For(HudRole.Engaged);

        DrawRing(canvas, content.Column(0, 3), 0, "POWER", systems.Power, colour);
        DrawRing(canvas, content.Column(1, 3), 1, "TEMPERATURE", systems.Temperature, systems.Temperature > Hot ? HudTheme.Caution : colour);
        DrawRing(canvas, content.Column(2, 3), 2, "THRUST", systems.Thrust, colour);
    }

    private static void DrawRing(HudCanvas canvas, HudRect bounds, int index, string caption, float value, Color colour)
    {
        var theme = canvas.Theme;
        var dim = theme.Dim(HudRole.Engaged);
        var radius = bounds.Height / 2f - 0.2f;
        var center = new Vector2(bounds.Left + radius + 0.3f, bounds.Center.Y);

        value = MathUtil.Clamp(value, 0f, 1f);

        // Track
        canvas.Style(0f, 0.35f, theme.Ground);
        canvas.Shapes.DrawAnnulus(center, radius, radius - Band, dim);

        // Progress, clockwise from twelve o'clock, its glow adding light to the track
        canvas.Style(new HudStyle(0f, 0.95f, colour) { Glow = 3f, GlowColour = colour, Additive = true });
        canvas.Shapes.DrawSector(center, radius, MathF.PI / 2f, -MathF.Tau * value, colour, radius - Band);

        canvas.DashedRing(center, radius + 0.16f, 3f, 5f, dim);

        // The figure inside the ring, the caption and a bar of cells beside it
        var right = new HudRect(center.X + radius + 0.35f, bounds.Bottom, bounds.Right - center.X - radius - 0.45f, bounds.Height);

        canvas.Text(new(Name, "value", index), $"{value * 100f:0}%", center, HudText.Figure, theme.Text, colour);
        canvas.Text(new(Name, "caption", index), caption, new Vector2(right.Left, center.Y + 0.35f), HudText.Caption.Left, colour);
        canvas.SegmentedBar(HudRect.Centered(new Vector2(right.Center.X, center.Y - 0.2f), new Vector2(right.Width, 0.24f)), 8, value, colour, dim);
    }
}