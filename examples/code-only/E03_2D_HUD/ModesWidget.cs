using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The mode buttons. An engaged mode is lit: filled in the accent with dark lettering. A mode the
/// ship does not have is the same button at a quarter of its opacity. Click a button to switch it.
/// </summary>
public sealed class ModesWidget(ShipState ship) : HudWidget
{
    private const string Name = "modes";
    private const float Gap = 0.2f;
    private const float Unavailable = 0.25f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        for (var i = 0; i < ship.Modes.Length; i++)
        {
            DrawButton(canvas, i, bounds.Column(i, ship.Modes.Length, Gap));
        }
    }

    private void DrawButton(HudCanvas canvas, int index, HudRect button)
    {
        var theme = canvas.Theme;
        var mode = ship.Modes[index];
        var id = new HudId(Name, Index: index);
        var accent = theme.For(HudRole.Engaged);

        if (mode.Available && canvas.Clicks(id)) mode.Engaged = !mode.Engaged;

        var engaged = canvas.Animate(new(Name, "engaged", index), mode.Engaged ? 1f : 0f);
        var hover = canvas.Animate(new(Name, "hover", index), mode.Available && canvas.Hovers(id) ? 1f : 0f);
        var opacity = mode.Available ? 1f : Unavailable;

        canvas.Style(
            border: MathUtil.Lerp(HudCanvas.Thin, HudCanvas.Thick, MathF.Max(engaged, hover)),
            fillAlpha: MathUtil.Lerp(0.5f, 0.9f, engaged),
            fill: Color.Lerp(theme.Ground, accent, engaged),
            glow: 4f * engaged + 3f * hover,
            glowColour: theme.GlowFor(HudRole.Engaged),
            opacity: opacity,
            additive: true);

        canvas.Shapes.Tag = id;
        canvas.ChamferedPanel(button.Center, button.Size, 0.16f, Color.Lerp(accent, theme.Text, engaged));
        canvas.Shapes.Tag = null;

        // The decoy button carries its count on a second line
        var caption = mode.Name == "DECOY" ? $"{mode.Name}\n{ship.Decoys}" : mode.Name;

        canvas.Text(new(Name, "caption", index), caption, button.Center, HudText.Caption, Color.Lerp(theme.Text, HudTheme.Ink, engaged).WithAlpha(opacity));
    }
}