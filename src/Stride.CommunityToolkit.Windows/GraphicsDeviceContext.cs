using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Windows;

/// <summary>
/// Provides helper methods for retrieving graphics device context information using native Windows APIs.
/// </summary>
/// <remarks>This class is intended for scenarios where direct access to device context (DC) information is
/// required, such as obtaining screen DPI on Windows systems that do not support newer DPI APIs. All members are static
/// and thread-safe.</remarks>
public static class GraphicsDeviceContext
{
    [DllImport("user32.dll")] private static extern System.IntPtr GetDC(System.IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(System.IntPtr hWnd, System.IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(System.IntPtr hdc, int nIndex);

    private const int LOGPIXELSX = 88;
    private const int LOGPIXELSY = 90;

    /// <summary>
    /// Retrieves the desktop (screen) DPI using GDI functions as a fallback when modern APIs are unavailable.
    /// Returns <c>null</c> if a device context cannot be obtained or the retrieved values are not positive.
    /// </summary>
    /// <returns>Tuple with horizontal and vertical DPI or <c>null</c> on failure.</returns>
    public static (uint dpiX, uint dpiY)? GetDesktopDpi()
    {
        var hdc = GetDC(System.IntPtr.Zero);
        if (hdc == System.IntPtr.Zero) return null;
        try
        {
            int x = GetDeviceCaps(hdc, LOGPIXELSX);
            int y = GetDeviceCaps(hdc, LOGPIXELSY);
            if (x <= 0 || y <= 0) return null;
            return ((uint)x, (uint)y);
        }
        finally
        {
            ReleaseDC(System.IntPtr.Zero, hdc);
        }
    }
}