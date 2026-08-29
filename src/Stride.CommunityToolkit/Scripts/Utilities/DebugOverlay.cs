using Stride.CommunityToolkit.Renderers;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
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
///     new("SPACE - spawn more", Color.Yellow),
/// ]);
/// </code>
/// </example>
public sealed class DebugOverlay : GameSystemBase
{
    private readonly List<DebugOverlaySection> _sections = [];

    private static readonly Logger Log = GlobalLogger.GetLogger(nameof(DebugOverlay));

    private SpriteBatch? _spriteBatch;
    private SpriteFont? _defaultFont;
    private SpriteFont? _systemFont;
    private string? _systemFontKey;
    private bool _systemFontFailed;
    private Texture? _background;
    private InputManager? _input;
    private IGraphicsDeviceService? _graphicsDeviceService;

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
    /// Gets or sets the text height in unscaled pixels. Defaults to 16, the size of Stride's debug text.
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
    /// Gets or sets how much the whole overlay is enlarged: text, line spacing, margins and padding. <c>1</c> is the size of Stride's debug text, which is small on a high-DPI display; <c>2</c> doubles everything. Any positive value works, since the font is rasterised at the resulting size rather than stretched. Values below a small minimum are treated as that minimum.
    /// </summary>
    /// <remarks>
    /// <see cref="FontSize"/>, <see cref="LineHeight"/>, <see cref="Margin"/>, <see cref="CustomPosition"/> and <see cref="BackgroundPadding"/> are in unscaled pixels and are multiplied by this, so the block keeps its corner and its layout at any scale.
    /// </remarks>
    public float Scale { get; set; } = 1f;

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
    }

    /// <summary>
    /// Gets or sets where the overlay is drawn. <see cref="DisplayPosition.None"/> draws nothing.
    /// </summary>
    public DisplayPosition Position { get; set; } = DisplayPosition.TopRight;

    /// <summary>
    /// Gets or sets the pixel position used when <see cref="Position"/> is <see cref="DisplayPosition.Custom"/>.
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
    /// Moves the overlay to the next corner, skipping <see cref="DisplayPosition.None"/> and <see cref="DisplayPosition.Custom"/>.
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
        var font = ResolveFont(content);
        _spriteBatch ??= new SpriteBatch(device);
        _background ??= ScreenTextDrawer.CreateBackgroundTexture(device);

        var scale = EffectiveScale;
        var fontSize = FontSize * scale;

        // Measured rather than declared, so a section appearing or a dropdown expanding keeps the block
        // anchored to its corner instead of running off the edge
        var sizes = new Vector2[lines.Count];
        var blockWidth = 0f;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Text.Length == 0) continue;

            sizes[i] = font.MeasureString(lines[i].Text, fontSize);
            blockWidth = Math.Max(blockWidth, sizes[i].X);
        }

        var padding = BackgroundPadding * scale;

        // Line pitch in screen pixels: fixed if asked for, otherwise what the font and strips need
        var textHeight = 0f;

        for (var i = 0; i < lines.Count; i++)
            textHeight = Math.Max(textHeight, sizes[i].Y);

        var linePitch = LineHeight is { } fixedHeight
            ? fixedHeight * scale
            : textHeight + padding.Y * 2f + LineSpacing * scale;

        var origin = GetOrigin(lines.Count * linePitch / scale, blockWidth / scale);
        var backgroundColor = BackgroundColor.ToColor4();
        var drawBackground = BackgroundColor.A > 0;

        graphicsContext.CommandList.SetRenderTargetAndViewport(null, backBuffer);

        _spriteBatch.Begin(graphicsContext,
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendStates.AlphaBlend,
            samplerState: null,
            depthStencilState: DepthStencilStates.None);

        var y = origin.Y * scale;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Blank entries exist to space sections apart; drawing them would be wasted work
            if (line.Text.Length > 0)
            {
                var style = new ScreenTextStyle
                {
                    Font = font,
                    FontSize = fontSize,
                    Color = line.Color ?? DefaultTextColor,
                    Anchor = TextAnchor.TopLeft,
                    Scale = 1f,
                    Opacity = 1f,
                    EnableBackground = drawBackground,
                    BackgroundColor = backgroundColor,
                    Padding = padding,
                };

                ScreenTextDrawer.Draw(_spriteBatch, _background, line.Text, new Vector2(origin.X * scale, y), sizes[i], style);
            }

            y += linePitch;
        }

        _spriteBatch.End();
    }

    /// <inheritdoc />
    protected override void Destroy()
    {
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        _background?.Dispose();
        _background = null;
        _systemFont?.Dispose();
        _systemFont = null;

        base.Destroy();
    }

    /// <summary><see cref="Scale"/> with nonsense values clamped away.</summary>
    private float EffectiveScale => Math.Max(0.25f, Scale);

    /// <summary>
    /// <see cref="Font"/> if set; otherwise an installed font - <see cref="FontName"/>, or the first of <see cref="FontFamily"/> 's candidates that is present - registered with Stride's font system from its file the first time it is needed; otherwise Stride's default font.
    /// </summary>
    private SpriteFont ResolveFont(IContentManager content)
    {
        if (Font != null) return Font;

        var key = $"{FontFamily}|{FontName}|{FontStyle}|{FontFile}";

        if (key != _systemFontKey)
        {
            _systemFont?.Dispose();
            _systemFont = null;
            _systemFontKey = key;
            _systemFontFailed = false;
        }

        if (_systemFont is null && !_systemFontFailed)
        {
            _systemFont = TryLoadSystemFont();
            _systemFontFailed = _systemFont is null;
        }

        return _systemFont ?? (_defaultFont ??= content.Load<SpriteFont>(RendererDefaults.DefaultFontPath));
    }

    private SpriteFont? TryLoadSystemFont()
    {
        var fontSystem = Services.GetService<FontSystem>();

        if (fontSystem is null)
        {
            Log.Warning("No FontSystem service; drawing with Stride's default font.");
            return null;
        }

        var style = FontStyle;
        var families = FontName is { Length: > 0 } explicitName ? [explicitName] : FamilyCandidates(FontFamily);

        try
        {
            foreach (var family in families)
            {
                var runtimeFonts = fontSystem.RuntimeFonts;

                if (!runtimeFonts.IsRegistered(family, style))
                {
                    var path = FontFile ?? FindSystemFontFile(family, style);

                    if (path is null) continue;

                    runtimeFonts.RegisterFont(family, path, style);
                }

                return fontSystem.LoadRuntimeFont(family, FontSize, style);
            }

            Log.Warning($"None of the fonts {string.Join(", ", families)} ({style}) were found in the system font folders; drawing with Stride's default font instead.");
        }
        catch (Exception exception)
        {
            Log.Warning($"The overlay font could not be loaded; drawing with Stride's default font instead. {exception.Message}");
        }

        return null;
    }

    /// <summary>
    /// The families tried, in order, for a <see cref="DebugOverlayFontFamily"/> on the current operating system - the platform's usual screen font first, then the metric-compatible families other platforms ship, so a font is nearly always found without any being bundled.
    /// </summary>
    private static string[] FamilyCandidates(DebugOverlayFontFamily family)
    {
        var monospace = family == DebugOverlayFontFamily.Monospace;

        if (OperatingSystem.IsWindows())
        {
            return monospace
                ? ["Consolas", "Cascadia Mono", "Courier New", "DejaVu Sans Mono"]
                : ["Segoe UI", "Arial", "DejaVu Sans"];
        }

        if (OperatingSystem.IsMacOS())
        {
            return monospace
                ? ["Menlo", "Courier New", "DejaVu Sans Mono"]
                : ["Helvetica", "Arial", "DejaVu Sans"];
        }

        return monospace
            ? ["DejaVu Sans Mono", "Liberation Mono", "Courier New"]
            : ["Liberation Sans", "DejaVu Sans", "Arial"];
    }

    /// <summary>
    /// Looks for the font file of a family in the operating system's font folders, using the known file names of the common families and the usual naming conventions for the rest.
    /// </summary>
    private static string? FindSystemFontFile(string family, FontStyle style)
    {
        var directories = SystemFontDirectories().Where(Directory.Exists).ToList();

        if (directories.Count == 0) return null;

        var wanted = new HashSet<string>(FileNameCandidates(family, style), StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            IEnumerable<string> files;

            try
            {
                files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);

                if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)
                    && !extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase))
                { continue; }

                if (wanted.Contains(Path.GetFileNameWithoutExtension(file)))
                    return file;
            }
        }

        return null;
    }

    private static IEnumerable<string> SystemFontDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/System/Library/Fonts";
            yield return "/Library/Fonts";
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Fonts");
        }
        else
        {
            yield return "/usr/share/fonts";
            yield return "/usr/local/share/fonts";
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "fonts");
        }
    }

    /// <summary>
    /// File names (without extension) of the common families per style, for the ones whose files are not named after the family: Windows' classic eight-character names and macOS' collections. Anything else is tried under the usual conventions - "Family", "Family-Bold", "FamilyBold" and so on.
    /// </summary>
    private static readonly Dictionary<string, string[]> KnownFontFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Regular, Bold, Italic, BoldItalic
        ["Consolas"] = ["consola", "consolab", "consolai", "consolaz"],
        ["Courier New"] = ["cour", "courbd", "couri", "courbi"],
        ["Arial"] = ["arial", "arialbd", "ariali", "arialbi"],
        ["Segoe UI"] = ["segoeui", "segoeuib", "segoeuii", "segoeuiz"],
        ["Times New Roman"] = ["times", "timesbd", "timesi", "timesbi"],
        ["Verdana"] = ["verdana", "verdanab", "verdanai", "verdanaz"],
        ["Tahoma"] = ["tahoma", "tahomabd", "tahoma", "tahomabd"],
        ["Cascadia Mono"] = ["CascadiaMono", "CascadiaMono", "CascadiaMonoItalic", "CascadiaMonoItalic"],
        ["Menlo"] = ["Menlo", "Menlo", "Menlo", "Menlo"],
        ["Helvetica"] = ["Helvetica", "Helvetica", "Helvetica", "Helvetica"],
        ["DejaVu Sans"] = ["DejaVuSans", "DejaVuSans-Bold", "DejaVuSans-Oblique", "DejaVuSans-BoldOblique"],
        ["DejaVu Sans Mono"] = ["DejaVuSansMono", "DejaVuSansMono-Bold", "DejaVuSansMono-Oblique", "DejaVuSansMono-BoldOblique"],
    };

    private static IEnumerable<string> FileNameCandidates(string family, FontStyle style)
    {
        var bold = (style & FontStyle.Bold) == FontStyle.Bold;
        var italic = (style & FontStyle.Italic) == FontStyle.Italic;
        var styleIndex = (bold ? 1 : 0) + (italic ? 2 : 0);

        if (KnownFontFiles.TryGetValue(family, out var known))
            yield return known[styleIndex];

        var compact = family.Replace(" ", string.Empty);

        var suffixes = styleIndex switch
        {
            3 => new[] { "-BoldItalic", "BoldItalic", " Bold Italic", "bi", "z" },
            1 => new[] { "-Bold", "Bold", " Bold", "bd", "b" },
            2 => new[] { "-Italic", "Italic", " Italic", "i" },
            _ => new[] { string.Empty, "-Regular", "Regular", " Regular" },
        };

        foreach (var suffix in suffixes)
        {
            yield return compact + suffix;
            yield return family + suffix;
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
