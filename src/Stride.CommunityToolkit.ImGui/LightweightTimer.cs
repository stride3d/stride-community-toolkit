using System.Diagnostics;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// Represents a lightweight timer for measuring elapsed time with minimal overhead.
/// </summary>
/// <remarks>This struct provides methods to start, restart, and retrieve elapsed time. It is designed for
/// scenarios where performance is critical, such as in tight loops or high-frequency operations. Use <see
/// cref="StartNew"/> to create and start a new timer, <see cref="Start"/> to reset and start the timer, and <see
/// cref="Elapsed"/> to retrieve the elapsed time since the timer was started.</remarks>
public struct LightweightTimer
{
    private long _ts;

    /// <summary>
    /// Gets the initial timestamp when the timer was started.
    /// </summary>
    public TimeSpan InitTime => Core.Utilities.ConvertRawToTimestamp(_ts);

    /// <summary>
    /// Gets the elapsed time since the timer was started.
    /// </summary>
    public TimeSpan Elapsed => Core.Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - _ts);

    /// <summary>
    /// Starts or restarts the timer by recording the current timestamp.
    /// </summary>
    public void Start()
    {
        _ts = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Use this function and its return value when inside a loop instead of <see cref="Elapsed"/>
    /// as it guarantees that no time will be discarded
    /// </summary>
    public TimeSpan Restart()
    {
        long now = Stopwatch.GetTimestamp();
        var delta = Core.Utilities.ConvertRawToTimestamp(now - _ts);
        _ts = now;

        return delta;
    }

    /// <summary>
    /// Creates and starts a new instance of the <see cref="LightweightTimer"/> class.
    /// </summary>
    /// <returns>A <see cref="LightweightTimer"/> instance that has been started and is ready to measure elapsed time.</returns>
    public static LightweightTimer StartNew()
    {
        LightweightTimer lt = new();

        lt.Start();

        return lt;
    }
}