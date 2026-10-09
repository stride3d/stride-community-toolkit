using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Input;

namespace Stride.CommunityToolkit.Scripts.Utilities;

/// <summary>
/// A single on-screen block of debug text, assembled from sections contributed by anything that has something to say, with one position and one toggle key for the lot.
/// </summary>
/// <remarks>
///
/// <para>This is a game system rather than a script, so it is unaffected by scenes being swapped and draws itself once per frame with no help from the caller. Get one with <see cref="GetOrCreate(IGame)"/> - it is registered as a service, so every caller shares the same instance and the camera controller, your own instructions and any dropdowns end up in one place. </para>
///
/// <para>Contributors add a <see cref="DebugOverlaySection"/> whose callback runs each frame, so content that changes needs no pushing. Sections are separated by a blank line and sorted by <see cref="DebugOverlaySection.Order"/>. </para>
///
/// <para>
/// Text is drawn with an installed font chosen by <see cref="FontFamily"/> - monospace by default, like
/// Stride's own debug text - rasterised at <see cref="FontSize"/> times <see cref="Scale"/>, so it stays
/// sharp on high-DPI displays. Each line gets a <see cref="BackgroundColor"/> strip behind it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var overlay = DebugOverlay.GetOrCreate(game);
///
/// overlay.AddSection("Stress pile", () =>
/// [
///     new($"{bodies.Count:N0} bodies", Color.LightGreen),
///     new("Space", "Spawn more", Color.Yellow),
/// ]);
/// </code>
/// </example>
public sealed class DebugOverlay : GameSystemBase
{
    private readonly List<DebugOverlaySection> _sections = [];

    private SpriteBatch? _spriteBatch;
    private readonly DebugOverlayFontResolver _fontResolver = new();
    private Texture? _background;
    private InputManager? _input;
    private IGraphicsDeviceService? _graphicsDeviceService;
    private DisplayScale? _displayScale;

    // The widest the block has been since its line count last changed, in unscaled pixels
    private float _stickyWidth;
    private int _stickyLineCount;

    /// <summary>
    /// Gets or sets a font to draw with, overriding <see cref="FontName"/>. <see langword="null"/>, the default, uses the system font named by <see cref="FontName"/>.
    /// </summary>
    public SpriteFont? Font { get; set; }

    /// <summary>
    /// Gets or sets which kind of installed font to draw with when <see cref="FontName"/> is not set. Defaults to <see cref="DebugOverlayFontFamily.Monospace"/>, the character of Stride's own debug text.
    /// </summary>
    /// <remarks>
    /// The font file is located in the system font folders and rasterised at the size asked for, so it stays sharp at any <see cref="FontSize"/> and <see cref="Scale"/>. If none of the family's fonts is installed, Stride's default font is used - which is bold and proportional.
    /// </remarks>
    public DebugOverlayFontFamily FontFamily { get; set; } = DebugOverlayFontFamily.Monospace;

    /// <summary>
    /// Gets or sets the family name of a specific installed font to draw with, such as <c>"Consolas"</c> or <c>"Segoe UI"</c>, overriding <see cref="FontFamily"/>. <see langword="null"/>, the default, chooses from <see cref="FontFamily"/>. A font that cannot be found falls back the same way.
    /// </summary>
    /// <remarks>
    /// Set <see cref="FontFile"/> to point at a specific file instead of searching the system font folders.
    /// </remarks>
    public string? FontName { get; set; }

    /// <summary>
    /// Gets or sets the weight and slant of <see cref="FontName"/>. Defaults to <see cref="FontStyle.Regular"/>.
    /// </summary>
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;

    /// <summary>
    /// Gets or sets the path of the TrueType file for <see cref="FontName"/>, for fonts that are not in the system font folders. <see langword="null"/>, the default, searches those folders.
    /// </summary>
    public string? FontFile { get; set; }

    /// <summary>
    /// Gets or sets the text height in unscaled pixels. Defaults to 14.
    /// </summary>
    public float FontSize { get; set; } = 14f;

    /// <summary>
    /// Gets or sets the colour of the strip drawn behind each line of text, exactly as wide as the text. Defaults to black at 49% alpha, which is the look Stride's own debug text has; <see cref="Color.Transparent"/> draws no strips.
    /// </summary>
    public Color BackgroundColor { get; set; } = new(0, 0, 0, 125);

    /// <summary>
    /// Gets or sets how far each background strip extends beyond its text, in unscaled pixels. Defaults to 3 by 1.
    /// </summary>
    public Vector2 BackgroundPadding { get; set; } = new(3f, 1f);

    /// <summary>
    /// Gets or sets how much the whole overlay is enlarged on top of the display's own scale: text, line spacing, margins and padding. <c>1</c>, the default, is the size of Stride's debug text on a 100% display; <c>2</c> doubles everything. Any positive value works, since the font is rasterised at the resulting size rather than stretched. Values below a small minimum are treated as that minimum.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="FontSize"/>, <see cref="LineHeight"/>, <see cref="Margin"/>, <see cref="CustomPosition"/> and <see cref="BackgroundPadding"/> are in unscaled pixels and are multiplied by this, so the block keeps its corner and its layout at any scale.
    /// </para>
    /// <para>
    /// This multiplies the <see cref="DisplayScale"/> while <see cref="AutoScale"/> is on, so it is a preference - "a bit bigger" - rather than a DPI figure. To pin the overlay to an exact size regardless of the display, turn <see cref="AutoScale"/> off.
    /// </para>
    /// </remarks>
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether the overlay follows the display's scale, so it is the same size to the eye on a 150% laptop as on a 100% monitor. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Scale"/> multiplies on top. The factor comes from <see cref="DisplayScale"/> and is re-read when the window moves to another monitor. Turn it off to draw at exactly <see cref="Scale"/>, for example for a screenshot at a known size or when the game sets its own UI scale through <see cref="DisplayScale.Override"/>.
    /// </remarks>
    public bool AutoScale { get; set; } = true;

    /// <summary>
    /// Gets or sets the colour of lines that do not specify one. Defaults to <see cref="Color.LightGreen"/>, the same as Stride's own debug text.
    /// </summary>
    public Color DefaultTextColor { get; set; } = Color.LightGreen;

    /// <summary>
    /// Initializes a new overlay. Prefer <see cref="GetOrCreate(IGame)"/>, which shares one instance.
    /// </summary>
    /// <param name="registry">The service registry to resolve input and the graphics device from.</param>
    public DebugOverlay(IServiceRegistry registry) : base(registry)
    {
        Enabled = true;
        Visible = true;

        // Help goes over everything drawn in the frame, the immediate debug shapes included (they draw
        // at 0xffffff); only the screenshot capture, at int.MaxValue, comes after, so it sees the text
        DrawOrder = int.MaxValue - 1;
    }

    /// <summary>
    /// Gets or sets where the overlay is drawn. <see cref="DisplayPosition.None"/> draws nothing.
    /// </summary>
    public DisplayPosition Position { get; set; } = DisplayPosition.TopRight;

    /// <summary>
    /// Gets or sets the pixel position used when <see cref="Position"/> is <see cref="DisplayPosition.Custom"/>, in unscaled pixels from the top left. Setting this alone changes nothing while <see cref="Position"/> is a corner; <see cref="SetPosition(Int2)"/> sets both.
    /// </summary>
    public Int2 CustomPosition { get; set; }

    /// <summary>
    /// Gets or sets the key that shows and hides the whole overlay. Defaults to <see cref="Keys.F4"/>.
    /// </summary>
    /// <remarks>
    /// This is the blunt instrument, for a clean screenshot. Prefer collapsing individual sections - a collapsed section leaves a line saying which key brings it back, whereas hiding everything leaves no clue that there was anything to see. <see cref="Keys.F2"/> is deliberately left to the camera controllers, whose help is what most callers actually want out of the way.
    /// </remarks>
    public Keys ToggleKey { get; set; } = Keys.F4;

    /// <summary>
    /// Gets or sets the key that moves the overlay to the next corner. Defaults to <see cref="Keys.F3"/>.
    /// </summary>
    /// <remarks>
    /// Does nothing while <see cref="Position"/> is <see cref="DisplayPosition.Custom"/>, which is an explicit choice by the caller and not something a keypress should silently override.
    /// </remarks>
    public Keys RepositionKey { get; set; } = Keys.F3;

    /// <summary>
    /// Gets or sets a fixed vertical distance between lines, in unscaled pixels. <see langword="null"/>, the default, derives it from the font: the text height plus the background padding plus <see cref="LineSpacing"/>, so lines neither overlap nor drift apart when <see cref="FontSize"/> changes.
    /// </summary>
    public int? LineHeight { get; set; }

    /// <summary>
    /// Gets or sets the gap between one line's background strip and the next, in unscaled pixels, when <see cref="LineHeight"/> is not set. Defaults to 2; <c>0</c> makes the strips touch.
    /// </summary>
    public float LineSpacing { get; set; } = 2f;

    /// <summary>Gets or sets how many blank lines separate one section from the next - the empty line under the camera controls, for one. Defaults to 1; <c>0</c> runs a section straight on from the one above it.</summary>
    public int SectionGap { get; set; } = 1;

    /// <summary>
    /// Gets or sets how far the text is shifted inside its background strip, in screen pixels, positive downwards. Defaults to <c>-1</c>: a font's line gap sits above its ascender, so glyphs land about a pixel low in a strip sized from the line height, and one pixel up centres them. Not multiplied by the scale: the imbalance stays close to one pixel at the sizes the overlay is drawn at.
    /// </summary>
    public float TextNudge { get; set; } = -1f;

    /// <summary>
    /// Gets or sets the assumed width of one character, in pixels, used to right-align the overlay.
    /// </summary>
    [Obsolete("Text is measured with the font since the overlay draws with a SpriteFont; this value is no longer used.")]
    public int CharacterWidth { get; set; } = 8;

    /// <summary>Gets or sets the gap kept between the overlay and the edge of the screen, in pixels.</summary>
    public Int2 Margin { get; set; } = new(5, 10);

    /// <summary>Gets or sets the marker shown on a collapsed section's title line.</summary>
    /// <remarks>Printable ASCII only; arrow glyphs such as <c>▼</c> render as blanks.</remarks>
    public string CollapsedMarker { get; set; } = "[+]";

    /// <summary>Gets or sets the marker shown on an expanded section's title line.</summary>
    public string ExpandedMarker { get; set; } = "[-]";

    /// <summary>
    /// Gets or sets how a key named by a <see cref="TextElement"/> is decorated when drawn: a composite format
    /// with the key at <c>{0}</c>. Defaults to <c>[{0}]</c>, so <c>H</c> reads <c>[H]</c>; <c>{0}:</c> reads
    /// <c>H:</c>. One place to restyle every help line in every example.
    /// </summary>
    public string KeyFormat { get; set; } = "[{0}]";

    /// <summary>Gets or sets what separates the keys of a line that names several, as in <c>[Q] [E]</c>. Defaults to one space.</summary>
    public string KeySeparator { get; set; } = " ";

    /// <summary>
    /// Gets or sets the colour of markers and keys. <see langword="null"/>, the default, draws them in the
    /// line's own colour blended halfway to white, so they stand a shade apart from the text without leaving
    /// its palette.
    /// </summary>
    public Color? KeyColor { get; set; }

    /// <summary>Gets or sets the colour of section title lines. <see langword="null"/>, the default, uses <see cref="DefaultTextColor"/>.</summary>
    public Color? TitleColor { get; set; }

    /// <summary>
    /// Gets the rectangle the block was last drawn in, in screen pixels, background strips included. Empty until the first draw, and one frame behind whatever the sections say now.
    /// </summary>
    /// <remarks>
    /// For laying a scene out around the overlay: the overlay is pixels and a scene is world units, so a panel that must sit under the help block asks for this rather than guessing how tall twelve lines are on this display.
    /// </remarks>
    public RectangleF BlockBounds { get; private set; }

    /// <summary>Gets the sections currently registered, in insertion order.</summary>
    public IReadOnlyList<DebugOverlaySection> Sections => _sections;

    /// <summary>
    /// Decorates keys the way this overlay draws them: each through <see cref="KeyFormat"/>, joined by
    /// <see cref="KeySeparator"/>. <c>["Q", "E"]</c> gives <c>[Q] [E]</c> with the defaults.
    /// </summary>
    /// <param name="keys">The keys, in order.</param>
    /// <returns>The decorated keys, or an empty string for none.</returns>
    public string FormatKeys(IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return string.Join(KeySeparator, keys.Select(key => string.Format(KeyFormat, key)));
    }

    /// <summary>What is drawn before a line's text, in the key colour: its marker, then its decorated keys, then a space.</summary>
    private string Prefix(TextElement line)
    {
        var parts = new List<string>(2);

        if (!string.IsNullOrEmpty(line.Marker)) parts.Add(line.Marker);
        if (line.Keys is { Count: > 0 } keys) parts.Add(FormatKeys(keys));

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts) + (line.Text.Length > 0 ? " " : string.Empty);
    }

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
    /// Moves the overlay to the next corner, skipping <see cref="DisplayPosition.None"/> and <see cref="DisplayPosition.Custom"/>.
    /// </summary>
    public void CyclePosition() => Position = Position switch
    {
        DisplayPosition.TopLeft => DisplayPosition.TopRight,
        DisplayPosition.TopRight => DisplayPosition.BottomRight,
        DisplayPosition.BottomRight => DisplayPosition.BottomLeft,
        _ => DisplayPosition.TopLeft,
    };

    /// <summary>
    /// Places the overlay at a pixel position instead of a corner: sets <see cref="CustomPosition"/> and switches <see cref="Position"/> to <see cref="DisplayPosition.Custom"/> in one call, so the block moves at once rather than after a second assignment.
    /// </summary>
    /// <param name="position">The top-left corner of the block, in unscaled pixels from the top left of the window; multiplied by <see cref="Scale"/> and the display's scale when drawn.</param>
    /// <remarks>
    /// The <see cref="RepositionKey"/> is ignored from then on, because a position chosen by the caller is not something a keypress should silently override. Set <see cref="Position"/> back to a corner to hand it back.
    /// </remarks>
    public void SetPosition(Int2 position)
    {
        CustomPosition = position;
        Position = DisplayPosition.Custom;
    }

    /// <summary>
    /// Places the overlay at a pixel position instead of a corner. See <see cref="SetPosition(Int2)"/>.
    /// </summary>
    /// <param name="x">Pixels from the left edge of the window, unscaled.</param>
    /// <param name="y">Pixels from the top edge of the window, unscaled.</param>
    public void SetPosition(int x, int y) => SetPosition(new Int2(x, y));

    /// <summary>
    /// Places the overlay in a corner, or hides it with <see cref="DisplayPosition.None"/>. The same as setting <see cref="Position"/>; here so a corner and a pixel position are chosen through one method.
    /// </summary>
    /// <param name="position">The corner, <see cref="DisplayPosition.None"/> to draw nothing, or <see cref="DisplayPosition.Custom"/> to draw at <see cref="CustomPosition"/> as last set.</param>
    public void SetPosition(DisplayPosition position) => Position = position;

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

        var graphicsContext = Game?.GraphicsContext;
        var backBuffer = Game?.GraphicsDevice?.Presenter?.BackBuffer;
        var content = Content;

        if (graphicsContext is null || backBuffer is null || content is null) return;

        var lines = CollectLines();

        if (lines.Count == 0) return;

        var device = graphicsContext.CommandList.GraphicsDevice;

        // Stride's DebugTextSystem draws an 8 by 16 pixel bitmap font at a fixed size with a grey strip
        // baked into every glyph. Drawing with a real font through the sprite batch instead is what makes
        // Scale, FontSize and BackgroundColor possible, and keeps the text sharp at any size.
        var font = _fontResolver.Resolve(this, content, Services);
        _spriteBatch ??= new SpriteBatch(device);
        _background ??= ScreenTextDrawer.CreateBackgroundTexture(device);

        var scale = EffectiveScale;
        var block = MeasureBlock(lines, font, scale);
        var padding = new Vector2(MathF.Round(BackgroundPadding.X * scale), MathF.Round(BackgroundPadding.Y * scale));
        var linePitch = LinePitch(block.Sizes, padding, scale);
        var origin = GetOrigin(lines.Count * linePitch / scale, block.Width / scale);

        BlockBounds = new RectangleF(origin.X * scale - padding.X, origin.Y * scale - padding.Y, block.Width + padding.X * 2f, lines.Count * linePitch);

        var style = new ScreenTextStyle
        {
            Font = font,
            FontSize = FontSize * scale,
            Color = DefaultTextColor,
            Anchor = TextAnchor.TopLeft,
            Scale = 1f,
            Opacity = 1f,
            EnableBackground = false,
            BackgroundColor = BackgroundColor.ToColor4(),
            Padding = padding,
            TextOffset = new Vector2(0f, MathF.Round(TextNudge)),
        };

        graphicsContext.CommandList.SetRenderTargetAndViewport(null, backBuffer);

        _spriteBatch.Begin(graphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        // The origin is in unscaled pixels; at a 150% display scale it would land between two rows
        var y = MathF.Round(origin.Y * scale);
        var left = MathF.Round(origin.X * scale);

        for (var i = 0; i < lines.Count; i++)
        {
            var position = new Vector2(left + (lines[i].Indented ? block.IndentWidth : 0f), y);

            DrawLine(_spriteBatch, _background, lines[i], block.Prefixes[i], position, block.Sizes[i], style);

            y += linePitch;
        }

        _spriteBatch.End();
    }

    /// <summary>What the lines measure at the current font and scale, in screen pixels.</summary>
    /// <param name="Sizes">The size of each line's text, in whole pixels. Zero for a blank line.</param>
    /// <param name="Prefixes">The marker and keys each line starts with.</param>
    /// <param name="IndentWidth">How far a line under a collapsible title is moved in.</param>
    /// <param name="Width">The width of the block.</param>
    private readonly record struct BlockMeasure(Vector2[] Sizes, string[] Prefixes, float IndentWidth, float Width);

    /// <summary>
    /// Measures every line. Measured rather than declared, so a section appearing or a dropdown
    /// expanding keeps the block anchored to its corner instead of running off the edge.
    /// </summary>
    private BlockMeasure MeasureBlock(List<TextElement> lines, SpriteFont font, float scale)
    {
        var fontSize = FontSize * scale;
        var sizes = new Vector2[lines.Count];
        var prefixes = new string[lines.Count];
        var blockWidth = 0f;

        // Lines under a collapsible title start one marker in, so their keys sit under the title's key
        var indentWidth = MathF.Ceiling(font.MeasureString(CollapsedMarker + " ", fontSize).X);

        for (var i = 0; i < lines.Count; i++)
        {
            prefixes[i] = Prefix(lines[i]);

            var text = prefixes[i] + lines[i].Text;

            if (text.Length == 0) continue;

            // Whole pixels: a strip 18.7 pixels tall would end on a half pixel, and which row that half
            // pixel fills depends on where the block is anchored, so the gap under the text would
            // differ by a pixel between a top and a bottom corner
            var measured = font.MeasureString(text, fontSize);

            sizes[i] = new Vector2(MathF.Ceiling(measured.X), MathF.Ceiling(measured.Y));
            blockWidth = Math.Max(blockWidth, sizes[i].X + (lines[i].Indented ? indentWidth : 0f));
        }

        // A live value gaining or losing a digit would otherwise move a right-anchored block every frame
        // the camera flies. The block only grows, and forgets its width when a section opens or closes.
        if (lines.Count != _stickyLineCount)
        {
            _stickyLineCount = lines.Count;
            _stickyWidth = 0f;
        }

        _stickyWidth = Math.Max(_stickyWidth, blockWidth / scale);

        return new BlockMeasure(sizes, prefixes, indentWidth, _stickyWidth * scale);
    }

    /// <summary>Line pitch in screen pixels: fixed if asked for, otherwise what the font and strips need.</summary>
    private float LinePitch(Vector2[] sizes, Vector2 padding, float scale)
    {
        if (LineHeight is { } fixedHeight) return MathF.Ceiling(fixedHeight * scale);

        var textHeight = 0f;

        foreach (var size in sizes)
            textHeight = Math.Max(textHeight, size.Y);

        return MathF.Ceiling(textHeight + padding.Y * 2f + LineSpacing * scale);
    }

    /// <summary>
    /// Draws one line: one strip under the whole line, then the text in up to two runs, the marker
    /// and keys in the key colour, and what they do in the line's own, starting where the keys end.
    /// </summary>
    private void DrawLine(SpriteBatch batch, Texture background, TextElement line, string prefix, Vector2 position, Vector2 size, ScreenTextStyle style)
    {
        // Blank entries exist to space sections apart; drawing them would be wasted work
        if (prefix.Length + line.Text.Length == 0) return;

        var colour = line.Color ?? DefaultTextColor;

        style = style with { Color = colour };

        if (BackgroundColor.A > 0) ScreenTextDrawer.DrawBackground(batch, background, position, size, style);

        if (prefix.Length > 0)
        {
            var keyColour = KeyColor ?? Color.Lerp(colour, Color.White, 0.5f);

            ScreenTextDrawer.Draw(batch, null, prefix, position, size, style with { Color = keyColour });
            position.X += MathF.Round(style.Font.MeasureString(prefix, style.FontSize).X);
        }

        if (line.Text.Length > 0) ScreenTextDrawer.Draw(batch, null, line.Text, position, size, style);
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _background?.Dispose();
        _background = null;
        _fontResolver.Dispose();

        base.Destroy();
    }

    /// <summary><see cref="Scale"/> times the display's scale when <see cref="AutoScale"/> is on, with nonsense values clamped away.</summary>
    private float EffectiveScale
    {
        get
        {
            var display = AutoScale && Game is { } game ? (_displayScale ??= DisplayScale.GetOrCreate(game)).Value : 1f;

            return Math.Max(0.25f, Scale * display);
        }
    }

    /// <summary>The lines to draw, top down: section titles built, and the body of a collapsible section marked indented.</summary>
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

            if (lines.Count > 0)
            {
                for (var gap = 0; gap < SectionGap; gap++) lines.Add(new(string.Empty));
            }

            if (collapsible)
            {
                // "[+] [F2] Camera controls": the marker first, so every dropdown lines up, then the key,
                // decorated like the section's own key lines
                lines.Add(new(KeyNames.Describe(section.ToggleKey!.Value), section.Title!, TitleColor)
                {
                    Marker = section.Collapsed ? CollapsedMarker : ExpandedMarker,
                });
            }
            else if (!string.IsNullOrEmpty(section.Title))
            {
                lines.Add(new(section.Title, TitleColor));
            }

            // Under a "[+] [F2]" title the body starts where the key does, one marker in
            foreach (var line in sectionLines)
            {
                lines.Add(collapsible && !line.Indented ? line with { Indented = true } : line);
            }
        }

        return lines;
    }

    private Int2 GetOrigin(float blockHeight, float blockWidth)
    {
        if (Position == DisplayPosition.Custom) return CustomPosition;

        _graphicsDeviceService ??= Services.GetService<IGraphicsDeviceService>();

        var backBuffer = _graphicsDeviceService?.GraphicsDevice?.Presenter?.BackBuffer;

        // In unscaled pixels: everything here is multiplied by Scale when drawn
        var scale = EffectiveScale;
        var screen = backBuffer is null
            ? new Int2((int)(1280 / scale), (int)(720 / scale))
            : new Int2((int)(backBuffer.Width / scale), (int)(backBuffer.Height / scale));

        // Measured rather than declared, so a section appearing or a dropdown expanding keeps the
        // block anchored to its corner instead of running off the edge
        var width = (int)MathF.Ceiling(blockWidth);
        var height = (int)MathF.Ceiling(blockHeight);

        // The margin applies to the background box, so the text sits a padding further in
        var padding = BackgroundColor.A > 0 ? BackgroundPadding : Vector2.Zero;
        var left = Margin.X + (int)MathF.Ceiling(padding.X);
        var top = Margin.Y + (int)MathF.Ceiling(padding.Y);
        var right = Math.Max(left, screen.X - width - left);
        var bottom = Math.Max(top, screen.Y - height - top);

        return Position switch
        {
            DisplayPosition.TopLeft => new(left, top),
            DisplayPosition.BottomLeft => new(left, bottom),
            DisplayPosition.BottomRight => new(right, bottom),
            _ => new(right, top),
        };
    }
}