using Hexa.NET.ImGui;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using static Stride.CommunityToolkit.ImGui.DebugTools.PerfMonitorHelpers;
using TimeSpan = System.TimeSpan;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// Performance monitor window for Stride Engine
/// </summary>
public class PerfMonitor : BaseWindow
{
    /// <summary>The height in pixels (before <see cref="BaseWindow.Scale"/>) of each history graph.</summary>
    public float GraphHeight = 48;

    /// <summary>The height in pixels of each per-thread, GPU and Stride-systems sample strip in the Frame section.</summary>
    public float FrameHeight = 128;

    /// <summary>
    /// The time in milliseconds a frame stays on the sample strips before the next one replaces it. Redrawn every
    /// frame, the bars shift a few pixels with each frame's timing and cannot be read; half a second, Stride's own
    /// profiler rate, can. Zero redraws every frame. A frame that trips <see cref="PauseOnLargeDelta"/> shows at once.
    /// </summary>
    public double StripRefreshTime = 500;

    /// <summary>
    /// When <see langword="true"/>, the graphs and sample strips stop updating and keep showing the last frame,
    /// so a spike can be examined. Toggled by the "Pause" checkbox.
    /// </summary>
    public bool PauseEval;

    /// <summary>
    /// When <see langword="true"/>, <see cref="PauseEval"/> is set automatically as soon as a frame takes less than
    /// half or more than one and a half times the average frame time.
    /// </summary>
    public bool PauseOnLargeDelta;

    /// <summary>
    /// When <see langword="true"/>, every <see cref="PerfSampler"/> also records the bytes allocated on its thread
    /// while it was open, shown in the sample's tooltip. Costs a little per sample.
    /// </summary>
    public bool MonitorSampleAlloc;

    /// <summary>
    /// This thread's sample collection and the monitor it belongs to, so most samples skip the dictionary lookup.
    /// Checked against the sampler's monitor, so two monitors on one thread do not cross-wire.
    /// </summary>
    [ThreadStatic] private static (PerfMonitor? Monitor, ThreadSampleCollection? Collection) _threadCache;

    // Work agnostic data. Written from whichever thread opens its first sample and read on the game thread
    private readonly ConcurrentDictionary<Thread, ThreadSampleCollection> _cpuSamples = [];
    private LightweightTimer _timer = LightweightTimer.StartNew();
    private LightweightTimer _stripTimer = LightweightTimer.StartNew();
    private (TimeSpan start, double duration) _cpuFrame;

    // Stride-specific data: the events the marker loop has delivered since the last frame, and the two strips they
    // feed. The GPU's frame starts at the compositor's root event, the systems' at the game update
    private readonly List<ProfilingEvent> _incoming = [];
    private CancellationTokenSource? _stopProfiling;
    private readonly ProfilerStrip _gpu = new(static e => e.Key.Name == "GraphicsCompositor");
    private readonly ProfilerStrip _stride = new(static e => e.Key == GameProfilingKeys.GameUpdate);

    private GraphPoint _graphAggregated;
    private GraphPoint[] _graph = new GraphPoint[256];
    private GraphPoint Average => _graphAggregated / _graph.Length;

    private bool _profiling;
    private PerfSampler? _frame;

    private PerfMonitorAutoSampler? _autoSampler;
    private PerfSampler _update, _draw;

    /// <summary>
    /// Place within a using statement to monitor the code within it.
    /// Allocates a string on every call; use <see cref="Sample(string, bool)"/> to avoid the garbage.
    /// </summary>
    public PerfSampler Sample(
        bool sample = true,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? member = null,
        [CallerFilePath] string? filePath = null) => Sample($"{filePath} . {member}:{line}", sample);

    /// <summary> Place within a using statement to monitor the code within it </summary>
    public PerfSampler Sample(string id, bool sample = true) => sample ? new PerfSampler(id, this) : default;

    /// <inheritdoc />
    protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing;

    /// <inheritdoc />
    protected override Vector2? WindowPos => new Vector2(1f, 1f);

    /// <inheritdoc />
    protected override Vector2? WindowSize => new Vector2(420f, 240f);

    private Vector2 GetGraphSize() => new(GetContentRegionAvail().X, GraphHeight * Scale);

    /// <summary>
    /// Changes how many frames the history graphs keep, preserving the most recent samples.
    /// </summary>
    /// <param name="frames">The number of frames to keep, clamped to 10..2048.</param>
    public void SetGraphSize(int frames)
    {
        var newSize = Math.Clamp(frames, 10, 2048);
        var delta = newSize - _graph.Length;

        if (delta == 0) return;

        var newGraph = new GraphPoint[newSize];

        if (delta > 0)
        {
            // Grown: the old points sit at the end, and the padding before them repeats the oldest point
            _graph.CopyTo(newGraph, delta);
            Array.Fill(newGraph, _graph[0], 0, delta);
        }
        else
        {
            // Shrunk: only the most recent points are kept
            Array.Copy(_graph, -delta, newGraph, 0, newSize);
        }

        _graph = newGraph;
        _graphAggregated = default;

        foreach (var point in _graph) _graphAggregated += point;
    }

    /// <summary>
    /// Creates the window, registers it with the game's systems and starts sampling the game's update and draw
    /// phases automatically.
    /// </summary>
    /// <param name="services">The game's service registry, which must already contain an <see cref="ImGuiSystem"/>.</param>
    public PerfMonitor(IServiceRegistry services) : base(services)
    {
        Visible = true;
        DrawOrder = int.MaxValue;
        Enabled = true;
        UpdateOrder = int.MaxValue;
        _autoSampler = new PerfMonitorAutoSampler(this);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        if (IsStrideProfilingAll()) Profiler.DisableAll();

        // Ends the marker loop too, or it would stay subscribed and fill the dead sorter once anything re-enables profiling
        _stopProfiling?.Cancel();
        _stopProfiling?.Dispose();
        _stopProfiling = null;

        _autoSampler?.Dispose();
        _autoSampler = null;
    }

    /// <inheritdoc />
    protected override void OnDraw(bool collapsed)
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(ImGuiPass)}"))
        {
            ImGuiPass(collapsed);
        }
    }

    /// <summary>
    /// Draws the window, then closes the sample that was opened at the start of this update.
    /// </summary>
    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _update.Dispose();
    }

    /// <summary>
    /// Closes the sample opened at the start of this draw and finalises the frame: collects every thread's samples,
    /// parses Stride's profiler events and pushes a new point onto the history graphs.
    /// </summary>
    public override void EndDraw()
    {
        base.EndDraw();
        _draw.Dispose();
        EndFrame();
    }

    private void ImGuiPass(bool collapsed)
    {
        if (collapsed) return;

        DrawControls();
        DrawFrameTimeGraph();
        DrawMemoryAndDrawCallGraphs();
        DrawFrameSection();
    }

    private void DrawControls()
    {
        using (UColumns(2))
        {
            Checkbox("Pause", ref PauseEval);
            NextColumn();
            Checkbox("on large delta", ref PauseOnLargeDelta);
        }

        Checkbox("Monitor Sample Alloc", ref MonitorSampleAlloc);

        var sampleSize = _graph.Length;

        InputInt("Sample Size", ref sampleSize);

        if (sampleSize != _graph.Length) SetGraphSize(sampleSize);
    }

    private void DrawFrameTimeGraph()
    {
        float min = float.MaxValue, max = float.MinValue;

        foreach (var point in _graph)
        {
            min = MathF.Min(min, point.FrameTime);
            max = MathF.Max(max, point.FrameTime);
        }

        var average = Average;

        // Narrower than the other graphs, to leave room for the min/max text beside it
        Plot("Frame Time Graph", ref _graph[0].FrameTime, $"~{S(average.FrameTime)}ms({S(1f / (average.FrameTime / 1000f))}fps)", widthReduction: 48f);
        SameLine();
        TextUnformatted($"-{S(max)}\n({S(max - min)})\n-{S(min)}");
    }

    private void DrawMemoryAndDrawCallGraphs()
    {
        using (UColumns(2))
        {
            if (CollapsingHeader("Managed Memory"))
            {
                float min = float.MaxValue, max = float.MinValue;

                foreach (var point in _graph)
                {
                    min = MathF.Min(min, point.TotalManagedMB);
                    max = MathF.Max(max, point.TotalManagedMB);
                }

                Plot("Managed Memory Graph", ref _graph[0].TotalManagedMB, $"Total: ~{S(Average.TotalManagedMB)}MB ({S(max - min)}d)");
            }

            NextColumn();

            if (CollapsingHeader("Draw calls")) Plot("Draw Calls Graph", ref _graph[0].DrawCalls, $"~{S(Average.DrawCalls)}", valueMin: 0f);
        }

        using (UColumns(2))
        {
            if (CollapsingHeader("Buffer Memory")) Plot("Buffer Memory Graph", ref _graph[0].BufferMemMB, $"~{S(Average.BufferMemMB)}MB", valueMin: 0f);

            NextColumn();

            if (CollapsingHeader("Texture Memory")) Plot("Texture Memory Graph", ref _graph[0].TexMemMB, $"~{S(Average.TexMemMB)}MB", valueMin: 0f);
        }
    }

    // One history graph across the column, reading one field out of every point
    private void Plot(string label, ref float values, string overlay, float valueMin = float.MaxValue, float widthReduction = 0f)
        => PlotLines(label, ref values, _graph.Length, overlay: overlay, valueMin: valueMin, size: GetGraphSize() - new Vector2(widthReduction, 0f), stride: GraphPoint.SizeOf);

    private void DrawFrameSection()
    {
        if (!CollapsingHeader("Frame", ImGuiTreeNodeFlags.DefaultOpen)) return;

        using (Child())
        {
            DrawCpuSampleStrips();
            DrawProfilingToggle(_profiling);
            DrawProfilerSampleStrips(_profiling);
        }
    }

    private void DrawCpuSampleStrips()
    {
        foreach (var (thread, collection) in _cpuSamples)
        {
            // One id per thread: two unnamed threads share a header label, and every strip shares this line
            using (ID(thread.ManagedThreadId))
            {
                if (CollapsingHeader(thread.Name ?? "unnamed")) DrawStrip(collection.Displayed, _cpuFrame.start, _cpuFrame.duration);
            }
        }
    }

    private void DrawProfilingToggle(bool profiling)
    {
        var buttonSize = new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing());

        if (!Button(profiling ? "Stop Profiling" : "Profile Stride", buttonSize)) return;

        // The marker loop follows at the end of the frame, through SyncMarkerLoop
        if (profiling) Profiler.DisableAll();
        else Profiler.EnableAll();
    }

    /// <summary>
    /// Keeps the marker loop in step with Stride's profiler, whichever side switched it: the button here,
    /// <see cref="Scripts.GameProfiler"/>'s Ctrl+Shift+P, or the game itself. Started only from the button, a scene
    /// that began with the profiler on showed empty strips until the button had been pressed twice.
    /// </summary>
    private void SyncMarkerLoop(bool profiling)
    {
        if (profiling == (_stopProfiling is not null)) return;

        if (profiling)
        {
            _stopProfiling = new CancellationTokenSource();
            _ = StartProcessingMarkers(_incoming, _stopProfiling.Token);
        }
        else
        {
            _stopProfiling!.Cancel();
            _stopProfiling.Dispose();
            _stopProfiling = null;

            // The strips show the last complete frame; off, they are empty
            lock (_incoming) _incoming.Clear();

            _gpu.Clear();
            _stride.Clear();
        }
    }

    private void DrawProfilerSampleStrips(bool profiling)
    {
        if (CollapsingHeader(profiling ? "GPU" : "GPU (profiling is off)")) DrawStrip(_gpu.Samples, _gpu.Start, _gpu.Duration);
        if (CollapsingHeader(profiling ? "Stride Systems" : "Stride Systems (profiling is off)")) DrawStrip(_stride.Samples, _stride.Start, _stride.Duration);
    }

    // A child region keeps the strip's buttons aligned within it; the caller's line keeps the regions apart
    private void DrawStrip(IReadOnlyList<SampleInstance> samples, TimeSpan start, double duration, [CallerLineNumber] int cln = 0)
    {
        using (Child(cln, new Vector2(0f, FrameHeight)))
        {
            // A frame holds several events of one name, so the button's ID is the sample's index, not its label
            for (var i = 0; i < samples.Count; i++)
            {
                using (ID(i)) DrawSample(default, GetContentRegionAvail().X, samples[i], start, duration);
            }
        }
    }

    private void EndFrame()
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(EndFrame)}"))
        {
            // The first frame opens this thread's root scope; every later one closes the previous frame's sample
            if (_frame is { } frame) frame.Dispose();
            else CollectionFor(Thread.CurrentThread).EnterScope();

            _frame = new PerfSampler("Frame", this, 0);

            // Read once a frame: the check is not free, and the button, the strip titles and the marker loop must agree
            _profiling = IsStrideProfilingAll();
            SyncMarkerLoop(_profiling);

            if (PauseEval)
            {
                ParseStrideProfilerEvents(_incoming, _gpu, _stride, pauseEval: true, refresh: false);

                foreach (var collection in _cpuSamples.Values) collection.ClearBuffered();

                _timer = LightweightTimer.StartNew();

                return;
            }

            var frameStart = _timer.InitTime;
            var frameDuration = _timer.Restart().TotalMilliseconds;
            var point = NewGraphPoint((float)frameDuration);
            var spike = PauseOnLargeDelta && (point.FrameTime < Average.FrameTime * 0.5f || point.FrameTime > Average.FrameTime * 1.5f);

            // The strips hold a frame long enough to read; a spike goes up at once, since it is what the pause is for
            var refresh = spike || StripRefreshTime <= 0 || _stripTimer.Elapsed.TotalMilliseconds >= StripRefreshTime;

            if (refresh)
            {
                _stripTimer = LightweightTimer.StartNew();
                _cpuFrame = (frameStart, frameDuration);
            }

            foreach (var collection in _cpuSamples.Values)
            {
                if (refresh) collection.SetReady();
                else collection.ClearBuffered();
            }

            using (Sample($"{nameof(PerfSampler)}:StrideProfilerParsing"))
            {
                ParseStrideProfilerEvents(_incoming, _gpu, _stride, pauseEval: false, refresh);
            }

            PushGraphPoint(point);

            if (spike) PauseEval = true;
        }
    }

    private GraphPoint NewGraphPoint(float frameTime)
    {
        const double MB = 1 << 20;

        return new GraphPoint(
            frameTime,
            (float)(GC.GetTotalMemory(false) / MB),
            GraphicsDevice.FrameDrawCalls,
            (float)(GraphicsDevice.BuffersMemory / MB),
            (float)(GraphicsDevice.TextureMemory / MB));
    }

    private void PushGraphPoint(GraphPoint newPoint)
    {
        // Use simple aggregate to avoid having to loop through the array to get the average
        _graphAggregated -= _graph[0];
        _graphAggregated += newPoint;

        // Shift the history down one slot and push the latest point onto the end
        Array.Copy(_graph, 1, _graph, 0, _graph.Length - 1);
        _graph[^1] = newPoint;
    }

    /// <summary>The sample collection of <paramref name="thread"/>, made on its first sample.</summary>
    private ThreadSampleCollection CollectionFor(Thread thread) => _cpuSamples.GetOrAdd(thread, static _ => new());

    /// <summary>
    /// Put this within a using statement to log performance of the code within it.
    /// Creates a new <see cref="SampleInstance"/> for the attached <see cref="PerfMonitor"/>
    /// once <see cref="PerfSampler(string, PerfMonitor, int)"/> and <see cref="Dispose()"/> have been called.
    /// The duration sent to the <see cref="PerfMonitor"/> will be the one between those calls.
    /// </summary>
    public readonly struct PerfSampler : IDisposable
    {
        private readonly LightweightTimer _timer;
        private readonly string _id;
        private readonly int _depth;
        private readonly bool _valid;
        private readonly long? _mem;
        private readonly ThreadSampleCollection _target;
        private readonly bool _customDepth;

        /// <summary>
        /// Starts a sample. Prefer <see cref="PerfMonitor.Sample(string, bool)"/>, which also lets sampling be switched off.
        /// </summary>
        /// <param name="id">The label shown for the sample in the Frame section.</param>
        /// <param name="monitor">The monitor that receives the sample when it is disposed.</param>
        /// <param name="customDepthParam">The row to draw the sample on, or <c>-1</c> to nest it under the sample currently open on this thread.</param>
        public PerfSampler(string id, PerfMonitor monitor, int customDepthParam = -1)
        {
            _id = id;

            // Cached per thread, so most samples skip the dictionary lookup
            if (_threadCache.Monitor != monitor) _threadCache = (monitor, monitor.CollectionFor(Thread.CurrentThread));

            _target = _threadCache.Collection!;
            _customDepth = customDepthParam >= 0;
            _depth = _customDepth ? customDepthParam : _target.EnterScope();
            _timer = LightweightTimer.StartNew();
            _mem = monitor.MonitorSampleAlloc ? GC.GetAllocatedBytesForCurrentThread() : null;
            _valid = true;
        }

        /// <summary> Dispose of it to log it to the PerfMonitor </summary>
        public void Dispose()
        {
            if (!_valid) return;

            var sample = new SampleInstance(_id, _depth, _timer.InitTime, _timer.Elapsed.TotalMilliseconds, _mem is { } mem ? GC.GetAllocatedBytesForCurrentThread() - mem : null);

            if (!_customDepth) _target.ExitScope();

            _target.Add(sample);
        }
    }

    /// <summary>
    /// One frame of the history graphs. The fields are plotted straight out of the array, <see cref="SizeOf"/> bytes apart.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct GraphPoint(float frameTime, float totalManagedMB, float drawCalls, float bufferMemMB, float texMemMB)
    {
        internal static readonly int SizeOf = Unsafe.SizeOf<GraphPoint>();

        public float FrameTime = frameTime, TotalManagedMB = totalManagedMB, DrawCalls = drawCalls, BufferMemMB = bufferMemMB, TexMemMB = texMemMB;

        public static GraphPoint operator +(GraphPoint a, GraphPoint b) => new(a.FrameTime + b.FrameTime, a.TotalManagedMB + b.TotalManagedMB, a.DrawCalls + b.DrawCalls, a.BufferMemMB + b.BufferMemMB, a.TexMemMB + b.TexMemMB);

        public static GraphPoint operator -(GraphPoint a, GraphPoint b) => new(a.FrameTime - b.FrameTime, a.TotalManagedMB - b.TotalManagedMB, a.DrawCalls - b.DrawCalls, a.BufferMemMB - b.BufferMemMB, a.TexMemMB - b.TexMemMB);

        public static GraphPoint operator /(GraphPoint a, float v) => new(a.FrameTime / v, a.TotalManagedMB / v, a.DrawCalls / v, a.BufferMemMB / v, a.TexMemMB / v);
    }

    /// <summary>
    /// An event's begin or end marker, in time order. Stride stamps a parent and its children at the same instant
    /// when nothing happens between them, so the ties decide the nesting: ends come before begins, two begins put
    /// the longer event first, and two ends the shorter; when even the spans match, the arrival order
    /// (<paramref name="Seq"/>) tells, since Stride publishes an event when it ends, children before their parent.
    /// </summary>
    internal readonly record struct EventWrapper(ProfilingEvent Event, bool Begin, TimeSpan Stamp, long Seq) : IComparable<EventWrapper>
    {
        private TimeSpan End => Event.TimeStamp + Event.ElapsedTime;

        public int CompareTo(EventWrapper other)
        {
            var order = Stamp.CompareTo(other.Stamp);

            if (order != 0) return order;
            if (Begin != other.Begin) return Begin ? 1 : -1;

            if (Begin)
            {
                order = other.End.CompareTo(End);

                return order != 0 ? order : other.Seq.CompareTo(Seq);
            }

            order = other.Event.TimeStamp.CompareTo(Event.TimeStamp);

            return order != 0 ? order : Seq.CompareTo(other.Seq);
        }
    }

    /// <summary> Object containing a sample's data </summary>
    internal readonly record struct SampleInstance(string Id, int Depth, TimeSpan Start, double Duration, long? DeltaMemAlloc);

    /// <summary>
    /// One strip of Stride's own profiler events, the GPU's or the systems', showing one whole frame at a time.
    /// </summary>
    /// <remarks>
    /// Stride publishes an event when it ends, so a frame's children arrive before their parent, and the GPU's
    /// events arrive a frame or more after the CPU's, on a clock of their own. Parsing whatever had arrived by the
    /// end of each monitor frame drew children at the wrong depth, parents on their own a frame later, and two GPU
    /// frames squeezed into one strip, which flashed. The strip therefore holds its events back until the next
    /// frame's start event has arrived, which proves the frame before it complete, and shows that frame.
    /// </remarks>
    /// <param name="isFrameStart">Recognises the event that begins a frame on this strip's timeline.</param>
    internal sealed class ProfilerStrip(Predicate<ProfilingEvent> isFrameStart)
    {
        // A bound on what is held back, in case the frame-start event never comes
        private const int MaxPending = 4096;

        private readonly List<(ProfilingEvent Event, long Seq)> _pending = [];
        private readonly List<EventWrapper> _markers = [];
        private long _seq;

        /// <summary>The samples of the frame on display.</summary>
        internal List<SampleInstance> Samples { get; } = [];

        /// <summary>Where the displayed frame's first sample starts.</summary>
        internal TimeSpan Start { get; private set; }

        /// <summary>From the first displayed sample's start to the last one's end, in milliseconds.</summary>
        internal double Duration { get; private set; }

        /// <summary>Holds an event back until its frame is complete.</summary>
        internal void Add(ProfilingEvent e) => _pending.Add((e, _seq++));

        /// <summary>
        /// Drops everything older than the newest complete frame and, when <paramref name="show"/> is set, puts
        /// that frame on display; otherwise it is kept for the next call that shows.
        /// </summary>
        internal void Advance(bool show)
        {
            if (_pending.Count == 0) return;

            // By time, and by arrival within the same instant, so the order is the same every frame
            _pending.Sort(static (a, b) => a.Event.TimeStamp != b.Event.TimeStamp ? a.Event.TimeStamp.CompareTo(b.Event.TimeStamp) : a.Seq.CompareTo(b.Seq));

            if (_pending.Count > MaxPending) _pending.RemoveRange(0, _pending.Count - MaxPending);

            // The newest frame start closes the frame before it; a lone one leaves a partial frame to drop. The frame
            // is bounded by time, not position: events stamped at the same instant as a frame start belong with it
            var close = _pending.FindLastIndex(p => isFrameStart(p.Event));

            if (close < 0) return;

            var frameEnd = _pending[close].Event.TimeStamp;
            var open = close == 0 ? -1 : _pending.FindLastIndex(close - 1, p => isFrameStart(p.Event) && p.Event.TimeStamp < frameEnd);
            var first = open < 0 ? close : open;

            while (first > 0 && _pending[first - 1].Event.TimeStamp == _pending[first].Event.TimeStamp) first--;

            if (open < 0 || !show)
            {
                _pending.RemoveRange(0, first);

                return;
            }

            var next = first;

            _markers.Clear();

            while (next < _pending.Count && _pending[next].Event.TimeStamp < frameEnd)
            {
                var (e, seq) = _pending[next++];

                _markers.Add(new EventWrapper(e, true, e.TimeStamp, seq));
                _markers.Add(new EventWrapper(e, false, e.TimeStamp + e.ElapsedTime, seq));
            }

            _markers.Sort();
            Samples.Clear();

            var depth = 0;
            TimeSpan min = TimeSpan.MaxValue, max = TimeSpan.MinValue;

            foreach (var marker in _markers)
            {
                if (marker.Begin)
                {
                    depth++;
                    continue;
                }

                var e = marker.Event;

                if (e.TimeStamp < min) min = e.TimeStamp;
                if (marker.Stamp > max) max = marker.Stamp;

                // A zero-length event's end sorts before its begin; it is drawn on its parent's row rather than below zero
                Samples.Add(new SampleInstance(e.Key.Name, Math.Max(depth, 1), e.TimeStamp, e.ElapsedTime.TotalMilliseconds, null));

                if (depth > 0) depth--;
            }

            Start = min;
            Duration = (max - min).TotalMilliseconds;

            _pending.RemoveRange(0, next);
        }

        /// <summary>Empties the strip: what is on display and what was held back.</summary>
        internal void Clear()
        {
            Samples.Clear();
            _pending.Clear();
        }
    }

    /// <summary>
    /// A collection of <see cref="SampleInstance"/> for a specific thread,
    /// samples are aggregated and once <see cref="SetReady"/> called,
    /// will be pushed to <see cref="Displayed"/>.
    /// </summary>
    private sealed class ThreadSampleCollection
    {
        private readonly Lock _bufferLock = new();
        private List<SampleInstance> _buffered = [];
        private List<SampleInstance> _displayed = [];

        /// <summary>
        /// Current depth (sample within sample) of this thread: deepened when a sample opens,
        /// restored when it closes.
        /// </summary>
        private int _depth;

        /// <summary>Display-ready samples: samples that have ended before the end of the frame.</summary>
        internal IReadOnlyList<SampleInstance> Displayed => _displayed;

        /// <summary>Opens a nested sample scope: returns the row for the new sample and deepens the nesting.</summary>
        internal int EnterScope() => _depth++;

        /// <summary>Closes the innermost sample scope.</summary>
        internal void ExitScope() => _depth--;

        /// <summary> We received all of the data for this frame, set it has ready </summary>
        internal void SetReady()
        {
            lock (_bufferLock)
            {
                (_displayed, _buffered) = (_buffered, _displayed);
                _buffered.Clear();
            }
        }

        /// <summary> The given sample has finished and is ready to be displayed </summary>
        internal void Add(SampleInstance sampleInstance)
        {
            lock (_bufferLock) _buffered.Add(sampleInstance);
        }

        /// <summary> Clear any samples buffered </summary>
        internal void ClearBuffered()
        {
            lock (_bufferLock) _buffered.Clear();
        }
    }

    // Opens the Update and Draw samples at the very start of each phase; the monitor closes them at the end
    private sealed class PerfMonitorAutoSampler : GameSystem
    {
        private readonly PerfMonitor _monitor;

        internal PerfMonitorAutoSampler(PerfMonitor monitor) : base(monitor.Services)
        {
            Game.GameSystems.Add(this);
            Enabled = true;
            Visible = true;
            DrawOrder = int.MinValue;
            UpdateOrder = int.MinValue;
            _monitor = monitor;
        }

        public override bool BeginDraw()
        {
            _monitor._draw = _monitor.Sample(nameof(Draw));

            return base.BeginDraw();
        }

        public override void Update(GameTime gameTime)
        {
            _monitor._update = _monitor.Sample(nameof(Update));
            base.Update(gameTime);
        }

        // Disposing a game system does not take it out of the collection; left in, it would open a sample every
        // frame that the closed monitor never closes
        protected override void Destroy()
        {
            Game.GameSystems.Remove(this);
            base.Destroy();
        }
    }
}