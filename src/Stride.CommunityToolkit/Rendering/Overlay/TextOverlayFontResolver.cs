using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace Stride.CommunityToolkit.Rendering.Overlay;

/// <summary>
/// Finds, registers and caches the font <see cref="TextOverlay"/> draws with: the overlay's explicit
/// <see cref="TextOverlay.Font"/> if set, otherwise an installed system font of the overlay's
/// <see cref="TextOverlay.FontFamily"/>, otherwise Stride's default font. Split out of <see cref="TextOverlay"/> so the
/// overlay owns the settings and this owns the lookup and the caching.
/// </summary>
internal sealed class TextOverlayFontResolver : IDisposable
{
    private static readonly Logger _log = GlobalLogger.GetLogger(nameof(TextOverlay));

    private SpriteFont? _defaultFont;
    private SpriteFont? _systemFont;
    private TextOverlayFontFamily? _systemFontFamily;
    private bool _systemFontFailed;

    /// <summary>
    /// <see cref="TextOverlay.Font"/> if set; otherwise an installed font, the first of <see cref="TextOverlay.FontFamily"/>'s candidates that is present, registered with Stride's font system from its file the first time it is needed; otherwise Stride's default font.
    /// </summary>
    internal SpriteFont Resolve(TextOverlay overlay, IContentManager content, IServiceRegistry services)
    {
        if (overlay.Font != null) return overlay.Font;

        if (overlay.FontFamily != _systemFontFamily)
        {
            _systemFont?.Dispose();
            _systemFont = null;
            _systemFontFamily = overlay.FontFamily;
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
        var families = overlay.FontFamily == TextOverlayFontFamily.Monospace ? SystemFonts.MonospaceCandidates : SystemFonts.SansSerifCandidates;
        var font = SystemFonts.LoadFirst(services, families, overlay.FontSize, FontStyle.Regular);

        if (font is null)
        {
            _log.Warning($"None of the fonts {string.Join(", ", families)} could be loaded; drawing with Stride's default font instead.");
        }

        return font;
    }
}