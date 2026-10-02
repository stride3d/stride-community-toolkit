using Stride.CommunityToolkit.Mathematics;
using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The comms log: the speaker in a column of their own and a colour of their own, the message
/// beside it. System lines are amber and hostile ones red. A new line slides in from the bottom
/// while the oldest fades out at the top.
/// </summary>
public sealed class CommsWidget : HudWidget
{
    private const string Name = "comms";
    private const float Pitch = 0.44f;
    private const float SpeakerColumn = 2.1f;

    // The last part of each wait is the slide
    private const float SlideShare = 0.25f;

    // Speakers keep their colour in every scheme, as people do in a chat
    private static readonly Color[] Voices =
    [
        new(120, 210, 255), new(140, 240, 160), new(200, 160, 255), new(255, 220, 130), new(255, 160, 200),
    ];

    private readonly ShipState _ship;
    private readonly Dictionary<string, Color> _voices = [];

    public CommsWidget(ShipState ship)
    {
        _ship = ship;

        // Each speaker takes the next voice, in the order they first speak
        foreach (var line in ship.Comms)
        {
            if (line.Kind == CommsKind.Friendly) _voices.TryAdd(line.Speaker, Voices[_voices.Count % Voices.Length]);
        }
    }

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var content = canvas.Panel(bounds, Name, "PROX COMMS", "CH 04");
        var lines = (int)(content.Height / Pitch);
        var first = (int)_ship.CommsPosition;
        var wait = _ship.CommsPosition - first;
        var slide = Easing.SmoothStep(MathUtil.Clamp((wait - (1f - SlideShare)) / SlideShare, 0f, 1f));

        // One line more than fits: the incoming one, under the last
        for (var i = 0; i <= lines; i++)
        {
            var line = _ship.Comms[(first + i) % _ship.Comms.Length];
            var y = content.Top - (i + 0.5f - slide) * Pitch;

            // Older lines are dimmer, the top one fades out and the incoming one fades in
            var brightness = 0.55f + 0.45f * (i - slide) / lines;
            var alpha = i == 0 ? brightness * (1f - slide) : i == lines ? slide : brightness;

            canvas.Text(new(Name, "speaker", i), line.Speaker, new Vector2(content.Left, y), HudText.Caption.Left, SpeakerColour(line).WithAlpha(alpha));
            canvas.Text(new(Name, "message", i), line.Text, new Vector2(content.Left + SpeakerColumn, y), HudText.Body, MessageColour(canvas.Theme, line).WithAlpha(alpha));
        }
    }

    private Color SpeakerColour(CommsLine line) => line.Kind switch
    {
        CommsKind.System => HudTheme.Caution,
        CommsKind.Hostile => HudTheme.Warning,
        _ => _voices[line.Speaker],
    };

    private static Color MessageColour(HudTheme theme, CommsLine line) => line.Kind switch
    {
        CommsKind.System => HudTheme.Caution,
        CommsKind.Hostile => Color.Lerp(HudTheme.Warning, Color.White, 0.35f),
        _ => theme.Text,
    };
}