using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The Win32 entry points used by this assembly, kept in one place as the .NET interop guidelines ask
/// (CA1060 / NDepend ND2401), and source-generated with <see cref="LibraryImportAttribute"/> so the
/// marshalling is compiled rather than emitted at run time (SYSLIB1054).
/// </summary>
internal static partial class NativeMethods
{
    // user32.dll

    /// <summary>Win32 BOOL is a 4-byte int; without the marshalling hint the generator rejects <c>bool</c>.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    // shcore.dll

    /// <summary>2 = PROCESS_PER_MONITOR_DPI_AWARE.</summary>
    [LibraryImport("shcore.dll")]
    internal static partial int SetProcessDpiAwareness(int value);

    [LibraryImport("shcore.dll")]
    internal static partial int GetProcessDpiAwareness(IntPtr hprocess, out NativeProcessDpiAwareness awareness);

    /// <summary>PROCESS_DPI_AWARENESS, as defined by shcore.h.</summary>
    internal enum NativeProcessDpiAwareness
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }
}
