using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Rendering.Overlay;

/// <summary>
/// Finds, registers and caches the font <see cref="TextOverlay"/> draws with: the overlay's explicit
/// <see cref="TextOverlay.Font"/> if set, otherwise an installed system font matching the overlay's
/// font settings, otherwise Stride's default font. Split out of <see cref="TextOverlay"/> so the
/// overlay owns the settings and this owns the lookup and the caching.
/// </summary>
internal sealed class TextOverlayFontResolver : IDisposable
{
    private static readonly Logger _log = GlobalLogger.GetLogger(nameof(TextOverlay));

    private SpriteFont? _defaultFont;
    private SpriteFont? _systemFont;
    private string? _systemFontKey;
    private bool _systemFontFailed;

    /// <summary>
    /// <see cref="TextOverlay.Font"/> if set; otherwise an installed font - <see cref="TextOverlay.FontName"/>, or the first of <see cref="TextOverlay.FontFamily"/> 's candidates that is present - registered with Stride's font system from its file the first time it is needed; otherwise Stride's default font.
    /// </summary>
    internal SpriteFont Resolve(TextOverlay overlay, IContentManager content, IServiceRegistry services)
    {
        if (overlay.Font != null) return overlay.Font;

        var key = $"{overlay.FontFamily}|{overlay.FontName}|{overlay.FontStyle}|{overlay.FontFile}";

        if (key != _systemFontKey)
        {
            _systemFont?.Dispose();
            _systemFont = null;
            _systemFontKey = key;
            _systemFontFailed = false;
        }

        if (_systemFont is null && !_systemFontFailed)
        {
            _systemFont = LoadSystemFont(overlay, services);
            _systemFontFailed = _systemFont is null;
        }

        return _systemFont ?? (_defaultFont ??= content.Load<SpriteFont>(RendererDefaults.DefaultFontPath));
    }

    /// <summary>Releases the runtime font this resolver registered, if any. The default font belongs to the content manager and is left alone.</summary>
    public void Dispose()
    {
        _systemFont?.Dispose();
        _systemFont = null;
    }

    private static SpriteFont? LoadSystemFont(TextOverlay overlay, IServiceRegistry services)
    {
        var families = FamilyCandidates(overlay);
        var font = SystemFonts.LoadFirst(services, families, overlay.FontSize, overlay.FontStyle, overlay.FontFile);

        if (font is null)
        {
            _log.Warning($"None of the fonts {string.Join(", ", families)} ({overlay.FontStyle}) could be loaded; drawing with Stride's default font instead.");
        }

        return font;
    }

    /// <summary>
    /// The families tried, in order: the one the overlay names, or the platform's candidates for the kind of font it asks for.
    /// </summary>
    private static IReadOnlyList<string> FamilyCandidates(TextOverlay overlay)
    {
        if (overlay.FontName is { Length: > 0 } explicitName) return [explicitName];

        return overlay.FontFamily == TextOverlayFontFamily.Monospace
            ? SystemFonts.MonospaceCandidates
            : SystemFonts.SansSerifCandidates;
    }
}