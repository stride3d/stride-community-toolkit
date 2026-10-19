using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace Stride.CommunityToolkit.Rendering.Text;

/// <summary>
/// Loads fonts that are installed on the machine, so text can be drawn in something other than
/// Stride's default font without an asset pipeline or a font file shipped alongside the game.
/// </summary>
/// <remarks>
/// <para>
/// A code-only game has no compiled font assets, so Stride's default font - bold, proportional, and
/// the same in every such game - is the only one it gets for free. This finds a family's file in the
/// operating system's font folders, registers it with Stride's font system and rasterises it at the
/// size asked for, producing exactly what <see cref="WorldTextComponent.Font"/>,
/// <see cref="EntityTextComponent.Font"/> and the text overlay all accept.
/// </para>
/// <para>
/// Rasterised, not scaled: the font is sharp at whatever size it was loaded at, and a size well above
/// what you draw at costs glyph cache space for nothing.
/// </para>
/// <para>
/// The result belongs to the caller. Loading the same family and size twice returns two separate
/// fonts, so load each one once and keep it, and dispose it if the game outlives its use.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var font = game.LoadSystemFont("Segoe UI", 48);
///
/// entity.Add(new WorldTextComponent { Text = "Docking clamp", Font = font, FontSize = 48 });
/// </code>
/// </example>
public static class SystemFonts
{
    private static readonly Logger _log = GlobalLogger.GetLogger(nameof(SystemFonts));

    /// <summary>
    /// Proportional families to try, in order, on the current operating system: the platform's usual
    /// screen font first, then the metric-compatible families the other platforms ship, so something
    /// is nearly always found without any font being bundled.
    /// </summary>
    public static IReadOnlyList<string> SansSerifCandidates { get; } = OperatingSystem.IsWindows()
        ? ["Segoe UI", "Arial", "DejaVu Sans"]
        : OperatingSystem.IsMacOS()
            ? ["Helvetica", "Arial", "DejaVu Sans"]
            : ["Liberation Sans", "DejaVu Sans", "Arial"];

    /// <summary>
    /// Monospace families to try, in order, on the current operating system. The same idea as
    /// <see cref="SansSerifCandidates"/>, for the fixed-pitch look of console and debug text.
    /// </summary>
    public static IReadOnlyList<string> MonospaceCandidates { get; } = OperatingSystem.IsWindows()
        ? ["Consolas", "Cascadia Mono", "Courier New", "DejaVu Sans Mono"]
        : OperatingSystem.IsMacOS()
            ? ["Menlo", "Courier New", "DejaVu Sans Mono"]
            : ["DejaVu Sans Mono", "Liberation Mono", "Courier New"];

    /// <summary>
    /// Loads an installed font family, or returns <see langword="null"/> if it is not installed.
    /// </summary>
    /// <param name="services">The game's services, which must include Stride's font system.</param>
    /// <param name="family">The family name as the system knows it, such as <c>"Segoe UI"</c>.</param>
    /// <param name="size">The height the glyphs are rasterised at, in pixels.</param>
    /// <param name="style">The weight and slant wanted. Defaults to <see cref="FontStyle.Regular"/>.</param>
    /// <param name="fontFile">
    /// The TrueType file to register the family from, for fonts that are not in the system font
    /// folders. <see langword="null"/>, the default, searches those folders.
    /// </param>
    /// <returns>The font, or <see langword="null"/> if no file for the family was found.</returns>
    public static SpriteFont? Load(IServiceRegistry services, string family, float size, FontStyle style = FontStyle.Regular, string? fontFile = null)
        => LoadFirst(services, [family], size, style, fontFile);

    /// <summary>
    /// Loads the first of several families that is installed - the way to ask for "a monospace font"
    /// rather than one specific one, since no single family is present on every machine.
    /// </summary>
    /// <param name="services">The game's services, which must include Stride's font system.</param>
    /// <param name="families">Family names in order of preference, such as <see cref="MonospaceCandidates"/>.</param>
    /// <param name="size">The height the glyphs are rasterised at, in pixels.</param>
    /// <param name="style">The weight and slant wanted. Defaults to <see cref="FontStyle.Regular"/>.</param>
    /// <param name="fontFile">A TrueType file to register the first family from, bypassing the search.</param>
    /// <returns>The font, or <see langword="null"/> if none of the families was found.</returns>
    public static SpriteFont? LoadFirst(IServiceRegistry services, IEnumerable<string> families, float size, FontStyle style = FontStyle.Regular, string? fontFile = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(families);

        var fontSystem = services.GetService<FontSystem>();

        if (fontSystem is null)
        {
            _log.Warning("No FontSystem service is registered; no system font can be loaded.");

            return null;
        }

        try
        {
            foreach (var family in families)
            {
                var runtimeFonts = fontSystem.RuntimeFonts;

                if (!runtimeFonts.IsRegistered(family, style))
                {
                    var path = fontFile ?? FindFile(family, style);

                    if (path is null) continue;

                    runtimeFonts.RegisterFont(family, path, style);
                }

                return fontSystem.LoadRuntimeFont(family, size, style);
            }
        }
        catch (Exception exception)
        {
            _log.Warning($"A system font could not be loaded. {exception.Message}");
        }

        return null;
    }

    /// <summary>
    /// Looks for the file of a font family in the operating system's font folders, using the known
    /// file names of the common families and the usual naming conventions for the rest.
    /// </summary>
    /// <param name="family">The family name as the system knows it.</param>
    /// <param name="style">The weight and slant wanted.</param>
    /// <returns>The path of the font file, or <see langword="null"/> if none was found.</returns>
    public static string? FindFile(string family, FontStyle style = FontStyle.Regular)
    {
        ArgumentNullException.ThrowIfNull(family);

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
    /// File names (without extension) of the common families per style, for the ones whose files are
    /// not named after the family: Windows' classic eight-character names and macOS' collections.
    /// Anything else is tried under the usual conventions - "Family", "Family-Bold" and so on.
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
            3 => ["-BoldItalic", "BoldItalic", " Bold Italic", "bi", "z"],
            1 => ["-Bold", "Bold", " Bold", "bd", "b"],
            2 => ["-Italic", "Italic", " Italic", "i"],
            _ => new[] { string.Empty, "-Regular", "Regular", " Regular" },
        };

        foreach (var suffix in suffixes)
        {
            yield return compact + suffix;
            yield return family + suffix;
        }
    }
}