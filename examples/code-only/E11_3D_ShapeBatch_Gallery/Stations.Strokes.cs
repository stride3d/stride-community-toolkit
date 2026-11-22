using Stride.Core.Mathematics;
using static E11_3D_ShapeBatch_Gallery.Palette;

namespace E11_3D_ShapeBatch_Gallery;

// The strokes: lines, boxes, runs of points in a plane and through space.
public static class StrokeStations
{
    /// <summary>
    /// Thick 3D lines. Hardware line rendering clamps to one pixel on most drivers; these are
    /// capsules swung about their own axis to face the camera, so the width is real and holds up
    /// close. Golden lines from a hub to the pillar tops taper with distance; the pixel-width rails
    /// running away beside them are the same thickness near and far.
    /// </summary>
    public static void Line(ShapeStation s)
    {
        var shapes = s.Shapes;
        var hub = s.At(0f, 7f, 0f);
        var width = 0.14f + MathF.Sin(s.Seconds * 2f) * 0.05f;

        foreach (var pillar in s.Pillars)
        {
            shapes.DrawLine(hub, pillar.Top, width, Color.Gold);
        }

        shapes.DrawLine(hub, s.At(0f, Lift, 3f), width, Color.Gold);

        shapes.DrawPixelLine(s.At(-4.5f, 0.3f, -5f), s.At(-4.5f, 0.3f, -70f), 2f, Color.White);
        shapes.DrawPixelLine(s.At(4.5f, 0.3f, -5f), s.At(4.5f, 0.3f, -70f), 2f, Color.White);
    }

    /// <summary>
    /// A wire box is its twelve edges drawn as twelve thick lines: a selection volume around the
    /// pillar, and a flat one on the ground beside it. The box is axis-aligned to the world.
    /// </summary>
    public static void WireBox(ShapeStation s)
    {
        var shapes = s.Shapes;
        var pillar = s.Pillars[0];

        shapes.DrawWireBox(pillar.Centre, new Vector3(2.6f, pillar.Height + 0.8f, 2.6f), 0.08f, Color.Yellow);
        shapes.DrawWireBox(s.At(2.8f, 0.8f, 0.5f), new Vector3(3.2f, 1.6f, 3.2f), 0.06f, Color.Khaki);
    }

    /// <summary>
    /// A run of points as one stroke with round joins. A sine wave at two pixels; the same run at
    /// six pixels and half opacity, one shape, so its joins do not double up; a closed HUD bracket -
    /// concave, which no polygon fill could be - with dashes marching around it; and the same
    /// bracket flat on the ground at a world width, where the joins show their roundness.
    /// </summary>
    public static void Polyline(ShapeStation s)
    {
        var shapes = s.Shapes;

        Span<Vector2> wave = stackalloc Vector2[48];

        for (var i = 0; i < wave.Length; i++)
        {
            var x = -4.5f + 9f * i / (wave.Length - 1);

            wave[i] = new Vector2(x, MathF.Sin(x * 1.4f + s.Seconds) * 0.6f);
        }

        var back = s.At(0f, 1.5f, -3f);

        shapes.DrawPixelPolyline(wave, back, s.Right, s.Up, 2f, new Color(24, 28, 40));

        shapes.Opacity = 0.5f;
        shapes.DrawPixelPolyline(wave, back + s.Up * 1.8f, s.Right, s.Up, 6f, Color.Orange);
        shapes.Opacity = 1f;

        ReadOnlySpan<Vector2> bracket = [new(-2f, 0f), new(-2f, 1.5f), new(-1.25f, 2.25f), new(1.25f, 2.25f), new(2f, 1.5f), new(2f, 0f), new(1f, 0f), new(1f, 0.75f), new(-1f, 0.75f), new(-1f, 0f)];

        shapes.Dash.Set(8f, 5f, s.Seconds * 25f);
        shapes.DrawPixelPolyline(bracket, back + s.Up * 3.4f, s.Right, s.Up, 2f, Color.Cyan, closed: true);
        shapes.Dash.Clear();

        shapes.DrawPolyline(bracket, s.At(0f, Lift, 3.5f), s.Right, -s.Forward, 0.18f, Color.Gold, closed: true);
    }

    /// <summary>
    /// Space strokes: a run of 3D points stroked on screen, with no plane and no geometry. A helix
    /// of pixel width with a glow climbing the pillar - it hides behind the pillar where it passes
    /// behind it - and a closed trefoil of world width hanging beside it, thin where it is far and
    /// thick where it is near.
    /// </summary>
    public static void SpaceStroke(ShapeStation s)
    {
        var shapes = s.Shapes;
        var pillar = s.Pillars[0];

        Span<Vector3> helix = stackalloc Vector3[64];

        for (var i = 0; i < helix.Length; i++)
        {
            var t = (float)i / (helix.Length - 1);
            var angle = t * MathUtil.TwoPi * 3f + s.Seconds;

            helix[i] = pillar.Base + new Vector3(MathF.Cos(angle) * 1.8f, 0.6f + t * (pillar.Height + 0.4f), MathF.Sin(angle) * 1.8f);
        }

        shapes.Glow.Set(8f, new Color(255, 120, 40, 140));
        shapes.DrawPixelPolyline(helix, 3f, Color.Orange);
        shapes.Glow.Clear();

        Span<Vector3> trefoil = stackalloc Vector3[96];

        for (var i = 0; i < trefoil.Length; i++)
        {
            var angle = i * MathUtil.TwoPi / trefoil.Length;
            var x = (MathF.Sin(angle) + 2f * MathF.Sin(2f * angle)) * 0.9f;
            var y = MathF.Sin(3f * angle) * 0.8f;
            var z = (MathF.Cos(angle) - 2f * MathF.Cos(2f * angle)) * 0.9f;

            trefoil[i] = s.At(3f + x, 4.5f + y, -1f + z);
        }

        shapes.DrawPolyline(trefoil, 0.1f, Color.DeepSkyBlue, closed: true);
    }
}