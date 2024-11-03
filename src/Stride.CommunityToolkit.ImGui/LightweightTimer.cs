using System.Diagnostics;

namespace Stride.CommunityToolkit.ImGui;

public struct LightweightTimer
{
    long _ts;

    public TimeSpan InitTime => Stride.Core.Utilities.ConvertRawToTimestamp(_ts);
    public TimeSpan Elapsed => Stride.Core.Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - _ts);

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
        var delta = Stride.Core.Utilities.ConvertRawToTimestamp(now - _ts);
        _ts = now;
        return delta;
    }

    public static LightweightTimer StartNew()
    {
        LightweightTimer lt = new LightweightTimer();
        lt.Start();
        return lt;
    }
}