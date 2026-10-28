using Stride.Core.Diagnostics;
using System.Globalization;
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using TimeSpan = System.TimeSpan;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// The stateless companions of <see cref="PerfMonitor"/>: sample-strip drawing, Stride profiler
/// marker collection, and small formatting and lookup helpers. Split out of
/// <see cref="PerfMonitor"/>, whose code keeps calling them unqualified through
/// <c>using static</c>.
/// </summary>
internal static class PerfMonitorHelpers
{
    private static readonly ProfilingKey _dummyKey = new("dummy");

    /// <summary>
    /// Subscribes to Stride's profiler and keeps every event's begin and end marker in <paramref name="sortedList"/>,
    /// in time order, until <paramref name="token"/> is cancelled.
    /// </summary>
    internal static async void StartProcessingMarkers(List<PerfMonitor.EventWrapper> sortedList, CancellationToken token)
    {
        var events = Profiler.Subscribe();

        try
        {
            while (!token.IsCancellationRequested)
            {
                var perfEvent = await events.ReadAsync(token);

                lock (sortedList)
                {
                    Insert(new PerfMonitor.EventWrapper(perfEvent, true, perfEvent.TimeStamp));
                    Insert(new PerfMonitor.EventWrapper(perfEvent, false, perfEvent.TimeStamp + perfEvent.ElapsedTime));
                }
            }
        }
        finally
        {
            Profiler.Unsubscribe(events);
        }

        // Binary search gives the slot of an equal stamp, or the bitwise complement of where one would go
        void Insert(PerfMonitor.EventWrapper wrapper)
        {
            var index = sortedList.BinarySearch(wrapper);

            sortedList.Insert(index < 0 ? ~index : index, wrapper);
        }
    }

    /// <summary>
    /// Turns the frame's collected Stride profiler begin/end markers into displayable samples, or just
    /// clears them while the monitor is paused. Depth bookkeeping and the min/max window come along verbatim.
    /// </summary>
    internal static void ParseStrideProfilerEvents(
        List<PerfMonitor.EventWrapper> sorter,
        ref (List<PerfMonitor.SampleInstance> samples, TimeSpan start, double duration, int depth) gpu,
        ref (List<PerfMonitor.SampleInstance> samples, TimeSpan start, double duration, int depth) stride,
        bool pauseEval)
    {
        if (pauseEval)
        {
            lock (sorter) sorter.Clear();

            return;
        }

        TimeSpan min = TimeSpan.MaxValue, max = TimeSpan.MinValue;

        stride.samples.Clear();
        gpu.samples.Clear();

        lock (sorter)
        {
            foreach (var e in sorter)
            {
                ref var data = ref e.Event.IsGPUEvent() ? ref gpu : ref stride;

                if (e.Begin)
                {
                    data.depth++;
                    continue;
                }

                var end = e.Event.TimeStamp + e.Event.ElapsedTime;

                min = min <= e.Event.TimeStamp ? min : e.Event.TimeStamp;
                max = max > end ? max : end;
                data.samples.Add(new PerfMonitor.SampleInstance(e.Event.Key.Name, data.depth, e.Event.TimeStamp, e.Event.ElapsedTime.TotalMilliseconds, null));
                data.depth--;
            }

            sorter.Clear();
        }

        if (stride.samples.Count > 0) stride = stride with { start = min, duration = (max - min).TotalMilliseconds };
        if (gpu.samples.Count > 0) gpu = gpu with { start = min, duration = (max - min).TotalMilliseconds };
    }

    /// <summary>One sample as a button on its row of the strip, sized and placed by its share of the frame.</summary>
    internal static void DrawSample(Vector2 corner, float maxWidth, PerfMonitor.SampleInstance sample, TimeSpan start, double duration)
    {
        const float MinSize = 2f;

        var height = GetTextLineHeightWithSpacing();

        // The sample's share of the frame, fitted to the window, never thinner than a few pixels
        var size = MathF.Max((float)(sample.Duration / duration) * maxWidth, MinSize);

        // Its offset from the window's edge
        var pos = (float)((sample.Start - start).TotalMilliseconds / duration) * maxWidth;

        // Outside the view on the left
        if (pos + size < MinSize) size += pos + size + MinSize;

        // Outside the view on the right
        if (pos > maxWidth - MinSize) pos = maxWidth - MinSize;

        SetCursorPos(corner + new Vector2(pos, sample.Depth * height));
        Button(sample.Id, new Vector2(size, height));

        if (IsItemHovered())
        {
            using (Tooltip())
            {
                TextUnformatted(sample.DeltaMemAlloc is { } bytes
                    ? $"{sample.Id}:\n{S(sample.Duration)}ms - {bytes} byte(s)"
                    : $"{sample.Id}:\n{S(sample.Duration)}ms");
            }
        }
    }

    internal static string S(float val, string? format = null) => val.ToString(format ?? "F2", CultureInfo.CurrentCulture);

    private static string S(double val, string? format = null) => val.ToString(format ?? "F2", CultureInfo.CurrentCulture);

    internal static bool IsStrideProfilingAll()
    {
        // With the given disabled key this function will return true if EnableAll is set
        Profiler.Disable(_dummyKey);

        return Profiler.IsEnabled(_dummyKey);
    }

    /// <summary> Guarantees that this key exist and returns at least a default new() value </summary>
    internal static TValue Guaranteed<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
        if (!dictionary.TryGetValue(key, out var value)) dictionary[key] = value = new TValue();

        return value;
    }
}