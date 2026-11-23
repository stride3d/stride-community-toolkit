using Stride.Core.Diagnostics;
using System.Globalization;
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using TimeSpan = System.TimeSpan;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// The stateless companions of <see cref="PerfMonitor"/>: sample-strip drawing, Stride profiler
/// marker collection, and small formatting and lookup helpers.
/// </summary>
internal static class PerfMonitorHelpers
{
    private static readonly ProfilingKey _dummyKey = new("dummy");

    /// <summary>
    /// Subscribes to Stride's profiler and appends every event to <paramref name="incoming"/> as it ends, until
    /// <paramref name="token"/> is cancelled. Cancelling is the normal way out, not a fault.
    /// </summary>
    internal static async Task StartProcessingMarkers(List<ProfilingEvent> incoming, CancellationToken token)
    {
        var events = Profiler.Subscribe();

        try
        {
            while (!token.IsCancellationRequested)
            {
                var perfEvent = await events.ReadAsync(token);

                lock (incoming) incoming.Add(perfEvent);
            }
        }
        catch (OperationCanceledException)
        {
            // Stop Profiling, or the window closing
        }
        finally
        {
            Profiler.Unsubscribe(events);
        }
    }

    /// <summary>
    /// Hands the Stride profiler events that arrived since the last frame to their strips, GPU or systems, and lets
    /// each keep its newest complete frame, shown when <paramref name="refresh"/> is set; or just discards the
    /// events while the monitor is paused. See <see cref="PerfMonitor.ProfilerStrip"/> for why the strips hold
    /// events back.
    /// </summary>
    internal static void ParseStrideProfilerEvents(List<ProfilingEvent> incoming, PerfMonitor.ProfilerStrip gpu, PerfMonitor.ProfilerStrip stride, bool pauseEval, bool refresh)
    {
        lock (incoming)
        {
            if (!pauseEval)
            {
                foreach (var e in incoming) (e.IsGPUEvent() ? gpu : stride).Add(e);
            }

            incoming.Clear();
        }

        if (pauseEval) return;

        gpu.Advance(refresh);
        stride.Advance(refresh);
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
            using (Tooltip(out var open))
            {
                if (open)
                {
                    TextUnformatted(sample.DeltaMemAlloc is { } bytes
                        ? $"{sample.Id}:\n{S(sample.Duration)}ms - {bytes} byte(s)"
                        : $"{sample.Id}:\n{S(sample.Duration)}ms");
                }
            }
        }
    }

    internal static string S(float val) => val.ToString("F2", CultureInfo.CurrentCulture);

    private static string S(double val) => val.ToString("F2", CultureInfo.CurrentCulture);

    internal static bool IsStrideProfilingAll()
    {
        // With the given disabled key this function will return true if EnableAll is set
        Profiler.Disable(_dummyKey);

        return Profiler.IsEnabled(_dummyKey);
    }
}