using E13_SignalR_Shared;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.Core.Mathematics;
using Stride.Input;

namespace E13_SignalR.Station;

/// <summary>
/// The in-game console's state: the current scheme as Stride colours, the scheme menu, a short log
/// of what happened last, and a hail from the web that shows for a few seconds. What is drawn from
/// this - the boards, the banner, the overlay - is composed elsewhere; this is only the state.
/// </summary>
public sealed class StationConsole
{
    private const int LogLength = 6;
    private const float HailSeconds = 8f;

    private readonly DebugTextDropdown _menu;
    private readonly List<LogEntry> _log = [];

    private string? _hail;
    private float _hailFrom;
    private float _hailUntil;
    private float _time;

    public StationConsole()
    {
        _menu = new DebugTextDropdown
        {
            Title = "Scheme",
            ToggleKey = Keys.T,
            CloseOnSelect = false,
            SelectedIndex = 0,
            Items = [.. Schemes.All.Index().Select(pair => new DebugTextDropdownItem(
                Keys.D1 + pair.Index, pair.Item.Name, () => Select(pair.Item.Name)))],
        };

        Apply(Schemes.Default);
    }

    /// <summary>Raised when a scheme is selected from either console, so the choice can be reported to the other.</summary>
    public event Action<Scheme>? SchemeChanged;

    /// <summary>Raised when a hail arrives from the web, for the effects that mark the moment.</summary>
    public event Action<string>? Hailed;

    public Scheme Scheme { get; private set; } = Schemes.Default;

    public Color Accent { get; private set; }

    public Color Fill { get; private set; }

    public Color Text { get; private set; }

    public Color Glow { get; private set; }

    /// <summary>While the scheme list is open, the digit keys pick a scheme rather than a container size.</summary>
    public bool IsMenuOpen => _menu.IsOpen;

    /// <summary>The last few things that happened, oldest first.</summary>
    public IReadOnlyList<LogEntry> Log => _log;

    public string? Hail => _time < _hailUntil ? _hail : null;

    /// <summary>Seconds since the current hail arrived.</summary>
    public float HailAge => _time - _hailFrom;

    /// <summary>Seconds the current hail has left on screen.</summary>
    public float HailRemaining => MathF.Max(0f, _hailUntil - _time);

    /// <summary>
    /// Switches scheme by name and announces it. An unknown name is ignored: it came over the wire,
    /// and a console that changed to nothing would be worse than one that stayed put.
    /// </summary>
    public void Select(string name)
    {
        var scheme = Schemes.Find(name);

        if (scheme is null || scheme == Scheme) return;

        Apply(scheme);

        _menu.SelectedIndex = Schemes.All.IndexOf(scheme);

        SchemeChanged?.Invoke(scheme);
    }

    public void Note(string line, LogKind kind = LogKind.Info)
    {
        _log.Add(new LogEntry(line, kind));

        if (_log.Count > LogLength)
        {
            _log.RemoveAt(0);
        }
    }

    public void ShowHail(string text)
    {
        _hail = text;
        _hailFrom = _time;
        _hailUntil = _time + HailSeconds;

        Hailed?.Invoke(text);
    }

    public void Update(InputManager input, float time)
    {
        _time = time;

        _menu.Update(input);
    }

    public IReadOnlyList<TextElement> MenuLines() => _menu.GetLines();

    private void Apply(Scheme scheme)
    {
        Scheme = scheme;
        Accent = Hex.ToColor(scheme.Accent);
        Fill = Hex.ToColor(scheme.Fill);
        Text = Hex.ToColor(scheme.Text);
        Glow = Hex.ToColor(scheme.Glow);

        _menu.TitleColor = Accent;
        _menu.SelectedColor = Text;
    }
}