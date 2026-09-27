using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// How the reactor's output is shared: a triangle with a system at each corner and a dot that sits
/// nearer the systems that get more. Click a corner to focus that system, or the middle to balance.
/// </summary>
public sealed class PowerWidget(ShipState ship) : HudWidget
{
    private const string Name = "power";
    private const float DiagramColumn = 3.5f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var power = ship.Power;
        var focus = power.Focus == PowerState.Balanced ? "BALANCED" : $"{PowerState.Names[power.Focus]} FOCUS";
        var content = canvas.Panel(bounds, Name, "POWER", focus);

        DrawDiagram(canvas, content.TakeLeft(DiagramColumn));

        var rows = content.DropLeft(DiagramColumn + 0.3f).Inset(0f, 0.35f);

        for (var i = 0; i < power.Shares.Length; i++)
        {
            DrawShare(canvas, i, rows.Row(i, power.Shares.Length));
        }
    }

    private void DrawDiagram(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var power = ship.Power;
        var frame = theme.For(HudRole.Frame);
        var commanded = theme.For(HudRole.Commanded);

        // An equilateral triangle, as tall as the room under the top label allows
        var height = bounds.Height - 0.75f;
        var half = height / MathF.Sqrt(3f);
        var bottom = bounds.Bottom + 0.4f;

        ReadOnlySpan<Vector2> corners =
        [
            new(bounds.Center.X, bottom + height),
            new(bounds.Center.X - half, bottom),
            new(bounds.Center.X + half, bottom),
        ];

        var middle = (corners[0] + corners[1] + corners[2]) / 3f;

        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Shapes.DrawPixelPolyline(corners, HudCanvas.Thin, frame.WithAlpha(0.8f), closed: true);

        var dot = Vector2.Zero;

        for (var i = 0; i < corners.Length; i++)
        {
            var id = new HudId(Name, Index: i);
            var focused = canvas.Animate(new(Name, "focused", i), power.Focus == i ? 1f : 0f);
            var hover = canvas.Animate(new(Name, "hover", i), canvas.Hovers(id) ? 1f : 0f);
            var colour = Color.Lerp(frame, commanded, MathF.Max(focused, hover));

            if (canvas.Clicks(id)) power.Focus = i;

            canvas.Style(HudCanvas.Thin, 0f);
            canvas.Line(middle, corners[i], HudCanvas.Thin, theme.Dim(HudRole.Frame));

            canvas.Style(HudCanvas.Thin, 0.3f + 0.6f * focused, colour, 5f * focused, commanded, additive: true);
            canvas.Shapes.DrawSolidCircle(corners[i], 0.13f + 0.04f * hover, colour);

            // The top label sits above its corner, the other two under theirs
            var label = corners[i] + new Vector2(0f, i == 0 ? 0.35f : -0.35f);

            canvas.Text(new(Name, "corner", i), PowerState.Names[i], label, HudText.Caption, colour);
            canvas.HitDisc(id, corners[i], 0.45f);

            dot += corners[i] * power.Shares[i];
        }

        var balance = new HudId(Name, "balance");

        if (canvas.Clicks(balance)) power.Focus = PowerState.Balanced;

        canvas.HitDisc(balance, middle, 0.35f);

        // The setting itself
        canvas.Style(0f, 1f, commanded, 6f, commanded, additive: true);
        canvas.Shapes.DrawSolidCircle(dot, 0.1f, commanded);
    }

    private void DrawShare(HudCanvas canvas, int index, HudRect row)
    {
        var theme = canvas.Theme;
        var share = ship.Power.Shares[index];
        var colour = ship.Power.Focus == index ? theme.For(HudRole.Commanded) : theme.For(HudRole.Engaged);

        canvas.Text(new(Name, "name", index), PowerState.Names[index], new Vector2(row.Left, row.Center.Y), HudText.Caption.Left, theme.For(HudRole.Frame));
        canvas.SegmentedBar(new HudRect(row.Left + 0.9f, row.Center.Y - 0.13f, row.Width - 1.9f, 0.26f), 10, share / 0.6f, colour, theme.Dim(HudRole.Frame));
        canvas.Text(new(Name, "share", index), $"{share * 100f:0}%", new Vector2(row.Right, row.Center.Y), HudText.Small.Right, theme.Text);
    }
}