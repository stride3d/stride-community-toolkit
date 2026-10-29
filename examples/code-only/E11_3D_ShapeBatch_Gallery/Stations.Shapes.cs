using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using static E11_3D_ShapeBatch_Gallery.Palette;

namespace E11_3D_ShapeBatch_Gallery;

// The closed shapes, from a single disc to a HUD panel with text on it.
public static class ShapeStations
{
    /// <summary>
    /// Pulsing filled discs lying on the floor: the shape every game needs for an area of effect, a
    /// spawn point or a capture zone. A disc is one vertex plus a radius, so it is analytically
    /// round - no tessellation to give it away up close.
    /// </summary>
    public static void Disc(ShapeStation s)
    {
        var shapes = s.Shapes;

        for (var i = 0; i < 3; i++)
        {
            var pulse = 1.5f + MathF.Sin(s.Seconds * 1.6f + i * 0.9f) * 0.4f;

            shapes.DrawDisc(s.At(-3.6f + i * 3.6f, Lift, 0f), s.Up, pulse, Color.OrangeRed);
        }
    }

    /// <summary>
    /// Unfilled rings, which is the same shape with the fill turned off - a selection marker that
    /// does not tint what it encircles. One around the pillar's base, one standing up, and one the
    /// size of the pad to show the width holding at a large radius.
    /// </summary>
    public static void Ring(ShapeStation s)
    {
        var shapes = s.Shapes;
        var pillar = s.Pillars[0];

        shapes.DrawRing(pillar.Base + Vector3.UnitY * Lift, s.Up, 1.9f, Color.Cyan);
        shapes.DrawRing(s.At(2.5f, 1.8f, 0f), s.Forward, 1.4f, Color.Cyan);
        shapes.DrawRing(s.At(0f, Lift, 0f), s.Up, 4.8f, Color.DeepSkyBlue);
    }

    /// <summary>
    /// Flat art laid onto the ground. A hexagon landing pad and a rounded triangle - arbitrary
    /// convex polygons on an arbitrary plane, which is all a decal really is. Lying flat means the
    /// polygon's own X and Y axes map to the station's right and away.
    /// </summary>
    public static void Polygon(ShapeStation s)
    {
        var shapes = s.Shapes;

        ReadOnlySpan<Vector2> hexagon =
        [
            new(2.6f, 0f), new(1.3f, 2.25f), new(-1.3f, 2.25f),
            new(-2.6f, 0f), new(-1.3f, -2.25f), new(1.3f, -2.25f),
        ];

        ReadOnlySpan<Vector2> triangle = [new(0f, 1.6f), new(-1.5f, -1f), new(1.5f, -1f)];

        shapes.DrawSolidPolygon(hexagon, s.At(-1.5f, Lift, 0f), s.Right, -s.Forward, Color.MediumPurple);
        shapes.DrawSolidPolygon(triangle, s.At(3.4f, Lift, 0.8f), s.Right, -s.Forward, Color.Violet, radius: 0.3f);
    }

    /// <summary>
    /// Rectangles: four tiles rolling around a point on the ground, their axes turning with them so
    /// they roll rather than slide, and one standing upright facing the visitor. Rounded corners
    /// come free - the rounding radius is the same term that makes a capsule.
    /// </summary>
    public static void Rectangle(ShapeStation s)
    {
        var shapes = s.Shapes;

        for (var i = 0; i < 4; i++)
        {
            var angle = s.Seconds * 0.4f + i * MathF.PI * 0.5f;
            var (sin, cos) = MathF.SinCos(angle);
            var axisX = s.Direction(cos, 0f, -sin);
            var axisY = s.Direction(sin, 0f, cos);
            var centre = s.At(cos * 3.2f, Lift, -sin * 3.2f + 1f);

            shapes.DrawRectangle(centre, axisX, axisY, new Vector2(1.6f, 1.6f), Color.Violet, cornerRadius: 0.3f);
        }

        shapes.DrawRectangle(s.At(0f, 2.4f, -2f), s.Right, s.Up, new Vector2(4f, 2f), Color.Orchid, cornerRadius: 0.4f);
    }

    /// <summary>
    /// A sector keeps an angular range of a disc between two radial edges: a pie wedge, or with an
    /// inner radius a donut segment. A donut chart on the ground, a wedge turning beside it, and a
    /// field-of-view cone sweeping from the pillar's base. Angles are radians, counter-clockwise
    /// from the plane's X axis, negative for clockwise.
    /// </summary>
    public static void Sector(ShapeStation s)
    {
        var shapes = s.Shapes;

        // Four sectors sharing a centre, each filled in its own colour inside a neutral outline,
        // with a small gap between them
        ReadOnlySpan<(float Share, Color Fill)> segments =
        [
            (0.38f, Color.DodgerBlue), (0.27f, Color.Orange), (0.2f, Color.MediumSeaGreen), (0.15f, Color.Crimson),
        ];

        var chart = s.At(1.5f, Lift, 0.5f);
        var angle = MathF.PI * 0.5f;

        foreach (var (share, fill) in segments)
        {
            var sweep = share * MathF.Tau;

            shapes.Fill.Color = fill;
            shapes.DrawSector(chart, s.Up, 2.4f, angle + 0.03f, sweep - 0.06f, Color.White, innerRadius: 1.2f);

            angle += sweep;
        }

        shapes.Fill.Color = null;

        shapes.DrawSector(s.At(4.6f, Lift, -1f), s.Up, 1.5f, s.Seconds * 0.8f, MathF.PI * 0.6f, Color.Gold);

        // A cone that starts at the centre: the watcher's field of view, sweeping
        var watcher = s.Pillars[0].Base + Vector3.UnitY * Lift;
        var facing = MathF.Sin(s.Seconds * 0.5f) * 0.9f - MathF.PI * 0.5f;

        shapes.DrawSector(watcher, s.Up, 5f, facing - 0.45f, 0.9f, Color.Yellow);
    }

    /// <summary>
    /// An annulus is a ring with real width and an outline on both edges; an arc keeps a range of a
    /// ring with round ends - a progress bar bent into a circle. Three radial progress dials stand
    /// facing the visitor, a faint full track behind a bright arc filling clockwise from twelve;
    /// on the ground, a stroke arc with a gap travelling around it.
    /// </summary>
    public static void AnnulusAndArc(ShapeStation s)
    {
        var shapes = s.Shapes;

        shapes.DrawAnnulus(s.At(-3f, Lift, 0f), s.Up, 2.2f, 1.4f, Color.Turquoise);

        for (var i = 0; i < 3; i++)
        {
            var centre = s.At(0.5f + i * 2.6f, 2.4f, -1f);
            var progress = (MathF.Sin(s.Seconds * 0.7f + i * 1.1f) + 1f) * 0.5f;

            shapes.Fill.Alpha = 0.25f;
            shapes.DrawArc(centre, s.Forward, 1f, 0f, MathF.Tau, Color.Gray, width: 0.3f);

            shapes.Fill.Alpha = 0.9f;
            shapes.DrawArc(centre, s.Forward, 1f, MathF.PI * 0.5f, -progress * MathF.Tau, Color.LimeGreen, width: 0.3f);
        }

        shapes.Fill.Alpha = s.Style.FillAlpha;

        shapes.DrawArc(s.At(2f, Lift, 1.5f), s.Up, 3f, s.Seconds, MathF.Tau * 0.8f, Color.HotPink);
    }

    /// <summary>
    /// Camera-facing markers. A billboard keeps its shape and its screen orientation from every
    /// angle, which is what a waypoint or a unit marker wants - fly around and they never
    /// foreshorten. Beside each, pixel-measured markers that are the same size on screen at any
    /// distance, where the world-radius billboards shrink with it, joined by a glowing pixel line.
    /// </summary>
    public static void Billboard(ShapeStation s)
    {
        var shapes = s.Shapes;
        var bob = MathF.Sin(s.Seconds * 2f) * 0.25f;

        // A coloured fill inside a neutral outline: readable against any background
        shapes.Fill.Color = Color.LimeGreen;

        foreach (var pillar in s.Pillars)
        {
            shapes.DrawBillboardCircle(pillar.Top + Vector3.UnitY * (1.6f + bob), 0.45f, Color.White);
        }

        shapes.Fill.Color = null;
        shapes.BorderWidth = 2f;
        shapes.Glow.Set(8f, new Color(255, 140, 0, 160));

        foreach (var pillar in s.Pillars)
        {
            var beside = pillar.Top + Vector3.UnitY * (1.6f + bob);
            var left = beside - s.Right * 1.4f;
            var right = beside + s.Right * 1.4f;

            shapes.DrawPixelDisc(left, 6f, Color.Orange);
            shapes.DrawPixelRing(right, 10f, Color.Orange);
            shapes.DrawPixelLine(left, right, 1.5f, new Color(255, 140, 0, 120));
        }

        shapes.Glow.Clear();
        shapes.BorderWidth = s.Style.BorderWidth;

        // Any polygon can be billboarded, not just circles
        ReadOnlySpan<Vector2> diamond = [new(0.9f, 0f), new(0f, 0.9f), new(-0.9f, 0f), new(0f, -0.9f)];

        shapes.DrawBillboard(diamond, s.At(0f, 6f + bob, 0f), Color.GreenYellow);
    }

    /// <summary>
    /// The headline: identical rings marching away from the visitor. They shrink, their outlines do
    /// not. Geometry-based outlines cannot do this - a ring of triangles thins to nothing with
    /// distance. Fly down the corridor.
    /// </summary>
    public static void DistanceProof(ShapeStation s)
    {
        for (var i = 0; i < 10; i++)
        {
            s.Shapes.DrawRing(s.At(0f, 2.4f, -6f - i * 6.5f), s.Forward, 2f, Color.HotPink);
        }
    }

    /// <summary>
    /// The text on the panel is a WorldTextComponent placed once: with billboarding off it draws in
    /// its entity's own XY plane, so an entity rotated to face the visitor lays the text flat onto
    /// the panel. The shape is still drawn every frame; the text is updated by its property.
    /// </summary>
    public static void HudPanelSetup(ShapeStation s)
    {
        var text = new WorldTextComponent
        {
            Text = "",
            FontSize = 48,
            Height = 1.3f,
            TextColor = HudBlue,
            GlowColor = new Color(0, 140, 255, 170),
            GlowSize = 4f,
            Alignment = TextAlignment.Center,
            Billboard = false,
        };

        // A hair in front of the panel, so the text is unambiguously the nearer surface
        var entity = new Entity($"Station {s.Number} text")
        {
            Transform = { Position = s.At(0f, 3f, -1f) + s.Forward * 0.01f, Rotation = s.FacingRotation() },
        };

        entity.Add(text);
        entity.Scene = s.Scene;
        s.State = text;
    }

    /// <summary>
    /// A rectangle standing in the world, styled as a ship's HUD: a half-opaque dark fill, a thin
    /// light edge and a glow outside it, corner brackets in pixel lines, and the world text on it
    /// counting up. Fill and outline are independent colours - a faint dark panel with a light edge
    /// is something deriving the fill from the outline could never produce.
    /// </summary>
    public static void HudPanel(ShapeStation s)
    {
        var shapes = s.Shapes;
        var centre = s.At(0f, 3f, -1f);

        shapes.Fill.Set(HudFill, 0.55f);
        shapes.BorderWidth = 1.5f;
        shapes.Glow.Set(7f, HudGlow);
        shapes.DrawRectangle(centre, s.Right, s.Up, new Vector2(6f, 3.6f), HudBlue, cornerRadius: 0.35f);
        shapes.Glow.Clear();

        var topLeft = centre - s.Right * 2.7f + s.Up * 1.5f;
        var bottomRight = centre + s.Right * 2.7f - s.Up * 1.5f;

        shapes.DrawPixelLine(topLeft, topLeft + s.Right * 0.8f, 1.5f, HudBlue);
        shapes.DrawPixelLine(topLeft, topLeft - s.Up * 0.5f, 1.5f, HudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight - s.Right * 0.8f, 1.5f, HudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight + s.Up * 0.5f, 1.5f, HudBlue);

        s.ResetStyle(shapes);

        if (s.State is WorldTextComponent text)
        {
            // Thousands separators keep the digits moving
            text.Text = $"SHAPE GALLERY\n{(long)(s.Seconds * 137.5f):N0}";
        }
    }

    /// <summary>
    /// Builds the two labels the overflow station compares. Both carry the same words; only the
    /// string differs, because wrapping is the caller's job.
    /// </summary>
    public static void TextOverflowSetup(ShapeStation s)
    {
        var spilling = Label(s, Overflowing, 0.5f, new Vector3(-2.4f, 2.6f, -1f));
        var wrapped = Label(s, Wrapped, 1.25f, new Vector3(2.4f, 2.6f, -1f));

        s.State = new[] { spilling, wrapped };

        static WorldTextComponent Label(ShapeStation s, string text, float height, Vector3 local)
        {
            var label = new WorldTextComponent
            {
                Text = text,
                FontSize = 48,
                Height = height,
                TextColor = HudBlue,
                GlowColor = new Color(0, 140, 255, 170),
                GlowSize = 4f,
                Alignment = TextAlignment.Center,
                Billboard = false,
            };

            var entity = new Entity($"Station {s.Number} {(text == Overflowing ? "spilling" : "wrapped")}")
            {
                Transform = { Position = s.At(local) + s.Forward * 0.01f, Rotation = s.FacingRotation() },
            };

            entity.Add(label);
            entity.Scene = s.Scene;

            return label;
        }
    }

    /// <summary>
    /// A panel is a shape and the text on it is a separate renderer, so a shape never clips text:
    /// the left label runs straight out past its border, and no property on either will stop it.
    /// The right one carries the same words wrapped into three lines, with the block's height set
    /// to match, which is the whole of the fix - the caller decides where the lines break, because
    /// only the caller knows what the words mean. Both panels are the same size.
    /// </summary>
    public static void TextOverflow(ShapeStation s)
    {
        var shapes = s.Shapes;

        foreach (var spills in (ReadOnlySpan<bool>)[true, false])
        {
            var centre = s.At(spills ? -2.4f : 2.4f, 2.6f, -1f);

            shapes.Fill.Set(HudFill, 0.55f);
            shapes.BorderWidth = 1.5f;
            shapes.Glow.Set(6f, spills ? new Color(255, 90, 60, 150) : HudGlow);
            shapes.DrawRectangle(centre, s.Right, s.Up, new Vector2(4f, 2.2f), spills ? new Color(255, 140, 110) : HudBlue, cornerRadius: 0.3f);
            shapes.Glow.Clear();
        }

        s.ResetStyle(shapes);
    }

    /// <summary>The words the overflow station spills, on one line.</summary>
    private const string Overflowing = "Nothing clips a long line";

    /// <summary>The same words, broken where they read best - which no renderer can guess.</summary>
    private const string Wrapped = "Nothing\nclips a\nlong line";
}