using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// What is known about the target: a turning marker, its callsign and type, range, bearing and
/// closing speed, and its hull and shield. It follows whichever contact is selected.
/// </summary>
public sealed class TargetWidget(ShipState ship) : HudWidget
{
    private const string Name = "target";
    private const float MarkerColumn = 2.5f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var target = ship.Target;
        var content = canvas.Panel(bounds, Name, "TARGET", target.Hostile ? "HOSTILE" : "FRIENDLY");
        var colour = target.Hostile ? HudTheme.Warning : theme.For(HudRole.Commanded);

        DrawMarker(canvas, content.TakeLeft(MarkerColumn), target, colour);

        var facts = content.DropLeft(MarkerColumn + 0.2f);

        canvas.Text(new(Name, "callsign"), target.Callsign, new Vector2(facts.Left, facts.Top - 0.22f), HudText.Value.Left, theme.Text);
        canvas.Text(new(Name, "type"), target.Type.ToUpperInvariant(), new Vector2(facts.Right, facts.Top - 0.22f), HudText.Caption.Right, colour);

        var rows = facts.DropTop(0.55f);

        DrawFact(canvas, rows.Row(0, 5), 0, "RNG", $"{target.Range:0.00} KM");
        DrawFact(canvas, rows.Row(1, 5), 1, "BRG", $"{target.Bearing:000}");
        DrawFact(canvas, rows.Row(2, 5), 2, "CLS", $"{target.Closing:+0;-0} M/S");
        DrawBar(canvas, rows.Row(3, 5), 3, "HULL", target.Hull, colour);
        DrawBar(canvas, rows.Row(4, 5), 4, "SHLD", target.Shield, colour);
    }

    /// <summary>A diamond for a hostile and a ring for anything else, inside a dashed ring that turns.</summary>
    private static void DrawMarker(HudCanvas canvas, HudRect bounds, Contact target, Color colour)
    {
        var center = bounds.Center;
        var radius = MathF.Min(bounds.Width, bounds.Height) / 2f - 0.25f;

        canvas.DashedRing(center, radius, 8f, 6f, colour.WithAlpha(0.6f), canvas.Time * 14f);

        canvas.Style(HudCanvas.Thin, 0.2f, colour, glow: 4f, glowColour: colour, additive: true);

        if (target.Hostile)
        {
            var half = radius * 0.55f;

            ReadOnlySpan<Vector2> diamond = [new(0f, -half), new(half, 0f), new(0f, half), new(-half, 0f)];

            canvas.Shapes.DrawSolidPolygon(diamond, center, canvas.Time * 0.8f, colour);
        }
        else
        {
            canvas.Shapes.DrawSolidCircle(center, radius * 0.45f, colour);
        }

        canvas.Style(HudCanvas.Thin, 0f);

        foreach (var side in (ReadOnlySpan<Vector2>)[new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f)])
        {
            canvas.Bracket(center + side * (radius + 0.12f), side * 0.3f, HudCanvas.Thin, colour);
        }
    }

    private static void DrawFact(HudCanvas canvas, HudRect row, int index, string caption, string value)
    {
        var theme = canvas.Theme;

        canvas.Text(new(Name, "caption", index), caption, new Vector2(row.Left, row.Center.Y), HudText.Caption.Left, theme.For(HudRole.Frame));
        canvas.Text(new(Name, "value", index), value, new Vector2(row.Right, row.Center.Y), HudText.Small.Right, theme.Text);
    }

    private static void DrawBar(HudCanvas canvas, HudRect row, int index, string caption, float value, Color colour)
    {
        var theme = canvas.Theme;

        canvas.Text(new(Name, "caption", index), caption, new Vector2(row.Left, row.Center.Y), HudText.Caption.Left, theme.For(HudRole.Frame));
        canvas.SegmentedBar(new HudRect(row.Left + 1.1f, row.Center.Y - 0.12f, row.Width - 1.1f, 0.24f), 16, value, colour, theme.Dim(HudRole.Frame));
    }
}