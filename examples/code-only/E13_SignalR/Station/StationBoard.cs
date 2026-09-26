using E13_SignalR_Shared;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System.Globalization;

namespace E13_SignalR.Station;

/// <summary>
/// The web console's DECK panel, hung in the scene: title and link lamp, the scheme buttons, four
/// counters, bars by size and by paint. Drawn again every frame from the current census in the
/// current scheme's colours, so nothing here has to be kept in sync with anything. The scheme
/// buttons are real buttons: the mouse is picked against the board's plane, and a click selects.
/// </summary>
public sealed class StationBoard
{
    private const float Width = 18f;
    private const float Height = 9.5f;
    private const float Margin = 8.5f;
    private const float TopRow = 4f;
    private const float SubtitleRow = 3.4f;
    private const float RailRow = 3f;
    private const float LinkRow = 2.55f;
    private const float CounterRow = 1.2f;
    private const float CaptionRow = 0.35f;
    private const float SectionRow = -0.7f;
    private const float FirstBarRow = -1.3f;
    private const float RowStep = 0.58f;
    private const float BarHeight = 0.28f;
    private const float ButtonGap = 0.16f;

    private static readonly Vector2 ButtonSize = new(1.6f, 0.66f);
    private static readonly string[] Counters = ["ON DECK", "RELEASED", "LOST", "TONNES"];
    private static readonly float[] CounterColumns = [-6.5f, -2.2f, 2.2f, 6.5f];
    private static readonly string[] SizeNames = ["Small", "Medium", "Large"];
    private static readonly Color Online = new(80, 245, 150);
    private static readonly Color Offline = new(255, 170, 70);

    private readonly Board _board;
    private readonly Labels _labels;
    private readonly Color[] _paintColors = [.. Paints.All.Select(paint => Hex.ToColor(Paints.Hex(paint)))];
    private readonly Color[] _schemeAccents = [.. Schemes.All.Select(scheme => Hex.ToColor(scheme.Accent))];
    private readonly Color[] _schemeFills = [.. Schemes.All.Select(scheme => Hex.ToColor(scheme.Fill))];

    private int _hovered = -1;

    public StationBoard(Labels labels, StationConsole console, Vector3 center, Vector3 facing)
    {
        _board = new Board(center, facing, new Vector2(Width, Height));
        _labels = labels;

        labels.Add("title", 0.66f, labels.Bold, (t, c) => { t.TextColor = c.Text; t.GlowColor = c.Glow; }, TextAnchor.MiddleLeft, glow: 4f);
        labels.Add("subtitle", 0.36f, labels.Sans, (t, c) => t.TextColor = Hex.WithAlpha(c.Accent, 190), TextAnchor.MiddleLeft);

        // Coloured per frame by the scheme each button stands for, not by the current one
        for (var i = 0; i < Schemes.All.Length; i++)
        {
            labels.Add($"scheme-{i}", 0.36f, labels.Bold, (_, _) => { });
        }

        labels.Add("link", 0.42f, labels.Sans, (_, _) => { }, TextAnchor.MiddleLeft);
        labels.Add("uptime", 0.42f, labels.Mono, (t, c) => t.TextColor = Hex.WithAlpha(c.Text, 170), TextAnchor.MiddleRight);

        foreach (var counter in Counters)
        {
            labels.Add($"{counter}-value", 1.25f, labels.Mono, (t, c) => { t.TextColor = c.Accent; t.GlowColor = c.Glow; }, glow: 3f);
            labels.Add($"{counter}-caption", 0.36f, labels.Bold, (t, c) => t.TextColor = Hex.WithAlpha(c.Text, 150));
        }

        labels.Add("by-size", 0.36f, labels.Bold, (t, c) => t.TextColor = c.Accent, TextAnchor.MiddleLeft);
        labels.Add("by-paint", 0.36f, labels.Bold, (t, c) => t.TextColor = c.Accent, TextAnchor.MiddleLeft);
        labels.Add("dropping", 0.38f, labels.Sans, (t, c) => t.TextColor = c.Accent, TextAnchor.MiddleLeft);

        for (var i = 0; i < SizeNames.Length; i++)
        {
            labels.Add($"size-{i}", 0.42f, labels.Sans, (t, c) => t.TextColor = c.Text, TextAnchor.MiddleLeft);
            labels.Add($"size-{i}-count", 0.42f, labels.Mono, (t, c) => t.TextColor = c.Text, TextAnchor.MiddleRight);
        }

        for (var i = 0; i < Paints.All.Length; i++)
        {
            labels.Add($"paint-{i}", 0.4f, labels.Sans, (t, c) => t.TextColor = c.Text, TextAnchor.MiddleLeft);
            labels.Add($"paint-{i}-count", 0.4f, labels.Mono, (t, c) => t.TextColor = c.Text, TextAnchor.MiddleRight);
        }
    }

    /// <summary>
    /// Tracks the mouse over the scheme buttons and returns the scheme clicked this frame, if any.
    /// Call before <see cref="Draw"/>, which highlights the hovered button. The buttons are drawn
    /// with their index as the tag, so the batch says which one the mouse is over.
    /// </summary>
    public string? Pick(ShapeBatch shapes, InputManager input)
    {
        _hovered = shapes.TryPick(input.MousePosition, out var hit) && hit.Tag is int index ? index : -1;

        return _hovered >= 0 && input.IsMouseButtonPressed(MouseButton.Left) ? Schemes.All[_hovered].Name : null;
    }

    public void Draw(ShapeBatch shapes, StationConsole console, DeckSnapshot snapshot, int pending, bool linkOnline, float uptime, float time)
    {
        var accent = console.Accent;
        var faint = Hex.WithAlpha(accent, 70);

        // The panel: the scheme's dark fill, its accent as a border, its glow behind
        shapes.BorderWidth = 1.5f;
        shapes.Fill.Set(console.Fill, 0.9f);
        shapes.Glow.Set(10f, Hex.WithAlpha(console.Glow, 110));
        shapes.DrawRectangle(_board.Center, _board.AxisX, _board.AxisY, _board.Size, Hex.WithAlpha(accent, 200), 0.35f);
        shapes.Glow.Clear();
        shapes.Fill.Set(null, 0f);

        DrawCornerTicks(shapes, accent);

        // A marching rail under the header, and quiet dividers between the sections
        shapes.Dash.Set(8f, 6f, time * 12f);
        Line(shapes, -Margin, RailRow, Margin, RailRow, 1f, Hex.WithAlpha(accent, 110));
        shapes.Dash.Clear();

        Line(shapes, -Margin, LinkRow - 0.42f, Margin, LinkRow - 0.42f, 1f, faint);
        Line(shapes, -Margin, SectionRow + 0.45f, Margin, SectionRow + 0.45f, 1f, faint);
        Line(shapes, 0f, SectionRow + 0.2f, 0f, -Height / 2f + 0.4f, 1f, Hex.WithAlpha(accent, 50));

        _labels.Set("title", Constants.StationName.ToUpperInvariant(), _board, -Margin, TopRow);
        _labels.Set("subtitle", "game console · Stride + SignalR · click a scheme, or press T", _board, -Margin, SubtitleRow);

        DrawSchemeButtons(shapes, console);
        DrawLinkRow(shapes, linkOnline, uptime);
        DrawCounters(snapshot);
        DrawBySize(shapes, console, snapshot, pending);
        DrawByPaint(shapes, console, snapshot);
    }

    private void DrawSchemeButtons(ShapeBatch shapes, StationConsole console)
    {
        for (var i = 0; i < Schemes.All.Length; i++)
        {
            var center = ButtonCenter(i);
            var scheme = Schemes.All[i];
            var selected = scheme == console.Scheme;
            var hovered = i == _hovered;
            var accent = _schemeAccents[i];
            var fill = _schemeFills[i];

            // The website's buttons: each in its own scheme's colours, the chosen one lit solid
            shapes.BorderWidth = hovered ? 2f : 1.2f;
            shapes.Fill.Set(selected ? accent : fill, selected ? 0.9f : 0.95f);

            if (hovered || selected)
            {
                shapes.Glow.Set(hovered ? 9f : 5f, Hex.WithAlpha(accent, (byte)(hovered ? 170 : 110)));
                shapes.Glow.Additive = true;
            }

            shapes.Tag = i;
            shapes.DrawRectangle(_board.Place(center), _board.AxisX, _board.AxisY, ButtonSize, selected ? accent : Hex.WithAlpha(accent, 210), 0.12f);
            shapes.Tag = null;
            shapes.Glow.Clear();

            _labels.Set($"scheme-{i}", scheme.Name, _board, center.X, center.Y, selected ? fill : accent);
        }

        shapes.Fill.Set(null, 0f);
        shapes.BorderWidth = 1.5f;
    }

    private void DrawLinkRow(ShapeBatch shapes, bool linkOnline, float uptime)
    {
        var color = linkOnline ? Online : Offline;

        // The lamp: a solid disc with a glow, in the link's colour
        shapes.BorderWidth = 0f;
        shapes.Fill.Set(color, 1f);
        shapes.Glow.Set(6f, Hex.WithAlpha(color, 150));
        shapes.Glow.Additive = true;
        shapes.DrawDisc(_board.Place(-Margin + 0.25f, LinkRow), _board.Normal, 0.15f, color);
        shapes.Glow.Clear();
        shapes.Fill.Set(null, 0f);
        shapes.BorderWidth = 1.5f;

        _labels.Set("link", linkOnline ? "LINK ONLINE · web console connected" : "LINK OFFLINE · looking for the hub, keyboard still works", _board, -Margin + 0.65f, LinkRow, color);
        _labels.Set("uptime", $"UPTIME {TimeSpan.FromSeconds(uptime):h\\:mm\\:ss}", _board, Margin, LinkRow);
    }

    private void DrawCounters(DeckSnapshot snapshot)
    {
        string[] values = [snapshot.OnDeck.ToString(CultureInfo.InvariantCulture), snapshot.Released.ToString(CultureInfo.InvariantCulture), snapshot.Lost.ToString(CultureInfo.InvariantCulture), snapshot.TotalMass.ToString("0.0", CultureInfo.InvariantCulture)];

        for (var i = 0; i < Counters.Length; i++)
        {
            _labels.Set($"{Counters[i]}-value", values[i], _board, CounterColumns[i], CounterRow);
            _labels.Set($"{Counters[i]}-caption", Counters[i], _board, CounterColumns[i], CaptionRow);
        }
    }

    private void DrawBySize(ShapeBatch shapes, StationConsole console, DeckSnapshot snapshot, int pending)
    {
        _labels.Set("by-size", "BY SIZE", _board, -Margin, SectionRow);

        var most = Math.Max(1, snapshot.BySize.Max());

        for (var i = 0; i < SizeNames.Length; i++)
        {
            var v = FirstBarRow - i * RowStep;

            _labels.Set($"size-{i}", SizeNames[i], _board, -Margin, v);
            DrawBar(shapes, -6.3f, -1.7f, v, (float)snapshot.BySize[i] / most, console.Accent, console.Text);
            _labels.Set($"size-{i}-count", snapshot.BySize[i].ToString(), _board, -0.6f, v);
        }

        if (pending > 0)
        {
            _labels.Set("dropping", $"dropping {pending} more", _board, -Margin, FirstBarRow - 3.3f * RowStep);
        }
        else
        {
            _labels.Hide("dropping");
        }
    }

    private void DrawByPaint(ShapeBatch shapes, StationConsole console, DeckSnapshot snapshot)
    {
        _labels.Set("by-paint", "BY PAINT", _board, 0.7f, SectionRow);

        var most = Math.Max(1, snapshot.ByPaint.Max());

        for (var i = 0; i < Paints.All.Length; i++)
        {
            var v = FirstBarRow - i * RowStep;
            var paint = _paintColors[i];

            // The swatch: the paint itself, so the bar and the container on the deck match
            shapes.BorderWidth = 0f;
            shapes.Fill.Set(paint, 1f);
            shapes.DrawRectangle(_board.Place(0.85f, v), _board.AxisX, _board.AxisY, new Vector2(0.26f, 0.26f), paint, 0.04f);
            shapes.Fill.Set(null, 0f);
            shapes.BorderWidth = 1.5f;

            _labels.Set($"paint-{i}", Paints.All[i].ToString(), _board, 1.2f, v);
            DrawBar(shapes, 3.5f, 7.3f, v, (float)snapshot.ByPaint[i] / most, paint, console.Text);
            _labels.Set($"paint-{i}-count", snapshot.ByPaint[i].ToString(), _board, Margin, v);
        }
    }

    /// <summary>A track and the filled part of it, in board coordinates.</summary>
    private void DrawBar(ShapeBatch shapes, float from, float to, float v, float fraction, Color color, Color track)
    {
        var width = to - from;

        shapes.BorderWidth = 0f;
        shapes.Fill.Set(track, 0.1f);
        shapes.DrawRectangle(_board.Place(from + width / 2f, v), _board.AxisX, _board.AxisY, new Vector2(width, BarHeight), track, 0.08f);

        if (fraction > 0f)
        {
            var filled = width * MathUtil.Clamp(fraction, 0f, 1f);

            shapes.Fill.Set(color, 0.95f);
            shapes.Glow.Set(3f, Hex.WithAlpha(color, 90));
            shapes.Glow.Additive = true;
            shapes.DrawRectangle(_board.Place(from + filled / 2f, v, 0.04f), _board.AxisX, _board.AxisY, new Vector2(filled, BarHeight), color, 0.08f);
            shapes.Glow.Clear();
        }

        shapes.Fill.Set(null, 0f);
        shapes.BorderWidth = 1.5f;
    }

    /// <summary>
    /// An L in each corner with a rounded elbow that follows the panel's own corner: a quarter arc
    /// and a straight run along each edge from where the arc ends.
    /// </summary>
    private void DrawCornerTicks(ShapeBatch shapes, Color accent)
    {
        const float Tick = 0.7f;
        const float Radius = 0.25f;
        var half = _board.Half - new Vector2(0.15f, 0.15f);

        // The arc is a stroke at the border width, so it matches the lines' two pixels
        shapes.BorderWidth = 2f;

        foreach (var (signX, signY) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
        {
            var corner = new Vector2(signX * half.X, signY * half.Y);
            var elbow = corner - new Vector2(signX * Radius, signY * Radius);

            // The quadrant facing the corner: the arc's axes are the board's, so angles are in (u, v)
            var start = MathF.Atan2(signY, signX) - MathF.PI / 4f;

            shapes.DrawArc(_board.Place(elbow), _board.Normal, Radius, start, MathF.PI / 2f, accent);

            Line(shapes, elbow.X, corner.Y, elbow.X - signX * Tick, corner.Y, 2f, accent);
            Line(shapes, corner.X, elbow.Y, corner.X, elbow.Y - signY * Tick, 2f, accent);
        }

        shapes.BorderWidth = 1.5f;
    }

    private void Line(ShapeBatch shapes, float u1, float v1, float u2, float v2, float pixels, Color color)
        => shapes.DrawPixelLine(_board.Place(u1, v1), _board.Place(u2, v2), pixels, color);

    /// <summary>The buttons sit in the header row, right-aligned and in the website's order.</summary>
    private static Vector2 ButtonCenter(int index)
    {
        var span = Schemes.All.Length * ButtonSize.X + (Schemes.All.Length - 1) * ButtonGap;
        var first = Width / 2f - 0.45f - span + ButtonSize.X / 2f;

        return new Vector2(first + index * (ButtonSize.X + ButtonGap), TopRow);
    }
}