using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// A vertical tape with a moving fill, a red-line mark and the value boxed beside it. The speed
/// and the altitude tapes either side of the sight are two of these, mirrored.
/// </summary>
/// <param name="name">The widget's name, which is also what tells the two tapes' labels apart.</param>
/// <param name="caption">The caption above the tape.</param>
/// <param name="value">Reads the figure to show.</param>
/// <param name="range">The figure at the top of the tape.</param>
/// <param name="inward">1 when the sight is to the right of the tape, -1 when it is to the left.</param>
public sealed class TapeGaugeWidget(string name, string caption, Func<float> value, float range, float inward) : HudWidget
{
    private const float Width = 0.42f;
    private const float RedLine = 0.88f;
    private const int Ticks = 20;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var colour = theme.For(HudRole.Navigation);
        var figure = value();
        var level = MathUtil.Clamp(figure / range, 0f, 1f);

        // The track sits on the outer side, the readout between it and the sight
        var track = HudRect.Centered(new Vector2(bounds.Center.X - inward * 0.7f, bounds.Center.Y - 0.3f), new Vector2(Width, bounds.Height - 1f));

        canvas.Style(HudCanvas.Thin, 0.25f, theme.Ground);
        canvas.Box(track.Center, track.Size, theme.Dim(HudRole.Navigation));

        // The fill grows from the bottom and is brightest at its top edge
        canvas.Style(0f, 0.9f, colour.WithAlpha(0.35f), gradientTo: theme.Text);
        canvas.Box(new Vector2(track.Center.X, track.Bottom + track.Height * level / 2f), new Vector2(Width - 0.08f, track.Height * level), colour);

        // Ticks up the inner side, a longer one every fifth
        canvas.Style(HudCanvas.Thin, 0f);

        var edge = track.Center.X + inward * Width / 2f;

        for (var i = 0; i <= Ticks; i++)
        {
            var y = track.Bottom + track.Height * i / Ticks;

            canvas.Line(new Vector2(edge, y), new Vector2(edge + inward * (i % 5 == 0 ? 0.3f : 0.15f), y), HudCanvas.Thin, colour);
        }

        // The red line, a small hollow box on the outer edge
        canvas.Box(new Vector2(track.Center.X - inward * (Width / 2f + 0.16f), track.Bottom + track.Height * RedLine), new Vector2(0.22f, 0.22f), HudTheme.Warning);

        // The readout follows the fill
        var readout = new Vector2(track.Center.X + inward * 1.1f, MathUtil.Clamp(track.Bottom + track.Height * level, track.Bottom + 0.35f, track.Top - 0.35f));

        canvas.Style(HudCanvas.Thin, 0.95f, theme.Ground);
        canvas.ChamferedPanel(readout, new Vector2(1.3f, 0.56f), 0.1f, level > RedLine ? HudTheme.Warning : colour);
        canvas.Text(new(name, "value"), $"{figure:0}", readout, HudText.Value, theme.Text);

        var tag = new Vector2(track.Center.X, track.Top + 0.5f);

        canvas.Style(HudCanvas.Thin, 0.6f, theme.Ground);
        canvas.ChamferedPanel(tag, new Vector2(1.3f, 0.5f), 0.1f, colour.WithAlpha(0.7f));
        canvas.Text(new(name, "caption"), caption, tag, HudText.Caption, colour);
    }
}