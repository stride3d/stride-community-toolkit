using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// A single on-screen block of debug text, assembled from sections contributed by anything that has
/// something to say, with one position and one toggle key for the lot.
/// </summary>
/// <remarks>
/// <para>
/// This is a game system rather than a script, so it is unaffected by scenes being swapped and draws
/// itself once per frame with no help from the caller. Get one with
/// <see cref="GetOrCreate(IGame)"/> - it is registered as a service, so every caller shares the same
/// instance and the camera controller, your own instructions and any dropdowns end up in one place.
/// </para>
/// <para>
/// Contributors add a <see cref="DebugOverlaySection"/> whose callback runs each frame, so content
/// that changes needs no pushing. Sections are separated by a blank line and sorted by
/// <see cref="DebugOverlaySection.Order"/>.
/// </para>
/// <para>
/// Keep text to printable ASCII: the debug text renderer replaces anything outside the range 32 to
/// 126 with a space.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var overlay = DebugOverlay.GetOrCreate(game);
///
/// overlay.AddSection("Stress pile", () =>
/// [
///     new($"{bodies.Count:N0} bodies", Color.LightGreen),
///     new("SPACE - spawn more", Color.Yellow),
/// ]);
/// </code>
/// </example>
public sealed class DebugOverlay : GameSystemBase
{
    private readonly List<DebugOverlaySection> _sections = [];

    private Profiling.DebugTextSystem? _debugText;
    private InputManager? _input;
    private IGraphicsDeviceService? _graphicsDeviceService;

    /// <summary>
    /// Initializes a new overlay. Prefer <see cref="GetOrCreate(IGame)"/>, which shares one instance.
    /// </summary>
    /// <param name="registry">The service registry to resolve the debug text system and input from.</param>
    public DebugOverlay(IServiceRegistry registry) : base(registry)
    {
        Enabled = true;
        Visible = true;
    }

    /// <summary>
    /// Gets or sets where the overlay is drawn. <see cref="DisplayPosition.None"/> draws nothing.
    /// </summary>
    public DisplayPosition Position { get; set; } = DisplayPosition.TopRight;

    /// <summary>
    /// Gets or sets the pixel position used when <see cref="Position"/> is
    /// <see cref="DisplayPosition.Custom"/>.
    /// </summary>
    public Int2 CustomPosition { get; set; }

    /// <summary>
    /// Gets or sets the key that shows and hides the whole overlay. Defaults to <see cref="Keys.F4"/>.
    /// </summary>
    /// <remarks>
    /// This is the blunt instrument, for a clean screenshot. Prefer collapsing individual sections -
    /// a collapsed section leaves a line saying which key brings it back, whereas hiding everything
    /// leaves no clue that there was anything to see. <see cref="Keys.F2"/> is deliberately left to
    /// the camera controllers, whose help is what most callers actually want out of the way.
    /// </remarks>
    public Keys ToggleKey { get; set; } = Keys.F4;

    /// <summary>
    /// Gets or sets the key that moves the overlay to the next corner. Defaults to <see cref="Keys.F3"/>.
    /// </summary>
    /// <remarks>
    /// Does nothing while <see cref="Position"/> is <see cref="DisplayPosition.Custom"/>, which is an
    /// explicit choice by the caller and not something a keypress should silently override.
    /// </remarks>
    public Keys RepositionKey { get; set; } = Keys.F3;

    /// <summary>Gets or sets the vertical distance between lines, in pixels.</summary>
    public int LineHeight { get; set; } = 20;

    /// <summary>
    /// Gets or sets the assumed width of one character, in pixels, used to right-align the overlay.
    /// </summary>
    public int CharacterWidth { get; set; } = 8;

    /// <summary>Gets or sets the gap kept between the overlay and the edge of the screen, in pixels.</summary>
    public Int2 Margin { get; set; } = new(5, 10);

    /// <summary>Gets or sets the marker shown on a collapsed section's title line.</summary>
    /// <remarks>Printable ASCII only; arrow glyphs such as <c>▼</c> render as blanks.</remarks>
    public string CollapsedMarker { get; set; } = "[+]";

    /// <summary>Gets or sets the marker shown on an expanded section's title line.</summary>
    /// <inheritdoc cref="CollapsedMarker" path="/remarks"/>
    public string ExpandedMarker { get; set; } = "[-]";

    /// <summary>Gets or sets the colour used for section title lines.</summary>
    public Color? TitleColor { get; set; }

    /// <summary>Gets the sections currently registered, in insertion order.</summary>
    public IReadOnlyList<DebugOverlaySection> Sections => _sections;

    /// <summary>
    /// Returns the overlay registered with the game, creating and registering one if there is none.
    /// </summary>
    /// <param name="game">The game to attach to.</param>
    /// <returns>The shared overlay.</returns>
    public static DebugOverlay GetOrCreate(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.Services.GetService<DebugOverlay>() is { } existing) return existing;

        var overlay = new DebugOverlay(game.Services);

        game.Services.AddService(overlay);
        game.GameSystems.Add(overlay);

        return overlay;
    }

    /// <summary>
    /// Adds a section to the overlay.
    /// </summary>
    /// <param name="name">A name for the section, used to find it again. Not displayed.</param>
    /// <param name="lines">Produces the section's lines. Called every frame the overlay is drawn.</param>
    /// <param name="order">Sort order; lower is drawn first.</param>
    /// <returns>The section, so it can be disabled or removed later.</returns>
    public DebugOverlaySection AddSection(string name, Func<IReadOnlyList<TextElement>> lines, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(lines);

        var section = new DebugOverlaySection { Name = name, Lines = lines, Order = order };

        _sections.Add(section);

        return section;
    }

    /// <summary>
    /// Adds a section that can be collapsed to a single title line and expanded again with a key.
    /// </summary>
    /// <param name="name">A name for the section, used to find it again. Not displayed.</param>
    /// <param name="title">The heading, shown above the lines and on its own while collapsed.</param>
    /// <param name="toggleKey">The key that collapses and expands the section.</param>
    /// <param name="lines">Produces the section's lines. Called every frame it is drawn expanded.</param>
    /// <param name="collapsed">Whether it starts collapsed.</param>
    /// <param name="order">Sort order; lower is drawn first.</param>
    /// <returns>The section, so it can be collapsed, disabled or removed later.</returns>
    public DebugOverlaySection AddCollapsibleSection(
        string name,
        string title,
        Keys toggleKey,
        Func<IReadOnlyList<TextElement>> lines,
        bool collapsed = false,
        int order = 0)
    {
        var section = AddSection(name, lines, order);

        section.Title = title;
        section.ToggleKey = toggleKey;
        section.Collapsed = collapsed;

        return section;
    }

    /// <summary>Removes a section previously added with <see cref="AddSection"/>.</summary>
    /// <param name="section">The section to remove.</param>
    /// <returns><see langword="true"/> if it was present.</returns>
    public bool RemoveSection(DebugOverlaySection section) => _sections.Remove(section);

    /// <summary>Finds a section by name, or <see langword="null"/> if there is none.</summary>
    /// <param name="name">The name given when the section was added.</param>
    /// <returns>The section, if found.</returns>
    public DebugOverlaySection? FindSection(string name)
        => _sections.FirstOrDefault(section => section.Name == name);

    /// <summary>
    /// Moves the overlay to the next corner, skipping <see cref="DisplayPosition.None"/> and
    /// <see cref="DisplayPosition.Custom"/>.
    /// </summary>
    public void CyclePosition() => Position = Position switch
    {
        DisplayPosition.TopLeft => DisplayPosition.TopRight,
        DisplayPosition.TopRight => DisplayPosition.BottomRight,
        DisplayPosition.BottomRight => DisplayPosition.BottomLeft,
        _ => DisplayPosition.TopLeft,
    };

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        _input ??= Services.GetService<InputManager>();

        if (_input is null || !_input.HasKeyboard) return;

        if (_input.IsKeyPressed(ToggleKey)) Visible = !Visible;

        if (_input.IsKeyPressed(RepositionKey) && Position != DisplayPosition.Custom) CyclePosition();

        // Section keys are read even while the overlay is hidden, so a collapse toggle pressed with
        // everything off still takes effect rather than silently doing nothing
        foreach (var section in _sections)
        {
            if (section.IsCollapsible && _input.IsKeyPressed(section.ToggleKey!.Value))
            {
                section.Collapsed = !section.Collapsed;
            }
        }
    }

    /// <inheritdoc />
    public override void Draw(GameTime gameTime)
    {
        if (Position == DisplayPosition.None || _sections.Count == 0) return;

        _debugText ??= Services.GetService<Profiling.DebugTextSystem>();

        if (_debugText is null) return;

        var lines = CollectLines();

        if (lines.Count == 0) return;

        var origin = GetOrigin(lines);
        var y = origin.Y;

        foreach (var line in lines)
        {
            // Blank entries exist to space sections apart; printing them would be wasted work
            if (line.Text.Length > 0) _debugText.Print(line.Text, new Int2(origin.X, y), line.Color);

            y += LineHeight;
        }
    }

    /// <summary>
    /// Produces a readable name for a key, so <see cref="Keys.D2"/> shows as "2" rather than "D2".
    /// </summary>
    private static string DescribeKey(Keys key) => key switch
    {
        >= Keys.D0 and <= Keys.D9 => ((char)('0' + (key - Keys.D0))).ToString(),
        >= Keys.NumPad0 and <= Keys.NumPad9 => ((char)('0' + (key - Keys.NumPad0))).ToString(),
        _ => key.ToString()
    };

    private List<TextElement> CollectLines()
    {
        var lines = new List<TextElement>();

        foreach (var section in _sections.OrderBy(section => section.Order))
        {
            if (!section.Enabled) continue;

            var collapsible = section.IsCollapsible;

            // A collapsed section still costs its title line. That is the whole point: hiding content
            // outright leaves no clue it exists, or which key brings it back
            var sectionLines = collapsible && section.Collapsed ? [] : section.Lines();

            if (sectionLines.Count == 0 && !collapsible) continue;

            if (lines.Count > 0) lines.Add(new(string.Empty));

            if (collapsible)
            {
                var marker = section.Collapsed ? CollapsedMarker : ExpandedMarker;

                lines.Add(new($"{DescribeKey(section.ToggleKey!.Value)} - {section.Title} {marker}", TitleColor));
            }
            else if (!string.IsNullOrEmpty(section.Title))
            {
                lines.Add(new(section.Title, TitleColor));
            }

            lines.AddRange(sectionLines);
        }

        return lines;
    }

    private Int2 GetOrigin(List<TextElement> lines)
    {
        if (Position == DisplayPosition.Custom) return CustomPosition;

        _graphicsDeviceService ??= Services.GetService<IGraphicsDeviceService>();

        var backBuffer = _graphicsDeviceService?.GraphicsDevice?.Presenter?.BackBuffer;

        var screen = backBuffer is null
            ? new Int2(1280, 720)
            : new Int2(backBuffer.Width, backBuffer.Height);

        // Measured rather than declared, so a section appearing or a dropdown expanding keeps the
        // block anchored to its corner instead of running off the edge
        var width = lines.Max(line => line.Text.Length) * CharacterWidth;
        var height = lines.Count * LineHeight;

        var right = Math.Max(Margin.X, screen.X - width - Margin.X);
        var bottom = Math.Max(Margin.Y, screen.Y - height - Margin.Y);

        return Position switch
        {
            DisplayPosition.TopLeft => new(Margin.X, Margin.Y),
            DisplayPosition.BottomLeft => new(Margin.X, bottom),
            DisplayPosition.BottomRight => new(right, bottom),
            _ => new(right, Margin.Y),
        };
    }
}
