using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>What a colour is for. A widget asks the theme for a role, never for a colour.</summary>
public enum HudRole
{
    /// <summary>Panel frames, headers and captions.</summary>
    Frame,

    /// <summary>Where the ship is and where it points: heading, pitch, tapes, radar.</summary>
    Navigation,

    /// <summary>Something is on or healthy: systems, gauges, engaged modes.</summary>
    Engaged,

    /// <summary>What the pilot chose: the target, a selection, the power setting.</summary>
    Commanded,

    /// <summary>Figures and text.</summary>
    Data,
}

/// <summary>
/// A colour scheme: a dark ground, one accent, a text colour and a glow. A single-colour scheme
/// answers every role with its accent. A scheme with <see cref="Roles"/> gives each role a colour of
/// its own. Caution and warning are the same in every scheme.
/// </summary>
public sealed record HudTheme(string Name, Color Accent, Color Ground, Color Text, Color Glow)
{
    public static readonly Color Caution = new(255, 176, 40);

    public static readonly Color Warning = new(255, 64, 56);

    /// <summary>The colour on a lit button or annunciator: dark, so it reads on the lamp.</summary>
    public static readonly Color Ink = new(10, 10, 14);

    /// <summary>The roles with a colour of their own. Empty in a single-colour scheme.</summary>
    public IReadOnlyDictionary<HudRole, Color> Roles { get; init; } = new Dictionary<HudRole, Color>();

    /// <summary>The ten schemes, in the order of their keys: 1 to 9, then 0.</summary>
    public static IReadOnlyList<HudTheme> All { get; } =
    [
        new("Blue", new Color(90, 190, 255), new Color(8, 22, 42), new Color(205, 238, 255), new Color(0, 140, 255)),
        new("Red", new Color(255, 95, 90), new Color(36, 10, 12), new Color(255, 210, 205), new Color(255, 40, 40)),
        new("Green", new Color(80, 245, 150), new Color(6, 30, 20), new Color(200, 255, 225), new Color(0, 220, 120)),
        new("Purple", new Color(185, 130, 255), new Color(24, 12, 44), new Color(230, 210, 255), new Color(150, 60, 255)),
        new("Orange", new Color(255, 170, 70), new Color(40, 20, 6), new Color(255, 228, 190), new Color(255, 130, 0)),
        new("Teal", new Color(60, 225, 210), new Color(4, 28, 30), new Color(200, 255, 248), new Color(0, 200, 190)),
        new("Magenta", new Color(255, 90, 200), new Color(36, 8, 30), new Color(255, 210, 240), new Color(255, 30, 170)),
        new("Gold", new Color(245, 205, 80), new Color(34, 26, 6), new Color(255, 242, 200), new Color(255, 180, 0)),
        new("Ice", new Color(215, 232, 245), new Color(14, 20, 28), new Color(255, 255, 255), new Color(150, 190, 230)),
        new("Flight deck", new Color(120, 160, 195), new Color(10, 16, 26), new Color(240, 245, 250), new Color(90, 150, 210))
        {
            Roles = new Dictionary<HudRole, Color>
            {
                [HudRole.Navigation] = new(70, 220, 255),
                [HudRole.Engaged] = new(90, 240, 130),
                [HudRole.Commanded] = new(255, 100, 230),
            },
        },
    ];

    /// <summary>The colour of a role.</summary>
    public Color For(HudRole role) => Roles.TryGetValue(role, out var colour) ? colour : role == HudRole.Data ? Text : Accent;

    /// <summary>The colour of a role at a third of its strength: tracks, idle frames, range rings.</summary>
    public Color Dim(HudRole role) => For(role).WithAlpha(0.35f);

    /// <summary>The colour a role glows in.</summary>
    public Color GlowFor(HudRole role) => Roles.TryGetValue(role, out var colour) ? colour : Glow;
}