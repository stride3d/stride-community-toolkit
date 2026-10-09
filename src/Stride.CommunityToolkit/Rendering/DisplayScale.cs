using Stride.Core.Diagnostics;
using Stride.Games;
using SdlWindow = Stride.Graphics.SDL.Window;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// The factor anything measured in pixels should be multiplied by to look the size it was designed
/// at on the display the game is on: 1 on a 100% display, 1.5 on a 150% one, 2 on a Retina screen.
/// </summary>
/// <remarks>
/// <para>
/// Scale by this, not by the window size: a 4K laptop at 200% needs 16-pixel text drawn at 32 to
/// look the same, while a 4K monitor at 100% does not. Only the DPI tells the two apart.
/// </para>
/// <para>
/// It applies to what is measured in pixels: a debug overlay's font, a screen-space label, a shape
/// outline that is "3 pixels wide". Anything measured in world units already scales with the view
/// and needs nothing.
/// </para>
/// <para>
/// <b>Where the number comes from.</b> Two sources, and the larger wins. <see cref="GameWindow.ScaleFactor"/>
/// is drawable pixels per window unit: 1 on Windows and X11, 2 on a Retina display or a scaled
/// Wayland desktop, where the window is measured in points and the backbuffer is finer. On Windows
/// the backbuffer matches the window and the whole difference is the operating system's scale
/// setting, which SDL reports as the display DPI over 96; it follows the process's DPI awareness, so
/// an unaware process - one Windows is already stretching - correctly reads 1. Stride draws sprites
/// in backbuffer pixels either way, so one number serves both cases.
/// </para>
/// <para>
/// Verified on Windows. On X11 with a scale set through <c>Xft.dpi</c> or <c>GDK_SCALE</c> nothing
/// here can see it - SDL's DPI there is the panel's physical density, not the setting - so the value
/// stays 1; supply a <see cref="Source"/> that reads the setting if that matters to you.
/// </para>
/// <para>
/// The value is re-read when the window changes size, which also happens when it moves to a
/// monitor with a different scale, and every 60 frames. <see cref="Changed"/>
/// fires when it differs; consumers holding a rasterised font at the old size rebuild it then.
/// </para>
/// <para>
/// <see cref="Override"/> is the user's setting and always wins: a game's own UI-scale option, or a
/// developer who wants the overlay small. <see cref="Source"/> replaces the detection itself, for a
/// platform where the built-in query is wrong or a better one exists.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var scale = DisplayScale.GetOrCreate(game);
///
/// borderWidth = 3f * scale.Value;
/// scale.Changed += (_, _) => RebuildFontAtlas(scale.Value);
/// </code>
/// </example>
public sealed class DisplayScale : GameSystemBase
{
    private static readonly Logger _log = GlobalLogger.GetLogger(nameof(DisplayScale));

    /// <summary>The smallest value ever reported, so a broken query cannot make everything vanish.</summary>
    public static readonly float MinScale = 0.25f;

    private const int PollFrames = 60;

    private float _detected = 1f;
    private float? _override;
    private float _value = 1f;
    private int _framesSincePoll;
    private bool _subscribed;

    /// <summary>
    /// Initializes a new instance. Prefer <see cref="GetOrCreate(IGame)"/>, which shares one instance.
    /// </summary>
    /// <param name="registry">The service registry the game is running in.</param>
    public DisplayScale(IServiceRegistry registry) : base(registry)
    {
        Enabled = true;
    }

    /// <summary>
    /// Returns the display scale registered with the game, creating and registering one if there is
    /// none, so every consumer reads the same number and reacts to the same change.
    /// </summary>
    /// <param name="game">The game to attach to.</param>
    /// <returns>The shared instance.</returns>
    public static DisplayScale GetOrCreate(IGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.Services.GetService<DisplayScale>() is { } existing) return existing;

        var scale = new DisplayScale(game.Services);

        game.Services.AddService(scale);
        game.GameSystems.Add(scale);

        return scale;
    }

    /// <summary>
    /// Gets the factor to multiply pixel sizes by: <see cref="Override"/> if set, otherwise
    /// <see cref="Detected"/>, never below <see cref="MinScale"/>.
    /// </summary>
    public float Value => _value;

    /// <summary>
    /// Gets the factor the display reports, ignoring any <see cref="Override"/>. 1 until the window
    /// exists and has been queried.
    /// </summary>
    public float Detected => _detected;

    /// <summary>
    /// Gets or sets a value that replaces detection altogether: the user's own UI-scale setting, or a
    /// fixed size for a screenshot. <see langword="null"/>, the default, uses <see cref="Detected"/>.
    /// </summary>
    public float? Override
    {
        get => _override;
        set
        {
            _override = value;
            Recompute();
        }
    }

    /// <summary>
    /// Gets or sets a replacement for the built-in detection, given the window and returning the
    /// factor, or <see langword="null"/> to fall back to the built-in query. For a platform where
    /// the SDL answer is wrong, or a better source - a Windows DPI helper, a settings file.
    /// </summary>
    public Func<GameWindow, float?>? Source { get; set; }

    /// <summary>
    /// Raised after <see cref="Value"/> changes - the window moved to a differently scaled monitor,
    /// or <see cref="Override"/> was set. Consumers that rasterise at the scale rebuild here.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Re-reads the display now rather than waiting for the next window change or poll.
    /// </summary>
    public void Refresh()
    {
        var window = Game?.Window;

        if (window is null) return;

        var detected = Source?.Invoke(window) ?? Detect(window);

        _framesSincePoll = 0;

        if (MathF.Abs(detected - _detected) < 0.001f) return;

        _detected = detected;

        Recompute();
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        if (!_subscribed && Game?.Window is { } window)
        {
            // A monitor change arrives as a resize, so this is the timely signal; the poll below is
            // the safety net for the cases that do not
            window.ClientSizeChanged += (_, _) => Refresh();
            _subscribed = true;

            Refresh();

            return;
        }

        if (++_framesSincePoll >= PollFrames)
        {
            Refresh();
        }
    }

    /// <summary>
    /// The built-in query: the larger of Stride's drawable-per-window-unit factor and the SDL
    /// display DPI over 96, for the reasons given on the class.
    /// </summary>
    /// <param name="window">The window to measure for.</param>
    /// <returns>The factor, or 1 when nothing could be read.</returns>
    public static float Detect(GameWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var factor = window.ScaleFactor;

        if (!float.IsFinite(factor) || factor <= 0f) factor = 1f;

        // SDL's display DPI is the user's scale setting only on Windows, where SDL asks the operating
        // system for it. On X11 and macOS it is the panel's physical density - a 27-inch 4K monitor at
        // 100% reads about 1.7 - which is not what anyone set, so it is not consulted there.
        var dpiScale = OperatingSystem.IsWindows() ? QuerySdlDpiScale(window) : null;

        return MathF.Max(factor, dpiScale ?? 1f);
    }

    private static unsafe float? QuerySdlDpiScale(GameWindow window)
    {
        if (window.NativeWindow?.NativeWindow is not SdlWindow sdlWindow || sdlWindow.SdlHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var sdl = SdlWindow.SDL;
            var handle = (Silk.NET.SDL.Window*)sdlWindow.SdlHandle;
            var display = sdl.GetWindowDisplayIndex(handle);

            if (display < 0) return null;

            float diagonal = 0f, horizontal = 0f, vertical = 0f;

            if (sdl.GetDisplayDPI(display, ref diagonal, ref horizontal, ref vertical) != 0) return null;

            // Horizontal is what the operating system's scale setting drives; the diagonal figure
            // is derived from the physical size of the panel and is not what anyone set
            if (!float.IsFinite(horizontal) || horizontal <= 0f) return null;

            return horizontal / 96f;
        }
        catch (Exception exception)
        {
            _log.Warning($"The display DPI could not be read; assuming a 100% display. {exception.Message}");

            return null;
        }
    }

    private void Recompute()
    {
        var value = MathF.Max(MinScale, _override ?? _detected);

        if (MathF.Abs(value - _value) < 0.001f) return;

        _value = value;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}