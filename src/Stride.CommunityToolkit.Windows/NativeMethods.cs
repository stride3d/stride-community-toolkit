using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Windows;

/// <summary>
/// The Win32 entry points used by this assembly, kept in one place as the .NET interop guidelines ask
/// (CA1060 / NDepend ND2401), and source-generated with <see cref="LibraryImportAttribute"/> so the
/// marshalling is compiled rather than emitted at run time (SYSLIB1054).
/// </summary>
internal static partial class NativeMethods
{
    // user32.dll

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetDC(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    internal static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    /// <summary>Win32 BOOL is a 4-byte int; without the marshalling hint the generator rejects <c>bool</c>.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    // gdi32.dll

    [LibraryImport("gdi32.dll")]
    internal static partial int GetDeviceCaps(IntPtr hdc, int nIndex);

    // shcore.dll

    /// <summary>2 = PROCESS_PER_MONITOR_DPI_AWARE.</summary>
    [LibraryImport("shcore.dll")]
    internal static partial int SetProcessDpiAwareness(int value);

    [LibraryImport("shcore.dll")]
    internal static partial int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [LibraryImport("shcore.dll")]
    internal static partial int GetProcessDpiAwareness(IntPtr hprocess, out NativeProcessDpiAwareness awareness);

    /// <summary>PROCESS_DPI_AWARENESS, as defined by shcore.h.</summary>
    internal enum NativeProcessDpiAwareness
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }

    /// <summary>Win32 POINT; blittable, so it can be passed by value through LibraryImport.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct POINT
    {
        public readonly int X;
        public readonly int Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }
}