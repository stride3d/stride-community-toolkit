using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The layout: a rounded frame and three columns of widgets inside it, stacked top down. One
/// padding separates the frame from the columns, the columns from each other and the widgets in a
/// column. A widget draws inside the rectangle it is given, so moving one is moving a line here.
/// </summary>
public sealed class Hud
{
    public const float Width = 32.4f;
    public const float Height = 18f;
    public const float Padding = 0.4f;

    private const float Corner = 0.8f;
    private const float SideColumn = 8f;
    private const float Tape = 2.6f;

    private readonly HudWidget _root;

    public Hud(ShipState ship)
    {
        var left = new HudColumn(Padding,
            new RadarWidget(ship).Sized(7f),
            new ContactsWidget(ship).Sized(4f),
            new CommsWidget(ship));

        var flight = new HudRow(Padding,
            new TapeGaugeWidget("speed", "VEL", () => ship.Flight.Speed, range: 300f, inward: 1f).Sized(Tape),
            new SightWidget(ship),
            new TapeGaugeWidget("altitude", "ALT M", () => ship.Flight.Altitude, range: 2000f, inward: -1f).Sized(Tape));

        var centre = new HudColumn(Padding,
            new HeadingTapeWidget(ship).Sized(1.9f),
            flight.Sized(5.4f),
            new WarningWidget(ship).Sized(0.8f),
            new HudRow(Padding, new TraceWidget(), new SpectrumWidget(ship)).Sized(1.9f),
            new GaugesWidget(ship).Sized(2.6f),
            new SystemsWidget(ship));

        var right = new HudColumn(Padding,
            new TargetWidget(ship).Sized(4.4f),
            new WingWidget(ship).Sized(5.6f),
            new PowerWidget(ship).Sized(4.4f),
            new ModesWidget(ship));

        _root = new HudRow(Padding, left.Sized(SideColumn), centre, right.Sized(SideColumn));
    }

    public void Draw(HudCanvas canvas)
    {
        var frame = HudRect.Centered(Vector2.Zero, new Vector2(Width, Height));

        DrawFrame(canvas, frame);

        _root.Draw(canvas, frame.Inset(Padding));
    }

    /// <summary>
    /// The edge of the canopy glass: a rounded outline, with each corner drawn again brighter as an
    /// arc and two short arms.
    /// </summary>
    private static void DrawFrame(HudCanvas canvas, HudRect frame)
    {
        const float Arm = 1.2f;

        var theme = canvas.Theme;
        var accent = theme.For(HudRole.Frame);

        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Shapes.DrawRectangle(new Vector3(frame.Center, 0f), Vector3.UnitX, Vector3.UnitY, frame.Size, theme.Dim(HudRole.Frame), Corner);

        canvas.Style(HudCanvas.Thick, 0f, glow: 4f, glowColour: theme.Glow, additive: true);

        // Counter-clockwise from the top right, a quarter turn each
        ReadOnlySpan<Vector2> sides = [new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f)];

        for (var i = 0; i < sides.Length; i++)
        {
            var side = sides[i];
            var corner = frame.Center + side * (frame.Size / 2f);
            var center = corner - side * Corner;

            canvas.Shapes.DrawArc(center, Corner, i * MathF.PI / 2f, MathF.PI / 2f, accent);
            canvas.Line(new Vector2(center.X, corner.Y), new Vector2(center.X - side.X * Arm, corner.Y), HudCanvas.Thick, accent);
            canvas.Line(new Vector2(corner.X, center.Y), new Vector2(corner.X, center.Y - side.Y * Arm), HudCanvas.Thick, accent);
        }
    }
}