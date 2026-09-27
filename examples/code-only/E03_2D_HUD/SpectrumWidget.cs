using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>Bars from a moving spectrum, each one rectangle. The tall ones take the text colour.</summary>
public sealed class SpectrumWidget(ShipState ship) : HudWidget
{
    private const string Name = "spectrum";
    private const float Tall = 0.85f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name);
        var plot = new HudRect(content.Left, content.Bottom, content.Width, content.Height - 0.3f);
        var bars = ship.Spectrum;

        canvas.Text(new(Name, "caption"), "IN", new Vector2(content.Left, content.Top - 0.08f), HudText.Caption.Left, theme.For(HudRole.Frame));

        for (var i = 0; i < bars.Length; i++)
        {
            var bar = plot.Column(i, bars.Length);
            var height = MathF.Max(0.04f, plot.Height * bars[i]);
            var colour = bars[i] > Tall ? theme.Text : theme.For(HudRole.Engaged);

            canvas.Style(0f, 0.9f, colour.WithAlpha(0.4f), gradientTo: colour);
            canvas.Box(new Vector2(bar.Center.X, plot.Bottom + height / 2f), new Vector2(bar.Width * 0.6f, height), colour);
        }
    }
}