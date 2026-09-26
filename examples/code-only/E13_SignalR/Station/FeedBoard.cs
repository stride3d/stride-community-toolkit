using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;

namespace E13_SignalR.Station;

/// <summary>
/// The web console's FEED, as a narrower panel beside the deck: the last few things that happened,
/// newest on top, each with a tick in the colour of what it was - a release in the accent, a
/// landing in the text colour, a loss in red, a hail in yellow - and the older lines fading.
/// </summary>
public sealed class FeedBoard
{
    private const float Width = 7.5f;
    private const float Height = 4.6f;
    private const float Margin = 3.4f;
    private const int Lines = 6;
    private const float LineHeight = 0.42f;
    private const float FirstRow = 1.05f;
    private const float RowStep = 0.6f;
    private const int MaxChars = 30;

    private static readonly Color Lost = new(255, 95, 90);
    private static readonly Color Cleared = new(255, 170, 70);
    private static readonly Color Hail = new(255, 220, 80);

    private readonly Board _board;
    private readonly Labels _labels;

    public FeedBoard(Labels labels, StationConsole console, Vector3 center, Vector3 facing)
    {
        _board = new Board(center, facing, new Vector2(Width, Height));
        _labels = labels;

        labels.Add("feed-title", 0.32f, labels.Bold, (t, c) => t.TextColor = c.Accent, TextAnchor.MiddleLeft);
        labels.Add("feed-count", 0.3f, labels.Mono, (t, c) => t.TextColor = Hex.WithAlpha(c.Text, 150), TextAnchor.MiddleRight);

        for (var i = 0; i < Lines; i++)
        {
            // Colour set per frame by the line's age, so the restyle only has to note the text colour
            labels.Add($"feed-{i}", LineHeight, labels.Sans, (t, c) => t.TextColor = c.Text, TextAnchor.MiddleLeft);
        }
    }

    public void Draw(ShapeBatch shapes, StationConsole console, float time)
    {
        var accent = console.Accent;

        // The panel: a glowing bordered rectangle, then a marching dash under the title row
        shapes.BorderWidth = 1.5f;
        shapes.Fill.Set(console.Fill, 0.88f);
        shapes.Glow.Set(8f, Hex.WithAlpha(console.Glow, 100));
        shapes.DrawRectangle(_board.Center, _board.AxisX, _board.AxisY, _board.Size, Hex.WithAlpha(accent, 200), 0.3f);
        shapes.Glow.Clear();
        shapes.Fill.Set(null, 0f);

        shapes.Dash.Set(8f, 6f, -time * 12f);
        shapes.DrawPixelLine(_board.Place(-Margin, 1.5f), _board.Place(Margin, 1.5f), 1f, Hex.WithAlpha(accent, 110));
        shapes.Dash.Clear();

        // Title on the left, the count on the right, both sitting on the divider
        _labels.Set("feed-title", "FEED", _board, -Margin, 1.85f);
        _labels.Set("feed-count", $"{console.Log.Count} OF {Lines}", _board, Margin, 1.85f);

        // One label per row, newest first; rows past the end of the log are hidden
        var log = console.Log;

        for (var i = 0; i < Lines; i++)
        {
            var key = $"feed-{i}";

            if (i >= log.Count)
            {
                _labels.Hide(key);

                continue;
            }

            // Newest on top, and the older the line the dimmer it is
            var entry = log[log.Count - 1 - i];
            var v = FirstRow - i * RowStep;
            var age = (float)i / (Lines - 1);
            var alpha = (byte)(255 * (1f - 0.55f * age));
            var tick = KindColor(entry.Kind, console);

            // A colour tick at the left edge names the kind of entry
            shapes.BorderWidth = 0f;
            shapes.Fill.Set(tick, alpha / 255f);
            shapes.DrawRectangle(_board.Place(-Margin - 0.05f, v), _board.AxisX, _board.AxisY, new Vector2(0.08f, 0.34f), tick, 0.02f);
            shapes.Fill.Set(null, 0f);
            shapes.BorderWidth = 1.5f;

            _labels.Set(key, Fit(entry.Text), _board, -Margin + 0.2f, v, Hex.WithAlpha(entry.Kind == LogKind.Info ? console.Text : tick, alpha));
        }
    }

    private static Color KindColor(LogKind kind, StationConsole console) => kind switch
    {
        LogKind.Released => console.Accent,
        LogKind.Landed => console.Text,
        LogKind.Lost => Lost,
        LogKind.Cleared => Cleared,
        LogKind.Hail => Hail,
        _ => Hex.WithAlpha(console.Text, 160),
    };

    /// <summary>The board is narrow; a line that would run off it is cut rather than wrapped.</summary>
    private static string Fit(string text) => text.Length <= MaxChars ? text : text[..(MaxChars - 1)] + "…";
}