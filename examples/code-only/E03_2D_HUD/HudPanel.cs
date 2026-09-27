using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>The pattern of a panel's glass. The values are what the shader reads.</summary>
public enum GlassPattern
{
    /// <summary>A dark scanline every four pixels.</summary>
    Lines,

    /// <summary>Five-pixel squares, each at one of five opacities.</summary>
    Squares,
}

/// <summary>
/// The frame every panel shares: a chamfered outline over the ground colour, the glass shader over
/// that, and an optional header with a title. The frame brightens while the pointer is over the
/// panel or anything inside it.
/// </summary>
public static class HudPanel
{
    /// <summary>The height of the header strip.</summary>
    public const float Header = 0.6f;

    /// <summary>How much is cut off each corner.</summary>
    public const float Cut = 0.22f;

    /// <summary>The space between the frame and what is inside it.</summary>
    public const float Margin = 0.3f;

    /// <summary>Draws a panel's frame and header.</summary>
    /// <param name="canvas">The canvas to draw on.</param>
    /// <param name="bounds">The whole panel.</param>
    /// <param name="name">The widget's name: the panel's tag for the pointer.</param>
    /// <param name="title">The title in the header, or <c>null</c> for a panel without a header.</param>
    /// <param name="detail">A short figure at the right of the header, such as a range.</param>
    /// <returns>The rectangle inside the frame and under the header: where the content goes.</returns>
    public static HudRect Panel(this HudCanvas canvas, HudRect bounds, string name, string? title = null, string? detail = null)
    {
        var theme = canvas.Theme;
        var accent = theme.For(HudRole.Frame);
        var hover = canvas.Animate(new(name, "hover"), canvas.HoversPanel(name) ? 1f : 0f);

        // The body, tagged so the pointer finds the panel
        canvas.Style(HudCanvas.Thin + 0.6f * hover, 0.62f, theme.Ground, glow: 3f * hover, glowColour: theme.Glow, additive: true);
        canvas.Shapes.Tag = new HudId(name);
        canvas.ChamferedPanel(bounds.Center, bounds.Size, Cut, accent.WithAlpha(0.55f + 0.45f * hover));
        canvas.Shapes.Tag = null;

        // The glass over it: the batch's fill source, tinted by the accent. Squares cover less of
        // the panel than lines, so they are drawn stronger.
        canvas.Style(0f, canvas.Glass == GlassPattern.Squares ? 0.3f : 0.16f, accent, textured: true);
        canvas.ChamferedPanel(bounds.Center, bounds.Size - new Vector2(0.08f), Cut, accent);

        if (title is null) return bounds.Inset(Margin);

        var header = bounds.TakeTop(Header).Inset(Margin, 0f);
        var middle = header.Center.Y - 0.04f;

        // A tab, the title, a rule under them and three ticks at the far end
        canvas.Style(0f, 1f, accent);
        canvas.Box(new Vector2(header.Left + 0.05f, middle), new Vector2(0.1f, 0.3f), accent);
        canvas.Text(new(name, "title"), title, new Vector2(header.Left + 0.28f, middle), HudText.Title, accent);

        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Line(new Vector2(header.Left, header.Bottom), new Vector2(header.Right, header.Bottom), HudCanvas.Thin, theme.Dim(HudRole.Frame));

        if (detail is not null)
        {
            canvas.Text(new(name, "detail"), detail, new Vector2(header.Right, middle), HudText.Small.Right, theme.Text.WithAlpha(0.7f));
        }
        else
        {
            for (var i = 0; i < 3; i++)
            {
                canvas.Style(0f, 0.6f, accent);
                canvas.Box(new Vector2(header.Right - 0.08f - i * 0.2f, middle), new Vector2(0.1f, 0.1f), accent);
            }
        }

        return bounds.DropTop(Header).Inset(Margin, Margin * 0.6f);
    }
}