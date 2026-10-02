using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The open glass in the middle: the gun-sight, the pitch ladder sliding behind it and the bracket
/// that follows the target. It has no frame. The sight is projected light, so its glow is additive.
/// </summary>
public sealed class SightWidget(ShipState ship) : HudWidget
{
    private const string Name = "sight";
    private const float UnitsPerDegree = 0.115f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        DrawPitchLadder(canvas, bounds);
        DrawReticle(canvas, bounds.Center);
        DrawTargetBox(canvas, bounds);
    }

    /// <summary>Four arcs of one ring with gaps at the cardinal points, four ticks and a dot.</summary>
    private static void DrawReticle(HudCanvas canvas, Vector2 center)
    {
        var theme = canvas.Theme;
        var glow = theme.GlowFor(HudRole.Navigation);

        canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 3f, GlowColour = glow, Additive = true });

        // Four arcs read as a sight; a full ring reads as a target
        for (var i = 0; i < 4; i++)
        {
            canvas.Shapes.DrawArc(center, 0.42f, i * MathF.PI / 2f + 0.25f, MathF.PI / 2f - 0.5f, theme.Text);
        }

        canvas.Style(new HudStyle(0f, 1f, theme.Text) { Glow = 2.5f, GlowColour = glow, Additive = true });
        canvas.Shapes.DrawSolidCircle(center, 0.035f, theme.Text);

        canvas.Style(HudCanvas.Thin, 0f);

        foreach (var direction in (ReadOnlySpan<Vector2>)[Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY])
        {
            canvas.Line(center + direction * 0.5f, center + direction * 0.75f, HudCanvas.Thin, theme.Text);
        }
    }

    /// <summary>
    /// A line every ten degrees, sliding with the ship's pitch. Lines above the horizon are solid
    /// and lines below it dashed, the convention aircraft HUDs share.
    /// </summary>
    private void DrawPitchLadder(HudCanvas canvas, HudRect bounds)
    {
        const float Arm = 1.4f;
        const float Gap = 0.9f;

        var colour = canvas.Theme.For(HudRole.Navigation);
        var center = bounds.Center;
        var window = bounds.Height / 2f - 0.25f;

        for (var pitch = -30; pitch <= 30; pitch += 10)
        {
            var offset = (pitch - ship.Flight.Pitch) * UnitsPerDegree;

            if (pitch == 0 || MathF.Abs(offset) > window || MathF.Abs(offset) < 0.6f) continue;

            var y = center.Y + offset;
            var text = $"{pitch:+0;-0}";

            // Fades out towards the edge of the window rather than popping
            var fade = MathUtil.Clamp((window - MathF.Abs(offset)) / 0.5f, 0f, 1f);

            canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Dash = pitch > 0 ? 0f : 9f, Gap = 6f, Opacity = fade });
            canvas.Line(new Vector2(center.X - Arm - Gap, y), new Vector2(center.X - Gap, y), HudCanvas.Thin, colour);
            canvas.Line(new Vector2(center.X + Gap, y), new Vector2(center.X + Arm + Gap, y), HudCanvas.Thin, colour);

            canvas.Text(new(Name, "pitch-left", pitch), text, new Vector2(center.X - Arm - Gap - 0.5f, y), HudText.Small, colour.WithAlpha(fade));
            canvas.Text(new(Name, "pitch-right", pitch), text, new Vector2(center.X + Arm + Gap + 0.5f, y), HudText.Small, colour.WithAlpha(fade));
        }
    }

    /// <summary>Four corners breathing on a sine, with the target's callsign and range under them.</summary>
    private void DrawTargetBox(HudCanvas canvas, HudRect bounds)
    {
        const float Arm = 0.22f;

        var target = ship.Target;
        var colour = target.Hostile ? HudTheme.Warning : canvas.Theme.For(HudRole.Commanded);
        var center = bounds.Center + ship.TargetInSight * (bounds.Size / 2f - new Vector2(1.2f, 0.9f));
        var half = 0.5f + 0.05f * MathF.Sin(canvas.Time * 4f);

        canvas.Style(new HudStyle(HudCanvas.Thin, 0f) { Glow = 3f, GlowColour = colour, Additive = true });

        foreach (var side in (ReadOnlySpan<Vector2>)[new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f)])
        {
            canvas.Bracket(center + side * half, side * Arm, HudCanvas.Thick, colour);
        }

        canvas.Text(new(Name, "target"), $"{target.Callsign}  {target.Range:0.0} KM", center - new Vector2(0f, half + 0.3f), HudText.Small, colour);
    }
}