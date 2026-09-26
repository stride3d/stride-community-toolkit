using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace E13_SignalR.Station;

/// <summary>
/// The world-text labels the boards and effects draw with. Each is one entity carrying a
/// <see cref="WorldTextComponent"/>, created once and then moved, re-worded and shown or hidden every
/// frame - the same shape as ShapeBatch's resubmit-per-frame, so a layout is written as one draw
/// routine rather than as state to keep in sync. Every label also remembers how the scheme colours
/// it, so a scheme change restyles them all in one call.
/// </summary>
public sealed class Labels
{
    private readonly Scene _scene;
    private readonly StationConsole _console;
    private readonly Dictionary<string, Label> _labels = [];
    private readonly List<(WorldTextComponent Text, Action<WorldTextComponent, StationConsole> Restyle)> _styled = [];

    public Labels(Scene scene, Game game, StationConsole console)
    {
        _scene = scene;
        _console = console;

        // System fonts where there are any - the default Stride font is a fallback, not a look
        Sans = SystemFonts.LoadFirst(game.Services, SystemFonts.SansSerifCandidates, 48);
        Bold = SystemFonts.LoadFirst(game.Services, SystemFonts.SansSerifCandidates, 48, FontStyle.Bold);
        Mono = SystemFonts.LoadFirst(game.Services, SystemFonts.MonospaceCandidates, 48);
    }

    public SpriteFont? Sans { get; }

    public SpriteFont? Bold { get; }

    public SpriteFont? Mono { get; }

    /// <summary>
    /// Creates a hidden label. <paramref name="restyle"/> is how the scheme colours it; it runs now
    /// and again on every scheme change. An overlay label faces the camera and draws over
    /// everything, for tags and banners that float in the scene rather than lie on a board.
    /// </summary>
    public WorldTextComponent Add(string key, float lineHeight, SpriteFont? font, Action<WorldTextComponent, StationConsole> restyle,
        TextAnchor anchor = TextAnchor.MiddleCenter, float glow = 0f, bool overlay = false)
    {
        var text = new WorldTextComponent
        {
            Text = string.Empty,
            Height = lineHeight,
            FontSize = 48,
            Font = font,
            Anchor = anchor,
            Alignment = TextAlignment.Center,
            GlowSize = glow,
            Billboard = overlay,
            DepthTest = !overlay,
            IsVisible = false,
        };

        restyle(text, _console);

        var entity = new Entity($"Label {key}") { text };

        entity.Scene = _scene;

        _labels[key] = new Label(entity, text, lineHeight);
        _styled.Add((text, restyle));

        return text;
    }

    /// <summary>
    /// Shows a label with the given string at a position and orientation. A colour or opacity
    /// given here overrides what the scheme set; leave them null to keep it.
    /// </summary>
    public void Set(string key, string text, Vector3 position, Quaternion rotation, Color? color = null, float? opacity = null)
    {
        var label = _labels[key];

        label.Text.IsVisible = true;

        if (label.Text.Text != text)
        {
            label.Text.Text = text;

            // Height is the whole block, so a second line must not halve the letters
            label.Text.Height = label.LineHeight * (text.Count(c => c == '\n') + 1);
        }

        if (color is { } c) label.Text.TextColor = c;
        if (opacity is { } o) label.Text.Opacity = o;

        label.Entity.Transform.Position = position;
        label.Entity.Transform.Rotation = rotation;
    }

    /// <summary>Shows a label on a board, at board coordinates.</summary>
    public void Set(string key, string text, Board board, float u, float v, Color? color = null, float? opacity = null)
        => Set(key, text, board.Place(u, v), board.Rotation, color, opacity);

    public void Hide(string key) => _labels[key].Text.IsVisible = false;

    /// <summary>Recolours every label for the current scheme.</summary>
    public void Restyle()
    {
        foreach (var (text, restyle) in _styled)
        {
            restyle(text, _console);
        }
    }

    private sealed record Label(Entity Entity, WorldTextComponent Text, float LineHeight);
}