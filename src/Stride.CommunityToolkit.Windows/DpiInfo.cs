namespace Stride.CommunityToolkit.Windows;

/// <summary>
/// DPI result including the raw DPI values, the derived scale factor and whether a fallback was used.
/// </summary>
public readonly struct DpiInfo
{
    /// <summary>
    /// Horizontal DPI value (dots per inch).
    /// </summary>
    public uint DpiX { get; }

    /// <summary>
    /// Vertical DPI value (dots per inch).
    /// </summary>
    public uint DpiY { get; }

    /// <summary>
    /// Indicates whether the value was obtained via a fallback method (such as GDI) rather than modern monitor APIs.
    /// </summary>
    public bool IsFallback { get; }

    /// <summary>
    /// Derived scale factor based on a 96 DPI baseline. (e.g. 96 DPI -> 1.0f)
    /// </summary>
    public float Scale => DpiX / 96f;

    /// <summary>
    /// Initializes a new instance of the <see cref="DpiInfo"/> struct.
    /// </summary>
    /// <param name="dpiX">Horizontal DPI value.</param>
    /// <param name="dpiY">Vertical DPI value.</param>
    /// <param name="isFallback">True if values were obtained via fallback; otherwise false.</param>
    public DpiInfo(uint dpiX, uint dpiY, bool isFallback)
    {
        DpiX = dpiX;
        DpiY = dpiY;
        IsFallback = isFallback;
    }

    /// <summary>
    /// Returns a readable representation of the DPI information including scale and fallback flag.
    /// </summary>
    /// <returns>String describing the DPI values and scale.</returns>
    public override string ToString() => $"{DpiX}x{DpiY} (Scale {Scale:F2}x){(IsFallback ? " Fallback" : string.Empty)}";
}