using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The shapes several widgets share, as extension methods on the canvas. Each one draws with the
/// style that is current, except where its summary says it sets one.
/// </summary>
public static class HudShapes
{
    /// <summary>The colour with its alpha multiplied.</summary>
    public static Color WithAlpha(this Color colour, float alpha) => new(colour.R, colour.G, colour.B, (byte)(colour.A * MathUtil.Clamp(alpha, 0f, 1f)));

    /// <summary>A line between two points, its width in pixels.</summary>
    public static void Line(this HudCanvas canvas, Vector2 from, Vector2 to, float width, Color colour)
        => canvas.Shapes.DrawPixelLine(new Vector3(from, 0f), new Vector3(to, 0f), width, colour);

    /// <summary>A rectangle with square corners.</summary>
    public static void Box(this HudCanvas canvas, Vector2 center, Vector2 size, Color colour)
        => canvas.Shapes.DrawRectangle(new Vector3(center, 0f), Vector3.UnitX, Vector3.UnitY, size, colour);

    /// <summary>A rectangle with its corners cut at 45 degrees, the HUD's panel shape, as one convex polygon.</summary>
    public static void ChamferedPanel(this HudCanvas canvas, Vector2 center, Vector2 size, float cut, Color colour)
    {
        var w = size.X / 2f;
        var h = size.Y / 2f;

        cut = MathF.Min(cut, MathF.Min(w, h));

        ReadOnlySpan<Vector2> corners =
        [
            new(-w + cut, -h), new(w - cut, -h), new(w, -h + cut), new(w, h - cut),
            new(w - cut, h), new(-w + cut, h), new(-w, h - cut), new(-w, -h + cut),
        ];

        canvas.Shapes.DrawSolidPolygon(corners, center, 0f, colour);
    }

    /// <summary>
    /// A dashed ring as one shape: a tick ring, a range ring, a dial's scale. Dash, gap and phase
    /// are pixels. Advance the phase and the ring turns. Sets the style.
    /// </summary>
    public static void DashedRing(this HudCanvas canvas, Vector2 center, float radius, float dash, float gap, Color colour, float phase = 0f)
    {
        canvas.Style(HudCanvas.Thin, 0f, dash: dash, gap: gap, phase: phase);
        canvas.Shapes.DrawArc(center, radius, 0f, MathF.Tau, colour);
    }

    /// <summary>A bar made of cells, lit up to the value. Sets the style.</summary>
    public static void SegmentedBar(this HudCanvas canvas, HudRect bounds, int cells, float value, Color colour, Color idle)
    {
        var pitch = bounds.Width / cells;
        var lit = (int)MathF.Round(MathUtil.Clamp(value, 0f, 1f) * cells);

        for (var i = 0; i < cells; i++)
        {
            var on = i < lit;
            var cell = on ? colour : idle;

            canvas.Style(0f, on ? 0.95f : 0.25f, cell);
            canvas.Box(new Vector2(bounds.Left + pitch * (i + 0.5f), bounds.Center.Y), new Vector2(pitch * 0.7f, bounds.Height), cell);
        }
    }

    /// <summary>A corner bracket: two arms meeting at a point, as one stroke with a round join.</summary>
    /// <param name="corner">Where the arms meet.</param>
    /// <param name="arms">The length of each arm, signed: the arms run from the corner against these.</param>
    public static void Bracket(this HudCanvas canvas, Vector2 corner, Vector2 arms, float width, Color colour)
    {
        ReadOnlySpan<Vector2> points = [corner - new Vector2(arms.X, 0f), corner, corner - new Vector2(0f, arms.Y)];

        canvas.Shapes.DrawPixelPolyline(points, width, colour);
    }

    /// <summary>
    /// An invisible disc the pointer can find, for things too small to hit: a radar contact, a
    /// corner of a diagram. Sets the style and the tag, and clears the tag.
    /// </summary>
    public static void HitDisc(this HudCanvas canvas, HudId id, Vector2 center, float radius)
    {
        canvas.Style(0f, 1f, opacity: 0f);
        canvas.Shapes.Tag = id;
        canvas.Shapes.DrawSolidCircle(center, radius, Color.White);
        canvas.Shapes.Tag = null;
    }
}