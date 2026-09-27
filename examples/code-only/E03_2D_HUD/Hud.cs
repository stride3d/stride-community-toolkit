using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The layout: a frame with cut corners and three columns of widgets inside it, stacked top down. One
/// padding separates the frame from the columns, the columns from each other and the widgets in a
/// column. A widget draws inside the rectangle it is given, so moving one is moving a line here.
/// </summary>
public sealed class Hud
{
    public const float Width = 32.4f;
    public const float Height = 18f;
    public const float Padding = 0.4f;

    // The frame's corners are cut as much as a panel's
    private const float Cut = HudPanel.Cut;
    private const float SideColumn = 8f;
    private const float Tape = 2.6f;

    private readonly HudWidget _root;
    private readonly FrameLights _lights = new(seed: 2026);

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

        // In the middle of the band the padding leaves between the frame and the panels
        _lights.Draw(canvas, frame, Padding / 2f);

        _root.Draw(canvas, frame.Inset(Padding));
    }

    /// <summary>
    /// The edge of the canopy glass: an outline with its corners cut at 45 degrees, like the
    /// panels, and each corner drawn again brighter as one stroke with two short arms.
    /// </summary>
    private static void DrawFrame(HudCanvas canvas, HudRect frame)
    {
        const float Arm = 1.2f;

        var theme = canvas.Theme;
        var accent = theme.For(HudRole.Frame);
        var half = frame.Size / 2f;

        ReadOnlySpan<Vector2> outline =
        [
            new(-half.X + Cut, -half.Y), new(half.X - Cut, -half.Y), new(half.X, -half.Y + Cut), new(half.X, half.Y - Cut),
            new(half.X - Cut, half.Y), new(-half.X + Cut, half.Y), new(-half.X, half.Y - Cut), new(-half.X, -half.Y + Cut),
        ];

        canvas.Style(HudCanvas.Thin, 0f);
        canvas.Shapes.DrawPixelPolyline(outline, new Vector3(frame.Center, 0f), Vector3.UnitX, Vector3.UnitY, HudCanvas.Thin, theme.Dim(HudRole.Frame), closed: true);

        canvas.Style(HudCanvas.Thick, 0f, glow: 4f, glowColour: theme.Glow, additive: true);

        foreach (var side in (ReadOnlySpan<Vector2>)[new(1f, 1f), new(-1f, 1f), new(-1f, -1f), new(1f, -1f)])
        {
            var corner = frame.Center + side * half;

            // Along the top or bottom edge, down the cut, along the left or right edge
            ReadOnlySpan<Vector2> stroke =
            [
                corner - new Vector2(side.X * (Cut + Arm), 0f),
                corner - new Vector2(side.X * Cut, 0f),
                corner - new Vector2(0f, side.Y * Cut),
                corner - new Vector2(0f, side.Y * (Cut + Arm)),
            ];

            canvas.Shapes.DrawPixelPolyline(stroke, HudCanvas.Thick, accent);
        }
    }
}