using Hexa.NET.ImGui;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
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
    /// Circumvents the <see cref="_cpuSamples"/> dictionary lookup, but
    /// only works for <see cref="_threadStaticMonitor"/>
    /// </summary>
    [ThreadStatic] private static ThreadSampleCollection? _threadStaticCollection;

    /// <summary> Owner of <see cref="_threadStaticCollection"/> </summary>
    private static PerfMonitor? _threadStaticMonitor;

    // Work agnostic data
    private readonly Dictionary<Thread, ThreadSampleCollection> _cpuSamples = [];
    private LightweightTimer _timer = LightweightTimer.StartNew();
    private (TimeSpan start, double duration) _cpuFrame;

    // Stride-specific data
    private readonly List<EventWrapper> _sorter = [];
    private CancellationTokenSource? _stopProfiling;
    private (List<SampleInstance> samples, TimeSpan start, double duration, int depth) _gpu = ([], default, default, default), _stride = ([], default, default, default);

    private GraphPoint _graphAggregated;
    private GraphPoint[] _graph = new GraphPoint[256];
    private GraphPoint Average => _graphAggregated / _graph.Length;

    private bool _windowSizeApplied;
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
        [CallerFilePath] string? filePath = null) => Sample($"{filePath} . {member}:{S(line)}", sample);

    /// <summary> Place within a using statement to monitor the code within it </summary>
    public PerfSampler Sample(string id, bool sample = true) => sample ? new PerfSampler(id, this) : default;

    /// <inheritdoc />
    protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing;

    /// <inheritdoc />
    protected override Vector2? WindowPos => new Vector2(1f, 1f);

    /// <inheritdoc />
    protected override Vector2? WindowSize => _windowSizeApplied ? null : new Vector2(420f, 240f);

    private Vector2 GetGraphSize() => new(MaxWidth(), GraphHeight * Scale);

    private static float MaxWidth() => GetContentRegionAvail().X;

    /// <summary>
    /// Changes how many frames the history graphs keep, preserving the most recent samples.
    /// </summary>
    /// <param name="newSize">The number of frames to keep.</param>
    /// <param name="force">When <see langword="false"/>, <paramref name="newSize"/> is clamped to 10..2048.</param>
    public void SetGraphSize(int newSize, bool force = false)
    {
        if (!force) newSize = Math.Clamp(newSize, 10, 2048);

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
        if (_threadStaticMonitor == this) _threadStaticMonitor = null;

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

        // Leave it as dynamic after first set
        _windowSizeApplied = true;
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
            DrawProfilingToggle();
            DrawProfilerSampleStrips();
        }
    }

    private void DrawCpuSampleStrips()
    {
        foreach (var (thread, collection) in _cpuSamples)
        {
            if (CollapsingHeader(thread.Name ?? "unnamed")) DrawStrip(collection.Displayed, _cpuFrame.start, _cpuFrame.duration);
        }
    }

    private void DrawProfilingToggle()
    {
        var buttonSize = new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing());
        var profiling = IsStrideProfilingAll();

        if (!Button(profiling ? "Stop Profiling" : "Profile Stride", buttonSize)) return;

        if (profiling)
        {
            _stopProfiling?.Cancel();
            Profiler.DisableAll();
        }
        else
        {
            _stopProfiling = new CancellationTokenSource();
            StartProcessingMarkers(_sorter, _stopProfiling.Token);
            Profiler.EnableAll();
        }
    }

    private void DrawProfilerSampleStrips()
    {
        if (CollapsingHeader(_gpu.samples.Count != 0 ? "GPU" : "GPU (profiling is off)")) DrawStrip(_gpu.samples, _gpu.start, _gpu.duration);
        if (CollapsingHeader(_stride.samples.Count != 0 ? "Stride Systems" : "Stride Systems (profiling is off)")) DrawStrip(_stride.samples, _stride.start, _stride.duration);
    }

    // A child region keeps the strip's buttons aligned within it; the caller's line keeps the regions apart
    private void DrawStrip(IReadOnlyList<SampleInstance> samples, TimeSpan start, double duration, [CallerLineNumber] int cln = 0)
    {
        using (Child(cln, new Vector2(0f, FrameHeight)))
        {
            for (var i = 0; i < samples.Count; i++) DrawSample(default, MaxWidth(), samples[i], start, duration);
        }
    }

    private void EndFrame()
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(EndFrame)}"))
        {
            _threadStaticMonitor ??= this;

            // The first frame opens this thread's root scope; every later one closes the previous frame's sample
            if (_frame is { } frame) frame.Dispose();
            else Guaranteed(_cpuSamples, Thread.CurrentThread).EnterScope();

            _frame = new PerfSampler("Frame", this, 0);

            // Manage stride-specific profiler events
            using (Sample($"{nameof(PerfSampler)}:StrideProfilerParsing"))
            {
                ParseStrideProfilerEvents(_sorter, ref _gpu, ref _stride, PauseEval);
            }

            if (PauseEval)
            {
                foreach (var collection in _cpuSamples.Values) collection.ClearBuffered();

                _timer = LightweightTimer.StartNew();

                return;
            }

            foreach (var collection in _cpuSamples.Values) collection.SetReady();

            _cpuFrame = (_timer.InitTime, _timer.Restart().TotalMilliseconds);

            PushGraphPoint();
        }
    }

    private void PushGraphPoint()
    {
        const double MB = 1 << 20;

        var newPoint = new GraphPoint(
            (float)_cpuFrame.duration,
            (float)(GC.GetTotalMemory(false) / MB),
            GraphicsDevice.FrameDrawCalls,
            (float)(GraphicsDevice.BuffersMemory / MB),
            (float)(GraphicsDevice.TextureMemory / MB));

        if (PauseOnLargeDelta && (newPoint.FrameTime < Average.FrameTime * 0.5f || newPoint.FrameTime > Average.FrameTime * 1.5f)) PauseEval = true;

        // Use simple aggregate to avoid having to loop through the array to get the average
        _graphAggregated -= _graph[0];
        _graphAggregated += newPoint;

        // Shift the history down one slot and push the latest point onto the end
        Array.Copy(_graph, 1, _graph, 0, _graph.Length - 1);
        _graph[^1] = newPoint;
    }

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
            if (_threadStaticCollection is null || _threadStaticMonitor != monitor) _threadStaticCollection = Guaranteed(monitor._cpuSamples, Thread.CurrentThread);

            _target = _threadStaticCollection;
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

    internal readonly record struct EventWrapper(ProfilingEvent Event, bool Begin, TimeSpan Stamp) : IComparable<EventWrapper>
    {
        public int CompareTo(EventWrapper other) => Stamp.CompareTo(other.Stamp);
    }

    /// <summary> Object containing a sample's data </summary>
    internal readonly record struct SampleInstance(string Id, int Depth, TimeSpan Start, double Duration, long? DeltaMemAlloc);

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
    }
}