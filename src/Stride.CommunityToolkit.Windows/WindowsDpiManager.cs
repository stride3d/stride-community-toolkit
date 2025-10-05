using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Windows;

/// <summary>
/// Provides Windows DPI awareness configuration and diagnostic capabilities.
/// </summary>
public static class WindowsDpiManager
{
    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);

    [DllImport("User32.dll", ExactSpelling = true, SetLastError = false)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [DllImport("Shcore.dll")]
    private static extern int SetProcessDpiAwareness(int value); // 2 = PerMonitor

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("shcore.dll")]
    private static extern int GetProcessDpiAwareness(IntPtr hprocess, out NativeProcessDpiAwareness awareness);

    private enum NativeProcessDpiAwareness
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }

    /// <summary>
    /// Public representation of process DPI awareness values.
    /// </summary>
    public enum ProcessDpiAwareness
    {
        /// <summary>
        /// The process is DPI unaware.
        /// </summary>
        Unaware = 0,

        /// <summary>
        /// The process is system DPI aware.
        /// </summary>
        System = 1,

        /// <summary>
        /// The process is per-monitor DPI aware.
        /// </summary>
        PerMonitor = 2
    }

    private const int MDT_EFFECTIVE_DPI = 0;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct POINT
    {
        public readonly int X;
        public readonly int Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    /// <summary>
    /// Enables Per-Monitor-V2 DPI awareness for the current process (Windows 10+). Falls back to Per-Monitor if V2 is unavailable.
    /// This method is a best-effort call and does not throw on unsupported platforms or failures.
    /// </summary>
    public static void EnablePerMonitorV2()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            if (SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2))
                return;
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.EnablePerMonitorV2 primary attempt failed: {ex.Message}");
#endif
        }

        try
        {
            // 0=Unaware, 1=System, 2=PerMonitor
            SetProcessDpiAwareness(2);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.EnablePerMonitorV2 fallback attempt failed: {ex.Message}");
#endif
        }
    }

    /// <summary>
    /// Attempts to retrieve DPI for the specified window (or primary monitor if <paramref name="hwnd"/> is zero).
    /// </summary>
    /// <param name="hwnd">Window handle or <see cref="IntPtr.Zero"/> for primary monitor.</param>
    /// <param name="dpiX">Horizontal DPI when successful.</param>
    /// <param name="dpiY">Vertical DPI when successful.</param>
    /// <returns><c>true</c> if DPI could be retrieved (including fallback), otherwise <c>false</c>.</returns>
    public static bool TryGetDpi(IntPtr hwnd, out uint dpiX, out uint dpiY)
    {
        dpiX = dpiY = 0;
        if (!OperatingSystem.IsWindows()) return false;

        var info = hwnd == IntPtr.Zero ? InternalGetPrimaryDpi() : InternalGetWindowDpi(hwnd);
        if (info is null) return false;
        dpiX = info.Value.DpiX;
        dpiY = info.Value.DpiY;
        return true;
    }

    /// <summary>
    /// Gets DPI information for the monitor containing the specified window.
    /// </summary>
    /// <param name="windowHandle">A window handle.</param>
    /// <returns>DpiInfo if resolvable; otherwise <c>null</c>.</returns>
    public static DpiInfo? GetWindowDpi(IntPtr windowHandle) => !OperatingSystem.IsWindows() || windowHandle == IntPtr.Zero ? null : InternalGetWindowDpi(windowHandle);

    /// <summary>
    /// Gets DPI information for the primary monitor.
    /// </summary>
    /// <returns>DpiInfo if resolvable; otherwise <c>null</c>.</returns>
    public static DpiInfo? GetPrimaryDpi() => !OperatingSystem.IsWindows() ? null : InternalGetPrimaryDpi();

    /// <summary>
    /// Gets the scale factor (1.0 = 96 DPI) for the monitor containing the specified window.
    /// </summary>
    /// <param name="windowHandle">A window handle.</param>
    /// <returns>Scale value or <c>null</c> if DPI unavailable.</returns>
    public static float? GetWindowScale(IntPtr windowHandle) => GetWindowDpi(windowHandle)?.Scale;

    /// <summary>
    /// Gets the scale factor (1.0 = 96 DPI) for the primary monitor.
    /// </summary>
    /// <returns>Scale value or <c>null</c> if DPI unavailable.</returns>
    public static float? GetPrimaryScale() => GetPrimaryDpi()?.Scale;

    /// <summary>
    /// Gets the current process DPI awareness level.
    /// </summary>
    /// <returns>The current <see cref="ProcessDpiAwareness"/> value when available; otherwise <c>null</c>.
    /// </returns>
    public static ProcessDpiAwareness? GetProcessDpiAwareness()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var proc = Process.GetCurrentProcess().Handle;
            if (GetProcessDpiAwareness(proc, out var awareness) == 0)
            {
                return awareness switch
                {
                    NativeProcessDpiAwareness.Process_DPI_Unaware => ProcessDpiAwareness.Unaware,
                    NativeProcessDpiAwareness.Process_System_DPI_Aware => ProcessDpiAwareness.System,
                    NativeProcessDpiAwareness.Process_Per_Monitor_DPI_Aware => ProcessDpiAwareness.PerMonitor,
                    _ => null
                };
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.GetProcessDpiAwareness failed: {ex.Message}");
#endif
        }
        return null;
    }

    /// <summary>
    /// Logs DPI related information to the console. When a window handle is supplied, logs its monitor DPI; otherwise logs primary monitor DPI.
    /// </summary>
    /// <param name="prefix">Optional message prefix.</param>
    /// <param name="windowHandle">Optional window handle.</param>
    public static void LogDpiInfo(string prefix = "", IntPtr windowHandle = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine($"{prefix}DPI diagnostics: not Windows");
            return;
        }

        try
        {
            var dpi = windowHandle == IntPtr.Zero ? GetPrimaryDpi() : GetWindowDpi(windowHandle);
            if (dpi is null)
            {
                Console.WriteLine($"{prefix}DPI: unavailable");
            }
            else
            {
                Console.WriteLine($"{prefix}DPI: {dpi.Value}");
            }

            var procAwareness = GetProcessDpiAwareness();
            Console.WriteLine($"{prefix}Process DPI awareness: {procAwareness?.ToString() ?? "Unknown"}");
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.LogDpiInfo exception: {ex.Message}");
#endif
            Console.WriteLine($"{prefix}DPI diagnostics exception: {ex.Message}");
        }
    }

    private static DpiInfo? InternalGetPrimaryDpi()
    {
        try
        {
            var hMon = MonitorFromPoint(new POINT(0, 0), 2 /*MONITOR_DEFAULTTOPRIMARY*/);
            if (hMon == IntPtr.Zero) return null;
            if (GetDpiForMonitor(hMon, MDT_EFFECTIVE_DPI, out uint dx, out uint dy) == 0)
                return new DpiInfo(dx, dy, false);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.InternalGetPrimaryDpi failed: {ex.Message}");
#endif
        }
        // Fallback to GDI desktop DPI if possible (still flagged as fallback)
        var gdi = GraphicsDeviceContext.GetDesktopDpi();
        return gdi is null ? null : new DpiInfo(gdi.Value.dpiX, gdi.Value.dpiY, true);
    }

    private static DpiInfo? InternalGetWindowDpi(IntPtr windowHandle)
    {
        try
        {
            var hMon = MonitorFromWindow(windowHandle, 2 /*MONITOR_DEFAULTTONEAREST*/);
            if (hMon == IntPtr.Zero) return null;
            if (GetDpiForMonitor(hMon, MDT_EFFECTIVE_DPI, out uint dx, out uint dy) == 0)
                return new DpiInfo(dx, dy, false);
        }
        catch (Exception ex)
        {
#if DEBUG
            Debug.WriteLine($"WindowsDpiManager.InternalGetWindowDpi failed: {ex.Message}");
#endif
        }
        var gdi = GraphicsDeviceContext.GetDesktopDpi();
        return gdi is null ? null : new DpiInfo(gdi.Value.dpiX, gdi.Value.dpiY, true);
    }
}