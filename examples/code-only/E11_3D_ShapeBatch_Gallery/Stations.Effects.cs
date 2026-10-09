using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using static E11_3D_ShapeBatch_Gallery.Palette;

namespace E11_3D_ShapeBatch_Gallery;

// The per-draw states a shape can wear, and the batches a shape can go through.
public static class EffectStations
{
    /// <summary>
    /// Three ways to fill a disc. The first derives its fill from the outline colour, dimmed by the
    /// fill alpha, the Box2D testbed way; the second has a fill colour of its own inside a white
    /// outline; the third has no border at all, a fill and nothing else.
    /// </summary>
    public static void FillColour(ShapeStation s)
    {
        var shapes = s.Shapes;

        shapes.Fill.Set(null, 0.45f);
        shapes.DrawDisc(s.At(-3.6f, Lift, 0f), s.Up, 1.5f, Color.Orange);

        shapes.Fill.Set(new Color(20, 40, 110), 1f);
        shapes.DrawDisc(s.At(0f, Lift, 0f), s.Up, 1.5f, Color.White);

        shapes.BorderWidth = 0f;
        shapes.Fill.Set(Color.Crimson, 1f);
        shapes.DrawDisc(s.At(3.6f, Lift, 0f), s.Up, 1.5f, Color.White);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// The glow lives outside the outline and fades over a pixel width, so it neither tints the
    /// fill nor changes with distance. Its best use is contrast: a white cursor ring on a dark halo
    /// stays readable over anything. Wide, in the shape's own colour at a third of its strength and
    /// adding light, it is neon - standing here as a ring, a disc whose glow stops at its edge, and
    /// an arc. Press G to glow every station.
    /// </summary>
    public static void Glow(ShapeStation s)
    {
        var shapes = s.Shapes;
        var (sin, cos) = MathF.SinCos(s.Seconds * 0.6f);
        var cursor = s.At(cos * 3f, Lift, 2f + sin * 1.2f);

        shapes.Glow.Set(8f, new Color(0, 0, 0, 200));
        shapes.DrawRing(cursor, s.Up, 0.7f, Color.White);
        shapes.DrawPixelLine(cursor - s.Right * 1.3f, cursor + s.Right * 1.3f, 1.5f, Color.White);
        shapes.DrawPixelLine(cursor - s.Forward * 1.3f, cursor + s.Forward * 1.3f, 1.5f, Color.White);

        shapes.Glow.Set(28f);
        shapes.Glow.Strength = 0.35f;
        shapes.Glow.Additive = true;
        shapes.DrawRing(s.At(0f, 2.6f, -1f), s.Forward, 1.2f, Color.Cyan);
        shapes.DrawDisc(s.At(-3.4f, 2.6f, -1f), s.Forward, 0.9f, Color.Magenta);
        shapes.DrawArc(s.At(3.4f, 2.6f, -1f), s.Forward, 1.2f, s.Seconds * 1.5f, MathF.PI * 1.2f, Color.OrangeRed);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// Dashes are measured in pixels like the border and belong to rings, arcs and lines; advancing
    /// the phase turns a ring or marches a line. Three dashed rings turning at their own speeds and
    /// dash-to-gap ratios - tight ticks, half and half, sparse dots - the middle one with an
    /// additive glow, the right one breathing through its opacity; behind them a tick ring that
    /// never moves, so the turning ones can be judged against it, and one turning inside it. A
    /// ring's dashes are fitted once, in its own units, so they foreshorten with it like marks
    /// painted on the ground and stay in step however the camera moves.
    /// </summary>
    public static void Dash(ShapeStation s)
    {
        var shapes = s.Shapes;

        ReadOnlySpan<(float Dash, float Gap, float Speed)> rings = [(6f, 4f, 30f), (10f, 10f, -20f), (3f, 12f, 45f)];

        for (var i = 0; i < rings.Length; i++)
        {
            var (dash, gap, speed) = rings[i];
            var centre = s.At(-3.6f + i * 3.6f, Lift, 1.5f);

            shapes.Dash.Set(dash, gap, s.Seconds * speed);

            switch (i)
            {
                case 0:
                    shapes.BorderWidth = 2f;
                    shapes.DrawRing(centre, s.Up, 1.4f, Color.Cyan);
                    break;
                case 1:
                    shapes.BorderWidth = 3f;
                    shapes.Glow.Set(8f, new Color(255, 140, 0, 150));
                    shapes.Glow.Additive = true;
                    shapes.DrawRing(centre, s.Up, 1.4f, Color.Orange);
                    shapes.Glow.Clear();
                    break;
                default:
                    shapes.BorderWidth = 5f;
                    shapes.Opacity = 0.55f + 0.45f * MathF.Sin(s.Seconds * 2f);
                    shapes.DrawRing(centre, s.Up, 1.4f, Color.GreenYellow);
                    shapes.Opacity = 1f;
                    break;
            }
        }

        shapes.BorderWidth = s.Style.BorderWidth;

        shapes.Dash.Set(8f, 6f);
        shapes.DrawRing(s.At(0f, Lift, -2f), s.Up, 3.2f, Color.Orange);
        shapes.Dash.Set(8f, 6f, s.Seconds * 25f);
        shapes.DrawRing(s.At(0f, Lift, -2f), s.Up, 2.6f, Color.Orange);
        shapes.Dash.Clear();
    }

    /// <summary>
    /// A gradient runs the fill from its colour to another across the shape's own extent: a bar
    /// filling to its bright end, and a glass pane that fades to nothing along its length. The
    /// direction is in the plane's own axes, so X runs along the station's right.
    /// </summary>
    public static void Gradient(ShapeStation s)
    {
        var shapes = s.Shapes;
        var bar = (MathF.Sin(s.Seconds * 0.9f) + 1f) * 0.5f;

        shapes.BorderWidth = 1.5f;
        shapes.Fill.Set(new Color(255, 120, 40, 110), 1f);
        shapes.Gradient.Set(new Color(255, 230, 120), Vector2.UnitX);
        shapes.DrawRectangle(s.At(-3f + 3f * bar, 1f, 0f), s.Right, s.Up, new Vector2(6f * bar, 0.7f), Color.Orange);
        shapes.Gradient.Clear();

        shapes.Fill.Set(new Color(120, 200, 255, 140), 1f);
        shapes.Gradient.Set(new Color(120, 200, 255, 0), Vector2.UnitX);
        shapes.DrawRectangle(s.At(0f, 3.4f, 0f), s.Right, s.Up, new Vector2(8f, 2.4f), new Color(120, 200, 255), cornerRadius: 0.3f);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// Opacity dims everything a shape draws with one number: the same glowing billboard at 1, 0.65
    /// and 0.3, border, fill and glow together. It is how a widget goes disabled or fades in.
    /// </summary>
    public static void Opacity(ShapeStation s)
    {
        var shapes = s.Shapes;

        shapes.Glow.Set(10f);

        for (var i = 0; i < 3; i++)
        {
            shapes.Opacity = 1f - i * 0.35f;
            shapes.DrawBillboardCircle(s.At(-3.6f + i * 3.6f, 3f, 0f), 0.8f, Color.Gold);
        }

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// The soft depth fade. A shape fades out over a distance as it approaches scene geometry
    /// instead of cutting off at the depth test: a ring standing a hand in front of the pillar dims
    /// over the pillar and stays bright beside it; a marker sunk in the floor melts into it, next
    /// to the same marker without the fade, sliced flat. Only shows on the depth-tested batch.
    /// </summary>
    public static void DepthFade(ShapeStation s)
    {
        var shapes = s.Shapes;
        var pillar = s.Pillars[0];

        shapes.DepthFade = 0.6f;
        shapes.DrawRing(pillar.Base + Vector3.UnitY * 2.2f + s.Forward * 1.1f, s.Forward, 1.5f, Color.LightGreen);

        shapes.DepthFade = 1.5f;
        shapes.DrawBillboardCircle(s.At(1.6f, 0.5f, 0.5f), 0.9f, Color.Gold);

        shapes.DepthFade = 0f;
        shapes.DrawBillboardCircle(s.At(4.2f, 0.5f, 0.5f), 0.9f, Color.Gold);
    }

    /// <summary>
    /// Two batches, from two calls to AddShapeBatch. The depth-tested one belongs to the scene: its
    /// cyan ring behind the pillar is hidden by it. The overlay one draws over everything: its pink
    /// ring in the same place shows through the pillar, which is what gizmos and debug marks want.
    /// T switches every other station between the two; this one draws through both at once.
    /// </summary>
    public static void OverlayBatch(ShapeStation s)
    {
        var pillar = s.Pillars[0];
        var behind = pillar.Base + Vector3.UnitY * 2f - s.Forward * 2f;

        s.Batches.Scene.DrawRing(behind + s.Right * 0.7f, s.Forward, 1.4f, Color.Cyan);
        s.Batches.Overlay.DrawRing(behind - s.Right * 0.7f, s.Forward, 1.4f, Color.HotPink);
    }

    /// <summary>
    /// A textured batch: a batch created with a fill source samples it for every shape drawn while
    /// Textured is on. The gallery's picture, generated in code with a white square at its top-left
    /// so the mapping is unmistakable, fills a glowing HUD panel edge to edge, a disc with its
    /// inscribed square, and a disc tinted orange - the sample multiplies the fill colour.
    /// </summary>
    public static void TexturedFill(ShapeStation s)
    {
        var shapes = s.Batches.Pictures;

        shapes.Fill.Set(Color.White, 1f);
        shapes.Glow.Set(7f, HudGlow);
        shapes.DrawRectangle(s.At(-2.5f, 2.4f, 0f), s.Right, s.Up, new Vector2(5f, 3.5f), HudBlue, cornerRadius: 0.5f);
        shapes.Glow.Clear();

        shapes.DrawDisc(s.At(2.2f, 2.1f, 0f), s.Forward, 1.5f, Color.Cyan);

        shapes.Fill.Set(new Color(255, 160, 60), 1f);
        shapes.DrawDisc(s.At(4.6f, 1.2f, 1f), s.Up, 1.1f, Color.White);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// The same picture through a batch whose fill source tiles it four times across with wrap
    /// addressing. The node is re-read every frame, so scrolling is one assignment to its offset.
    /// </summary>
    public static void ScrollingTexture(ShapeStation s)
    {
        var shapes = s.Batches.Stripes;

        s.Batches.Stripe.Offset = new Vector2(s.Seconds * 0.25f, 0f);

        shapes.BorderWidth = 2f;
        shapes.Fill.Set(Color.White, 1f);
        shapes.DrawRectangle(s.At(0f, 1.4f, 0f), s.Right, s.Up, new Vector2(10f, 1.6f), Color.White, cornerRadius: 0.2f);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// A fill from a shader class instead of a texture: <c>Effects/GalleryPlasma.sdsl</c>, twenty lines
    /// that derive from <c>ComputeColor</c>, named by a <c>ComputeShaderClassColor</c> as the batch's
    /// fill source. The shader sees the same coordinates a texture is sampled with, 0 to 1 across the
    /// shape's bounding box, so each shape gets the whole pattern; the outline, the glow and the rounded
    /// corners are still the batch's. The clock is a node composed into the shader and set here every
    /// frame, since a shape batch's effect has no clock of its own.
    /// </summary>
    public static void ShaderFill(ShapeStation s)
    {
        var shapes = s.Batches.Shaded;

        s.Batches.Clock.Value = new Vector4(s.Seconds, 0f, 0f, 0f);

        shapes.Fill.Set(Color.White, 1f);
        shapes.Glow.Set(7f, HudGlow);
        shapes.DrawRectangle(s.At(-2.5f, 2.4f, 0f), s.Right, s.Up, new Vector2(5f, 3.5f), HudBlue, cornerRadius: 0.5f);
        shapes.Glow.Clear();

        shapes.DrawDisc(s.At(2.2f, 2.1f, 0f), s.Forward, 1.5f, Color.White);

        // The sample multiplies the fill colour, so a shader fill tints like a texture does
        shapes.Fill.Set(new Color(255, 160, 60), 1f);
        shapes.DrawDisc(s.At(4.6f, 1.2f, 1f), s.Up, 1.1f, Color.White);

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// Builds the second camera and the texture it draws into, with one toolkit call. Behind it: a
    /// texture that is both a render target and a shader resource, in HDR; a camera slot of its
    /// own; a camera renderer wrapping a render-texture renderer wrapping a second forward renderer
    /// over the compositor's existing stages. The E09_3D_RenderToTexture example takes the call
    /// further, with five feeds and two of them wearing a look of their own.
    /// </summary>
    public static void MirrorSetup(ShapeStation s)
    {
        // Beside the pad, looking at the station's own pillar from a few units away, so the panel
        // is unmistakably another camera's view
        var eye = s.At(-3.5f, 2.6f, 4f);
        var direction = Vector3.Normalize(s.Pillars[0].Centre - eye);
        var camera = new Entity("Mirror camera");

        camera.Transform.Position = eye;

        // A camera looks down its -Z; yaw turns that towards -X, pitch lifts it
        camera.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathF.Atan2(-direction.X, -direction.Z), MathF.Asin(direction.Y), 0f);
        camera.Scene = s.Scene;

        var feed = s.Game.AddRenderTextureCamera(camera, 512, 512);
        var batch = s.Game.AddShapeBatch(depthTest: true);

        batch.FillWith(feed.Texture);

        s.State = batch;
    }

    /// <summary>
    /// A second camera's view, live, inside a shape. The panel is filled from a texture the renderer
    /// draws that camera into every frame, so it is a rear-view mirror, a security monitor or a map;
    /// here it watches the station's own pillar from the side. Everything the shape batch does still
    /// applies to it: the outline holds its pixel width, the glow sits outside the border, and the
    /// corner brackets are pixel lines over the picture.
    /// </summary>
    /// <remarks>
    /// The shapes of every station appear in the mirror because a batch is drawn once per view and
    /// emptied only after the last one, which is what lets a second camera see the same frame as the
    /// first.
    /// </remarks>
    public static void Mirror(ShapeStation s)
    {
        if (s.State is not ShapeBatch shapes) return;

        var centre = s.At(0f, 3f, -1f);

        shapes.Fill.Set(Color.White, 1f);
        shapes.BorderWidth = 2f;
        shapes.Glow.Set(8f, HudGlow);
        shapes.DrawRectangle(centre, s.Right, s.Up, new Vector2(5f, 5f), HudBlue, cornerRadius: 0.3f);
        shapes.Glow.Clear();

        // Brackets over the picture, and a sweep turning like a radar
        shapes.Textured = false;

        var topLeft = centre - s.Right * 2.25f + s.Up * 2.25f;
        var bottomRight = centre + s.Right * 2.25f - s.Up * 2.25f;

        shapes.DrawPixelLine(topLeft, topLeft + s.Right * 0.7f, 2f, HudBlue);
        shapes.DrawPixelLine(topLeft, topLeft - s.Up * 0.7f, 2f, HudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight - s.Right * 0.7f, 2f, HudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight + s.Up * 0.7f, 2f, HudBlue);

        var angle = s.Seconds * 0.6f;

        shapes.DrawPixelLine(centre, centre + (s.Right * MathF.Cos(angle) + s.Up * MathF.Sin(angle)) * 2.3f, 2f, new Color(120, 255, 180, 160));

        s.ResetStyle(shapes);
    }

    /// <summary>
    /// Screen shapes: pixels from the top left of the window, Y down like a sprite, from the same
    /// batch as everything in the world and drawn over it whatever the depth test says. Shown only
    /// while the visitor stands at this station, because a HUD is on the screen, not on the ring.
    /// A crosshair in the middle, a gauge and a panel placed with Corner() so they follow the window,
    /// and a bar drawn into a viewport rectangle in that rectangle's own coordinates.
    /// </summary>
    public static void ScreenHud(ShapeStation s)
    {
        var shapes = s.Shapes;

        // On the pad, so the station has something when the visitor is elsewhere
        shapes.DrawRing(s.At(0f, Lift, 0f), s.Up, 2.2f, Color.White);

        if (!s.IsCurrent) return;

        shapes.Screen = true;

        var centre = shapes.ScreenSize * 0.5f;

        shapes.BorderWidth = 2f;
        shapes.DrawRing(centre, 22f, Color.White);
        shapes.DrawPixelLine(new Vector2(centre.X - 40f, centre.Y), new Vector2(centre.X - 10f, centre.Y), 2f, Color.White);
        shapes.DrawPixelLine(new Vector2(centre.X + 10f, centre.Y), new Vector2(centre.X + 40f, centre.Y), 2f, Color.White);
        shapes.DrawPixelLine(new Vector2(centre.X, centre.Y - 40f), new Vector2(centre.X, centre.Y - 10f), 2f, Color.White);
        shapes.DrawPixelLine(new Vector2(centre.X, centre.Y + 10f), new Vector2(centre.X, centre.Y + 40f), 2f, Color.White);

        // A radial gauge in the bottom-left corner, clockwise from twelve because Y is down
        var gauge = shapes.Corner(ScreenCorner.BottomLeft) + new Vector2(110f, -110f);
        var level = (MathF.Sin(s.Seconds * 0.8f) + 1f) * 0.5f;

        shapes.Fill.Alpha = 0.25f;
        shapes.DrawArc(gauge, 50f, 0f, MathF.Tau, Color.Gray, width: 14f);
        shapes.Fill.Alpha = 0.9f;
        shapes.DrawArc(gauge, 50f, -MathF.PI * 0.5f, MathF.Tau * level, Color.LimeGreen, width: 14f);

        // A panel in the bottom-right corner, HUD style, with a dashed ring turning inside it
        var panel = shapes.Corner(ScreenCorner.BottomRight) + new Vector2(-150f, -90f);

        shapes.Fill.Set(HudFill, 0.7f);
        shapes.BorderWidth = 1.5f;
        shapes.Glow.Set(8f, HudGlow);
        shapes.DrawRectangle(panel, new Vector2(260f, 140f), HudBlue, cornerRadius: 12f);
        shapes.Glow.Clear();
        shapes.Fill.Set(null, 0.45f);
        shapes.Dash.Set(8f, 6f, s.Seconds * 30f);
        shapes.DrawRing(panel, 44f, Color.Orange);
        shapes.Dash.Clear();

        // A viewport rectangle across the bottom: the bar is drawn in the rectangle's own coordinates
        shapes.Viewport = new RectangleF(centre.X - 200f, shapes.ScreenSize.Y - 60f, 400f, 40f);
        shapes.Fill.Set(new Color(255, 120, 40, 200), 1f);
        shapes.Gradient.Set(new Color(255, 230, 120), Vector2.UnitX);
        shapes.DrawRectangle(new Vector2(200f, 20f), new Vector2(360f * level + 20f, 16f), Color.Orange);
        shapes.Gradient.Clear();
        shapes.Viewport = null;

        s.ResetStyle(shapes);
        shapes.Screen = false;
    }

    /// <summary>The label above the station that says what the mouse is over.</summary>
    public static void PickingSetup(ShapeStation s)
    {
        var text = new WorldTextComponent
        {
            Text = "",
            FontSize = 40,
            Height = 0.32f,
            TextColor = HudBlue,
            GlowColor = new Color(0, 140, 255, 170),
            GlowSize = 3f,
            Alignment = Stride.Graphics.TextAlignment.Center,
            Billboard = false,
        };

        var entity = new Entity($"Station {s.Number} pick label")
        {
            Transform = { Position = s.At(0f, 4f, -1f), Rotation = s.FacingRotation() },
        };

        entity.Add(text);
        entity.Scene = s.Scene;
        s.State = text;
    }

    /// <summary>
    /// The batch knows which shape is under the mouse, from what it drew last frame and with the
    /// same distance functions the pixels are painted with. Every shape here carries a tag; the one
    /// under the mouse lights up, and the label says what it is, where on it the mouse is in the
    /// shape's own coordinates, and how far inside the outline. The filled shapes and the border
    /// are hit exactly as drawn; a few pixels of slack make the ring, the line and the polyline
    /// easy to catch.
    /// </summary>
    public static void Picking(ShapeStation s)
    {
        var shapes = s.Shapes;

        // What was under the mouse as of the frame last drawn; only while the visitor is here, so
        // the other stations' shapes are not lit from afar
        var hovered = s.IsCurrent && shapes.TryPick(s.Game.Input.MousePosition, out var hit, slackPixels: 4f) ? hit : (ShapeHit?)null;

        Tagged("disc", () => shapes.DrawDisc(s.At(-2.6f, Lift, 1.5f), s.Up, 1.1f, Color.DeepSkyBlue));
        Tagged("ring", () => shapes.DrawRing(s.At(0f, Lift, 1.5f), s.Up, 1.1f, Color.Gold));
        Tagged("sector", () => shapes.DrawSector(s.At(2.6f, Lift, 1.5f), s.Up, 1.2f, 0.4f, 4.4f, Color.Orange));
        Tagged("panel", () => shapes.DrawRectangle(s.At(-2.2f, 2f, -1f), s.Right, s.Up, new Vector2(2.4f, 1.4f), Color.MediumPurple, cornerRadius: 0.3f));
        Tagged("line", () => shapes.DrawLine(s.At(-0.4f, 1.2f, -1f), s.At(2.6f, 3f, -1f), 0.12f, Color.LimeGreen));
        Tagged("marker", () => shapes.DrawPixelDisc(s.At(2.6f, 1.2f, -1f), 12f, Color.White));
        Tagged("polyline", () => shapes.DrawPixelPolyline([new(-2.6f, 0f), new(-1.3f, 1f), new(0f, 0f), new(1.3f, 1f), new(2.6f, 0f)], s.At(0f, Lift, -3f), s.Right, s.Forward, 3f, Color.Tomato));

        if (s.State is WorldTextComponent label)
        {
            label.Text = hovered is { } h
                ? $"{h.Tag}   local ({h.Local.X:0.00}, {h.Local.Y:0.00})   distance {h.Distance:0.00}"
                : "move the mouse over a shape";
        }

        // Every shape drawn under its own tag; the hovered one with a fuller fill and a glow of its
        // own colour, the style put back after each
        void Tagged(string name, Action draw)
        {
            shapes.Tag = name;

            if (hovered is { } current && Equals(current.Tag, name))
            {
                shapes.Fill.Set(null, 0.85f);
                shapes.Glow.Set(14f);
                shapes.Glow.Strength = 0.5f;
                shapes.Glow.Additive = true;
            }

            draw();
            s.ResetStyle(shapes);
            shapes.Tag = null;
        }
    }
}