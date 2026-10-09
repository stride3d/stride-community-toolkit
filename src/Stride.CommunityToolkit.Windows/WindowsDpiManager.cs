using System.Diagnostics;

namespace Stride.CommunityToolkit.Windows;

/// <summary>
/// Declares the process DPI aware from code, and reports whether it is - the two things about DPI
/// that are Windows-only.
/// </summary>
/// <remarks>
/// <para>
/// Windows stretches the window of a process that has not declared itself DPI aware, which on a
/// 150% display means a blurred, upscaled image. The declaration usually lives in an
/// <c>app.manifest</c> referenced from the project, as Stride's templates do;
/// <see cref="EnablePerMonitorV2"/> is the same declaration made from code, for file-based apps and
/// anything else with no project file to hold a manifest. Either way it has to happen before the
/// window exists.
/// </para>
/// <para>
/// Reading the display's scale is not Windows-only and does not live here: see
/// <c>DisplayScale</c> in the core package, which the debug overlay follows by default.
/// </para>
/// </remarks>
public static class WindowsDpiManager
{
    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);

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

    /// <summary>
    /// Enables Per-Monitor-V2 DPI awareness for the current process (Windows 10+). Falls back to Per-Monitor if V2 is unavailable.
    /// This method is a best-effort call and does not throw on unsupported platforms or failures.
    /// </summary>
    /// <remarks>
    /// Call it before the game is created - Windows refuses the change once the process has a window.
    /// It is also refused when an <c>app.manifest</c> has already made a declaration, and the manifest's
    /// setting then stands - if that manifest asks only for <c>PerMonitor</c>, the process stays on the
    /// older mode rather than getting V2. Use one route, not both. Off Windows it does nothing: there is
    /// nothing to declare there.
    /// </remarks>
    public static void EnablePerMonitorV2()
    {
        if (!OperatingSystem.IsWindows()) return;

        // Per-Monitor-V2 is the Windows 10 API and the one worth having. Where it is unavailable the
        // older per-monitor call is still better than leaving the process DPI-unaware.
        if (TrySetPerMonitorV2Context()) return;

        FallBackToPerMonitorAwareness();
    }

    /// <summary>
    /// Asks for Per-Monitor-V2 awareness through the Windows 10 context API.
    /// </summary>
    /// <returns><c>true</c> when the process is now Per-Monitor-V2 aware.</returns>
    private static bool TrySetPerMonitorV2Context()
    {
        try
        {
            return NativeMethods.SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WindowsDpiManager.EnablePerMonitorV2 primary attempt failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Falls back to the older per-monitor awareness call, for Windows versions without the context API.
    /// </summary>
    private static void FallBackToPerMonitorAwareness()
    {
        try
        {
            // 0=Unaware, 1=System, 2=PerMonitor. Returns an HRESULT; E_ACCESSDENIED (0x80070005) means the
            // awareness was already fixed by a manifest or an earlier call, so a failure is only worth a debug line.
            var result = NativeMethods.SetProcessDpiAwareness(2);
            if (result < 0)
            {
                Debug.WriteLine($"WindowsDpiManager.EnablePerMonitorV2 fallback returned HRESULT 0x{result:X8}.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WindowsDpiManager.EnablePerMonitorV2 fallback attempt failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current process DPI awareness level - the way to check that a manifest or
    /// <see cref="EnablePerMonitorV2"/> actually took effect.
    /// </summary>
    /// <returns>The current <see cref="ProcessDpiAwareness"/> value when available; otherwise <c>null</c>.
    /// </returns>
    public static ProcessDpiAwareness? GetProcessDpiAwareness()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var proc = Process.GetCurrentProcess().Handle;
            if (NativeMethods.GetProcessDpiAwareness(proc, out var awareness) == 0)
            {
                return awareness switch
                {
                    NativeMethods.NativeProcessDpiAwareness.Process_DPI_Unaware => ProcessDpiAwareness.Unaware,
                    NativeMethods.NativeProcessDpiAwareness.Process_System_DPI_Aware => ProcessDpiAwareness.System,
                    NativeMethods.NativeProcessDpiAwareness.Process_Per_Monitor_DPI_Aware => ProcessDpiAwareness.PerMonitor,
                    _ => null
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WindowsDpiManager.GetProcessDpiAwareness failed: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Logs the process DPI awareness to the console, for checking that the declaration took.
    /// </summary>
    /// <param name="prefix">Optional message prefix.</param>
    public static void LogDpiInfo(string prefix = "")
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine($"{prefix}DPI diagnostics: not Windows");
            return;
        }

        var awareness = GetProcessDpiAwareness();

        Console.WriteLine($"{prefix}Process DPI awareness: {awareness?.ToString() ?? "Unknown"}");
    }
}