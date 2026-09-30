using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The annunciator under the sight: amber when the shield needs attention, red when it needs it
/// now, and quietly nominal the rest of the time. The one widget whose colour no scheme decides.
/// </summary>
public sealed class WarningWidget(ShipState ship) : HudWidget
{
    private const string Name = "warning";
    private const float Low = 0.35f;
    private const float Critical = 0.15f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var size = new Vector2(5.4f, MathF.Min(0.62f, bounds.Height));
        var shield = ship.Systems.Shield;

        if (shield >= Low)
        {
            canvas.Style(HudCanvas.Thin, 0.3f, theme.Ground);
            canvas.ChamferedPanel(bounds.Center, size, 0.14f, theme.Dim(HudRole.Engaged));
            canvas.Text(Name, "ALL SYSTEMS NOMINAL", bounds.Center, HudText.Caption, theme.For(HudRole.Engaged).WithAlpha(0.6f), Color.Transparent);

            return;
        }

        var colour = shield < Critical ? HudTheme.Warning : HudTheme.Caution;
        var pulse = 0.5f + 0.5f * MathF.Sin(canvas.Time * 6f);

        // The panel is the lamp. Its lettering is dark on it and its glow spills onto the glass.
        canvas.Style(new HudStyle(HudCanvas.Thin, 0.55f + 0.35f * pulse, colour) { Glow = 6f * pulse, GlowColour = colour, Additive = true });
        canvas.ChamferedPanel(bounds.Center, size, 0.14f, colour);
        canvas.Text(Name, shield < Critical ? "SHIELD CRITICAL" : "SHIELD LOW", bounds.Center, HudText.Caption, HudTheme.Ink, Color.Transparent);
    }
}