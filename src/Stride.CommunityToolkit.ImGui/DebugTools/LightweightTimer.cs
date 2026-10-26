using System.Diagnostics;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// Represents a lightweight timer for measuring elapsed time with minimal overhead.
/// </summary>
/// <remarks>This struct provides methods to start, restart, and retrieve elapsed time. It is designed for
/// scenarios where performance is critical, such as in tight loops or high-frequency operations. Use <see
/// cref="StartNew"/> to create and start a new timer, <see cref="Restart"/> to read and restart it, and <see
/// cref="Elapsed"/> to retrieve the elapsed time since the timer was started.</remarks>
internal struct LightweightTimer
{
    private long _ts;

    /// <summary>
    /// Gets the initial timestamp when the timer was started.
    /// </summary>
    internal TimeSpan InitTime => Core.Utilities.ConvertRawToTimestamp(_ts);

    /// <summary>
    /// Gets the elapsed time since the timer was started.
    /// </summary>
    internal TimeSpan Elapsed => Core.Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - _ts);

    /// <summary>
    /// Use this function and its return value when inside a loop instead of <see cref="Elapsed"/>
    /// as it guarantees that no time will be discarded
    /// </summary>
    internal TimeSpan Restart()
    {
        var now = Stopwatch.GetTimestamp();
        var delta = Core.Utilities.ConvertRawToTimestamp(now - _ts);

        _ts = now;

        return delta;
    }

    /// <summary>
    /// Creates and starts a new instance of the <see cref="LightweightTimer"/> struct.
    /// </summary>
    /// <returns>A <see cref="LightweightTimer"/> instance that has been started and is ready to measure elapsed time.</returns>
    internal static LightweightTimer StartNew() => new() { _ts = Stopwatch.GetTimestamp() };
}