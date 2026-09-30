using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The scope: range rings, a sweep that leaves a fading wedge behind it, and the contacts. Click a
/// contact to make it the target.
/// </summary>
public sealed class RadarWidget(ShipState ship) : HudWidget
{
    private const string Name = "radar";
    private const float Trail = 1.1f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name, "RADAR", $"{ShipState.RadarRange:0} KM");
        var colour = theme.For(HudRole.Navigation);
        var dim = theme.Dim(HudRole.Navigation);
        var glow = theme.GlowFor(HudRole.Navigation);
        var center = content.Center;
        var radius = MathF.Min(content.Width, content.Height) / 2f - 0.3f;

        // Ground disc
        canvas.Style(HudCanvas.Thin, 0.5f, theme.Ground);
        canvas.Shapes.DrawSolidCircle(center, radius, colour);

        // Range rings, one dashed shape each, and the cross
        canvas.DashedRing(center, radius * 0.66f, 7f, 6f, dim);
        canvas.DashedRing(center, radius * 0.33f, 7f, 6f, dim);

        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Line(center - new Vector2(radius, 0f), center + new Vector2(radius, 0f), HudCanvas.Thin, dim);
        canvas.Line(center - new Vector2(0f, radius), center + new Vector2(0f, radius), HudCanvas.Thin, dim);

        // The sweep: a wedge behind the leading edge, bright at the edge and fading across its
        // width. The gradient runs along the wedge's tangent, so it fades with the angle.
        var sweep = ship.RadarSweep;
        var middle = sweep + Trail / 2f;

        canvas.Style(new HudStyle(0f, 1f, colour.WithAlpha(0.45f)) { GradientTo = colour.WithAlpha(0f), GradientAlong = new Vector2(-MathF.Sin(middle), MathF.Cos(middle)) });
        canvas.Shapes.DrawSector(center, radius - 0.03f, sweep, Trail, colour);

        canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 1.5f, GlowColour = glow, Additive = true });
        canvas.Line(center, center + Direction(sweep) * radius, HudCanvas.Thick, colour);

        for (var i = 0; i < ship.Contacts.Length; i++)
        {
            DrawContact(canvas, i, center + Direction(ship.Contacts[i].Angle) * ship.Contacts[i].Distance * radius);
        }

        // Bezel, and a tick ring outside it that turns slowly: its phase is the whole animation
        canvas.Style(new HudStyle(HudCanvas.Thick, 0f) { Glow = 4f, GlowColour = glow });
        canvas.Shapes.DrawArc(center, radius, 0f, MathF.Tau, colour);
        canvas.DashedRing(center, radius + 0.22f, 4f, 10f, dim, canvas.Time * 12f);
    }

    private void DrawContact(HudCanvas canvas, int index, Vector2 position)
    {
        var theme = canvas.Theme;
        var contact = ship.Contacts[index];
        var id = new HudId(Name, Index: index);
        var colour = contact.Hostile ? HudTheme.Warning : theme.Text;
        var hover = canvas.Animate(new(Name, "hover", index), canvas.Hovers(id) ? 1f : 0f);

        if (canvas.Clicks(id)) ship.TargetIndex = index;

        canvas.Style(0f, 1f, colour);
        canvas.Shapes.DrawSolidCircle(position, 0.07f + 0.04f * hover, colour);

        // A hostile pulses its light out over the scope
        if (contact.Hostile)
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(canvas.Time * 5f);

            canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 4f + 6f * pulse, GlowColour = colour, Additive = true });
            canvas.Shapes.DrawArc(position, 0.18f, 0f, MathF.Tau, colour);
        }

        // The target wears a bracket
        if (index == ship.TargetIndex)
        {
            var commanded = theme.For(HudRole.Commanded);

            canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 3f, GlowColour = commanded, Additive = true });

            foreach (var side in (ReadOnlySpan<Vector2>)[new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f)])
            {
                canvas.Bracket(position + side * 0.3f, side * 0.14f, HudCanvas.Thin, commanded);
            }
        }

        canvas.HitDisc(id, position, 0.3f);
    }

    private static Vector2 Direction(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
}