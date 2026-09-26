using E13_SignalR_Shared;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace E13_SignalR.Station;

/// <summary>
/// What the deck's events look like in the world: a tag on a container for its first seconds and
/// again as it lands, a ring spreading from where it came to rest, the hazard line flashing and a
/// label drifting off the edge when one is lost, and a banner over the pad when the web hails. All
/// short-lived, all driven by <see cref="Deck"/>'s events, none of it known to the deck.
/// </summary>
public sealed class DeckEffects
{
    private const float TagSeconds = 2f;
    private const float LandedTagSeconds = 1.8f;
    private const float RingSeconds = 0.7f;
    private const float LostSeconds = 1.6f;
    private const float HailRingSeconds = 1.2f;
    private const int DriftPool = 4;

    private static readonly Color LostColor = new(255, 95, 90);

    private readonly Labels _labels;
    private readonly List<Ring> _rings = [];
    private readonly List<Tag> _tags = [];
    private readonly List<Drift> _drifts = [];

    private int _nextDrift;
    private float _time;

    public DeckEffects(Labels labels, StationConsole console)
    {
        _labels = labels;

        labels.Add("hail-caption", 0.4f, labels.Bold, (t, c) => t.TextColor = c.Accent, glow: 3f, overlay: true);
        labels.Add("hail-text", 0.8f, labels.Bold, (t, c) => { t.TextColor = c.Text; t.GlowColor = c.Glow; }, glow: 6f, overlay: true);

        for (var i = 0; i < DriftPool; i++)
        {
            labels.Add($"lost-{i}", 0.5f, labels.Bold, (t, _) => { t.TextColor = LostColor; t.GlowColor = Hex.WithAlpha(LostColor, 200); }, glow: 4f, overlay: true);
        }
    }

    /// <summary>How brightly the hazard line is flashing, 1 at the moment of a loss and fading.</summary>
    public float HazardFlash { get; private set; }

    /// <summary>A screen-space tag on the falling container: its number and size, for a couple of seconds.</summary>
    public void OnReleased(Container container, StationConsole console)
    {
        var text = new EntityTextComponent
        {
            Text = $"#{container.Id} {container.Size.ToString().ToUpperInvariant()}",
            FontSize = 13,
            Anchor = TextAnchor.BottomCenter,
            Offset = new Vector2(0, -14),
            TextColor = console.Accent,
            EnableShadow = true,
            ShadowColor = new Color(0, 0, 0, 200),
        };

        container.Entity.Add(text);

        _tags.Add(new Tag(container.Id, container.Entity, text) { Until = _time + TagSeconds });
    }

    /// <summary>The tag turns into the air time, and a ring spreads from the landing point.</summary>
    public void OnLanded(ContainerEvent container, StationConsole console)
    {
        if (_tags.Find(tag => tag.Id == container.Id) is { } tag)
        {
            tag.Text.Text = $"#{container.Id} · {container.AirTime:0.0} s";
            tag.Text.TextColor = console.Text;
            tag.Until = _time + LandedTagSeconds;
        }

        if (container.Position is { } at)
        {
            _rings.Add(new Ring(new Vector3(at.X, StationScene.Lift, at.Z), 0.5f, 2.4f, RingSeconds, console.Accent));
        }
    }

    /// <summary>The hazard line flashes and the number drifts down over the edge it went over.</summary>
    public void OnLost(ContainerEvent container)
    {
        HazardFlash = 1f;

        var x = container.Position is { } at ? MathUtil.Clamp(at.X, -StationScene.DeckHalf + 1f, StationScene.DeckHalf - 1f) : 0f;

        _drifts.Add(new Drift($"lost-{_nextDrift++ % DriftPool}", $"LOST #{container.Id}", new Vector3(x, 0.8f, StationScene.DeckHalf + 0.8f)));
    }

    /// <summary>A ring rolls out across the pad when the web hails the deck.</summary>
    public void OnHail(StationConsole console)
        => _rings.Add(new Ring(new Vector3(0, StationScene.Lift, 0), 3f, 9.5f, HailRingSeconds, console.Glow));

    /// <summary>The containers are gone, and so are their tags.</summary>
    public void OnCleared() => _tags.Clear();

    public void Update(float deltaSeconds, float time)
    {
        _time = time;

        // The flash decays over two-thirds of a second, whatever the frame rate
        HazardFlash = MathF.Max(0f, HazardFlash - deltaSeconds * 1.5f);

        // Everything short-lived ages by the frame; what has outlived its span is dropped below
        foreach (var ring in _rings) ring.Age += deltaSeconds;
        foreach (var drift in _drifts) drift.Age += deltaSeconds;

        _rings.RemoveAll(ring => ring.Age >= ring.Life);

        // A drift borrows a pooled label, so it is hidden rather than removed
        foreach (var drift in _drifts.Where(drift => drift.Age >= LostSeconds))
        {
            _labels.Hide(drift.Key);
        }

        _drifts.RemoveAll(drift => drift.Age >= LostSeconds);

        // An expired tag comes off its container; a container already gone takes it along anyway
        foreach (var tag in _tags.Where(tag => _time >= tag.Until))
        {
            tag.Entity.Remove(tag.Text);
        }

        _tags.RemoveAll(tag => _time >= tag.Until);
    }

    public void Draw(ShapeBatch shapes, StationConsole console)
    {
        shapes.BorderWidth = 2f;
        shapes.Fill.Set(null, 0f);

        foreach (var ring in _rings)
        {
            var t = ring.Age / ring.Life;

            // Opacity is the fade: the ring goes out as it goes wide
            shapes.Opacity = 1f - t;
            shapes.Glow.Set(6f, Hex.WithAlpha(ring.Color, 120));
            shapes.Glow.Additive = true;
            shapes.DrawRing(ring.Center, Vector3.UnitY, MathUtil.Lerp(ring.From, ring.To, t), ring.Color);
            shapes.Glow.Clear();
        }

        if (HazardFlash > 0f)
        {
            var inset = StationScene.DeckHalf - StationScene.LipThickness - 0.3f;
            var z = StationScene.DeckHalf - 0.3f;

            shapes.Opacity = HazardFlash;
            shapes.Glow.Set(14f, Hex.WithAlpha(LostColor, 200));
            shapes.Glow.Additive = true;
            shapes.DrawPixelLine(new Vector3(-inset, StationScene.Lift + 0.01f, z), new Vector3(inset, StationScene.Lift + 0.01f, z), 3f, LostColor);
            shapes.Glow.Clear();
        }

        shapes.Opacity = 1f;

        foreach (var drift in _drifts)
        {
            var t = drift.Age / LostSeconds;

            _labels.Set(drift.Key, drift.Text, drift.Start - new Vector3(0, 2.4f * t, 0), Quaternion.Identity, opacity: 1f - t);
        }

        DrawHail(console);
    }

    private void DrawHail(StationConsole console)
    {
        if (console.Hail is not { } hail)
        {
            _labels.Hide("hail-caption");
            _labels.Hide("hail-text");

            return;
        }

        // In fast, out slow: a hail should land like a message and leave like one
        var fade = MathF.Min(1f, MathF.Min(console.HailAge / 0.35f, console.HailRemaining / 1.5f));

        _labels.Set("hail-caption", "HAIL FROM WEB", new Vector3(0, 7.4f, 0), Quaternion.Identity, opacity: fade);
        _labels.Set("hail-text", Wrap(hail, 34, 3), new Vector3(0, 6.3f, 0), Quaternion.Identity, opacity: fade);
    }

    /// <summary>Breaks a hail into lines at word boundaries, keeping at most a few of them.</summary>
    private static string Wrap(string text, int width, int maxLines)
    {
        var lines = new List<string>();
        var line = string.Empty;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line);
                line = string.Empty;

                if (lines.Count == maxLines) break;
            }

            line = line.Length == 0 ? word : $"{line} {word}";
        }

        if (lines.Count < maxLines && line.Length > 0) lines.Add(line);

        return string.Join('\n', lines);
    }

    // Explicit constructors rather than primary ones: a primary constructor is public, and these
    // types are private.
    private sealed class Ring
    {
        internal Ring(Vector3 center, float from, float to, float life, Color color)
        {
            Center = center;
            From = from;
            To = to;
            Life = life;
            Color = color;
        }

        internal Vector3 Center { get; }

        internal float From { get; }

        internal float To { get; }

        internal float Life { get; }

        internal Color Color { get; }

        internal float Age { get; set; }
    }

    private sealed class Tag
    {
        internal Tag(int id, Entity entity, EntityTextComponent text)
        {
            Id = id;
            Entity = entity;
            Text = text;
        }

        internal int Id { get; }

        internal Entity Entity { get; }

        internal EntityTextComponent Text { get; }

        internal float Until { get; set; }
    }

    private sealed class Drift
    {
        internal Drift(string key, string text, Vector3 start)
        {
            Key = key;
            Text = text;
            Start = start;
        }

        internal string Key { get; }

        internal string Text { get; }

        internal Vector3 Start { get; }

        internal float Age { get; set; }
    }
}