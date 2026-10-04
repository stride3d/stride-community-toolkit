using Hexa.NET.ImGui;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System.Numerics;
using System.Runtime.CompilerServices;
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
    [System.ThreadStatic] static ThreadSampleCollection? _threadStaticCollection;

    /// <summary> Owner of <see cref="_threadStaticCollection"/> </summary>
    static PerfMonitor? _threadStaticMonitor;


    // Work agnostic data
    readonly Dictionary<Thread, ThreadSampleCollection> _cpuSamples = [];
    LightweightTimer _timer = LightweightTimer.StartNew();
    (TimeSpan start, double duration) _cpuFrame;

    // Stride-specific data
    readonly List<EventWrapper> _sorter = [];
    CancellationTokenSource? _stopProfiling;
    (List<SampleInstance> samples, TimeSpan start, double duration, int depth) _gpu = ([], default, default, default), _stride = ([], default, default, default);

    GraphPoint _graphAggregated;
    GraphPoint[] _graph = new GraphPoint[256];
    GraphPoint Average => _graphAggregated / _graph.Length;

    bool _windowSizeApplied;
    PerfSampler? _frame;

    PerfMonitorAutoSampler? _autoSampler;
    PerfSampler _update, _draw;

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
    public PerfSampler Sample(string id, bool sample = true)
    {
        return sample ? new PerfSampler(id, this) : new PerfSampler();
    }


    /// <inheritdoc />
    protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoMove |
                                                       ImGuiWindowFlags.NoSavedSettings |
                                                       ImGuiWindowFlags.NoFocusOnAppearing;

    /// <inheritdoc />
    protected override Vector2? WindowPos => new Vector2(1f, 1f);

    /// <inheritdoc />
    protected override Vector2? WindowSize => _windowSizeApplied ? null : new Vector2(420f, 240f);

    Vector2 GetGraphSize() => new(MaxWidth(), GraphHeight * Scale);

    static float MaxWidth() => GetContentRegionAvail().X;

    /// <summary>
    /// Changes how many frames the history graphs keep, preserving the most recent samples.
    /// </summary>
    /// <param name="newSize">The number of frames to keep.</param>
    /// <param name="force">When <see langword="false"/>, <paramref name="newSize"/> is clamped to 10..2048.</param>
    public void SetGraphSize(int newSize, bool force = false)
    {
        if (force == false)
            newSize = newSize < 10 ? 10 : newSize > 2048 ? 2048 : newSize;
        int delta = newSize - _graph.Length;
        if (delta == 0)
            return;

        var newGraph = new GraphPoint[newSize];
        if (delta > 0)
        {
            int offset = +delta;
            for (int i = 0; i < _graph.Length; i++)
                newGraph[i + offset] = _graph[i];
            // fill padded data with last graph data
            for (int i = 0; i < offset; i++)
                newGraph[i] = _graph[0];
        }
        else
        {
            // delta is negative, newGraph is smaller than _graph
            int offset = -delta;
            for (int i = 0; i < newGraph.Length; i++)
                newGraph[i] = _graph[i + offset];
        }

        _graph = newGraph;
        _graphAggregated = default;
        for (int i = 0; i < _graph.Length; i++)
            _graphAggregated += _graph[i];
    }

    /// <summary>
    /// Creates the window, registers it with the game's systems and starts sampling the game's update and draw
    /// phases automatically.
    /// </summary>
    /// <param name="services">The game's service registry, which must already contain an <see cref="ImGuiSystem"/>.</param>
    public PerfMonitor(Stride.Core.IServiceRegistry services) : base(services)
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
        if (IsStrideProfilingAll())
            Profiler.DisableAll();
        if (_threadStaticMonitor == this)
            _threadStaticMonitor = null;
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

    void ImGuiPass(bool collapsed)
    {
        if (collapsed)
            return;

        DrawControls();
        DrawFrameTimeGraph();
        DrawMemoryAndDrawCallGraphs();
        DrawFrameSection();

        // Leave it as dynamic after first set
        _windowSizeApplied = true;
    }

    void DrawControls()
    {
        using (UColumns(2))
        {
            Checkbox("Pause", ref PauseEval);
            NextColumn();
            Checkbox("on large delta", ref PauseOnLargeDelta);
        }
        Checkbox("Monitor Sample Alloc", ref MonitorSampleAlloc);

        int sampleSize = _graph.Length;
        InputInt("Sample Size", ref sampleSize);
        if (sampleSize != _graph.Length)
            SetGraphSize(sampleSize);
    }

    void DrawFrameTimeGraph()
    {
        {
            // Draw and update frame-time graph
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < _graph.Length; i++)
            {
                float v = _graph[i].FrameTime;
                if (v > max)
                    max = v;
                if (v < min)
                    min = v;
            }

            PlotLines("Frame Time Graph", ref _graph[0].FrameTime, _graph.Length,
                overlay: $"~{S(Average.FrameTime)}ms({S(1f / (Average.FrameTime / 1000f))}fps)",
                // Reduce the size somewhat to make space for the text
                size: GetGraphSize() - new Vector2(48f, 0f),
                stride: GraphPoint.SizeOf);
            SameLine();
            TextUnformatted($"-{S(max)}\n({S(max - min)})\n-{S(min)}");
        }
    }

    void DrawMemoryAndDrawCallGraphs()
    {
        using (UColumns(2))
        {
            if (CollapsingHeader("Managed Memory"))
            {
                float min = float.MaxValue, max = float.MinValue;
                for (int i = 0; i < _graph.Length; i++)
                {
                    float v = _graph[i].TotalManagedMB;
                    if (v > max)
                        max = v;
                    if (v < min)
                        min = v;
                }

                PlotLines("Managed Memory Graph", ref _graph[0].TotalManagedMB, _graph.Length,
                    overlay: $"Total: ~{S(Average.TotalManagedMB)}MB ({S(max - min)}d)",
                    size: GetGraphSize(),
                    stride: GraphPoint.SizeOf);
            }

            NextColumn();

            if (CollapsingHeader("Draw calls"))
            {
                PlotLines("Draw Calls Graph", ref _graph[0].DrawCalls, _graph.Length,
                    valueMin: 0f,
                    overlay: $"~{S(Average.DrawCalls)}",
                    size: GetGraphSize(),
                    stride: GraphPoint.SizeOf);
            }
        }

        using (UColumns(2))
        {
            if (CollapsingHeader("Buffer Memory"))
            {
                PlotLines("Buffer Memory Graph", ref _graph[0].BufferMemMB, _graph.Length,
                    valueMin: 0f,
                    overlay: $"~{S(Average.BufferMemMB)}MB",
                    size: GetGraphSize(),
                    stride: GraphPoint.SizeOf);
            }

            NextColumn();

            if (CollapsingHeader("Texture Memory"))
            {
                PlotLines("Texture Memory Graph", ref _graph[0].TexMemMB, _graph.Length,
                    valueMin: 0f,
                    overlay: $"~{S(Average.TexMemMB)}MB",
                    size: GetGraphSize(),
                    stride: GraphPoint.SizeOf);
            }
        }
    }

    void DrawFrameSection()
    {
        if (CollapsingHeader("Frame", ImGuiTreeNodeFlags.DefaultOpen))
        {
            using (Child())
            {
                DrawCpuSampleStrips();
                DrawProfilingToggle();
                DrawProfilerSampleStrips();            }
        }
    }

    void DrawCpuSampleStrips()
    {
        // CPU
        foreach (var data in _cpuSamples)
        {
                    var thread = data.Key;
                    var samples = data.Value.Displayed;
                    if (CollapsingHeader(thread.Name ?? "unnamed") == false)
                        continue;
                    // Child() to properly align content within
                    using (Child(size: new Vector2(0f, FrameHeight)))
                    {
                        for (int i = 0; i < samples.Count; i++)
                            DrawSample(default, MaxWidth(), samples[i], _cpuFrame.start, _cpuFrame.duration);
                    }
        }
    }

    void DrawProfilingToggle()
    {
                var buttonSize = new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing());
                bool profiling = IsStrideProfilingAll();
                if (Button(profiling ? "Stop Profiling" : "Profile Stride", buttonSize))
                {
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
                }    }

    void DrawProfilerSampleStrips()
    {
                // GPU
                if (CollapsingHeader(_gpu.samples.Count != 0 ? "GPU" : "GPU (profiling is off)"))
                {
                    // Child() to properly align content within
                    using (Child(size: new Vector2(0f, FrameHeight)))
                    {
                        foreach (var data in _gpu.samples)
                            DrawSample(default, MaxWidth(), data, _gpu.start, _gpu.duration);
                    }
                }

                // Stride Systems
                if (CollapsingHeader(_stride.samples.Count != 0 ? "Stride Systems" : "Stride Systems (profiling is off)"))
                {
                    // Child() to properly align content within
                    using (Child(size: new Vector2(0f, FrameHeight)))
                    {
                        foreach (var data in _stride.samples)
                            DrawSample(default, MaxWidth(), data, _stride.start, _stride.duration);
                    }
                }    }

    void EndFrame()
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(EndFrame)}"))
        {
            _threadStaticMonitor ??= this;
            if (_frame == null)
                _ = Guaranteed(_cpuSamples, Thread.CurrentThread).EnterScope();
            else
                _frame?.Dispose();
            _frame = new PerfSampler("Frame", this, 0);

            using (Sample($"{nameof(PerfSampler)}:StrideProfilerParsing")) // Manage stride-specific profiler events
            {
                ParseStrideProfilerEvents(_sorter, ref _gpu, ref _stride, PauseEval);
            }

            if (PauseEval)
            {
                foreach (var kvp in _cpuSamples)
                    kvp.Value.ClearBuffered();
                _timer = LightweightTimer.StartNew();
                return;
            }

            foreach (var threadSample in _cpuSamples)
                threadSample.Value.SetReady();

            _cpuFrame = (_timer.InitTime, _timer.Restart().TotalMilliseconds);

            PushGraphPoint();
        }
    }

    void PushGraphPoint()
    {
        const double MB = (1 << 20);
        var newPoint = new GraphPoint(
            (float)_cpuFrame.duration,
            (float)(System.GC.GetTotalMemory(false) / MB),
            GraphicsDevice.FrameDrawCalls,
            (float)(GraphicsDevice.BuffersMemory / MB),
            (float)(GraphicsDevice.TextureMemory / MB));

        if (PauseOnLargeDelta)
        {
            if (newPoint.FrameTime < Average.FrameTime * 0.5f || newPoint.FrameTime > Average.FrameTime * 1.5f)
                PauseEval = true;
        }

        // Use simple aggregate to avoid having to loop through the array to get the average
        _graphAggregated -= _graph[0];
        _graphAggregated += newPoint;

        // Move each value to a lower position in the array, could be replaced by a mem copy ?
        for (int i = 0; i < _graph.Length - 1; i++)
            _graph[i] = _graph[i + 1];
        // Push latest onto our plot
        _graph[^1] = newPoint;
    }

    /// <summary>
    /// Put this within a using statement to log performance of the code within it.
    /// Creates a new <see cref="SampleInstance"/> for the attached <see cref="PerfMonitor"/>
    /// once <see cref="PerfSampler(string, PerfMonitor, int)"/> and <see cref="Dispose()"/> have been called.
    /// The duration sent to the <see cref="PerfMonitor"/> will be the one between those calls.
    /// </summary>
    public readonly struct PerfSampler : System.IDisposable
    {
        readonly LightweightTimer _timer;
        readonly string _id;
        readonly int _depth;
        readonly bool _valid;
        readonly long? _mem;
        readonly ThreadSampleCollection _target;
        readonly bool _customDepth;

        /// <summary>
        /// Starts a sample. Prefer <see cref="PerfMonitor.Sample(string, bool)"/>, which also lets sampling be switched off.
        /// </summary>
        /// <param name="id">The label shown for the sample in the Frame section.</param>
        /// <param name="monitor">The monitor that receives the sample when it is disposed.</param>
        /// <param name="customDepthParam">The row to draw the sample on, or <c>-1</c> to nest it under the sample currently open on this thread.</param>
        public PerfSampler(string id, PerfMonitor monitor, int customDepthParam = -1)
        {
            _id = id;
            // Cache as ThreadStatic to remove most dictionary access
            if (_threadStaticCollection == null || _threadStaticMonitor != monitor)
                _threadStaticCollection = Guaranteed(monitor._cpuSamples, Thread.CurrentThread);
            _target = _threadStaticCollection;
            _customDepth = customDepthParam >= 0;
            if (_customDepth)
            {
                _depth = customDepthParam;
            }
            else
            {
                _depth = _target.EnterScope();
            }

            _timer = LightweightTimer.StartNew();

            _mem = null;
            if (monitor.MonitorSampleAlloc)
                _mem = System.GC.GetAllocatedBytesForCurrentThread();

            _valid = true;
        }

        /// <summary> Dispose of it to log it to the PerfMonitor </summary>
        public void Dispose()
        {
            if (_valid == false)
                return;

            TimeSpan start = _timer.InitTime;
            double ms = _timer.Elapsed.TotalMilliseconds;

            long? deltaMem = null;
            if (_mem.HasValue)
                deltaMem = System.GC.GetAllocatedBytesForCurrentThread() - _mem.Value;
            var sampleInstance = new SampleInstance(_id, _depth, start, ms, deltaMem);
            if (_customDepth == false)
                _target.ExitScope();
            _target.Add(sampleInstance);
        }
    }

    /// <summary>
    /// This struct's operation expects it to only contain instance fields of <see cref="float"/> type.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 4)]
    struct GraphPoint
    {
        public static readonly unsafe int SizeOf = sizeof(GraphPoint);
        public static readonly int Count = SizeOf / 4;

        public float FrameTime, TotalManagedMB, DrawCalls, BufferMemMB, TexMemMB;

        internal GraphPoint(float frameTime, float totalManagedMB, float drawCalls, float bufferMemMB, float texMemMB)
        {
            FrameTime = frameTime;
            TotalManagedMB = totalManagedMB;
            DrawCalls = drawCalls;
            BufferMemMB = bufferMemMB;
            TexMemMB = texMemMB;
        }

        // Math operations are made in bulk, the struct is read as an array of floats, applies the operation
        // over each elements and outputs the result of those operations as a new instance.

        #region OPERATOR

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator +(GraphPoint a, GraphPoint b)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                float* pSrc = (float*)&b;
                var remaining = Count;
                while (remaining-- > 0)
                {
                    *pDest += *pSrc;
                    pSrc++;
                    pDest++;
                }

                return dest;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator -(GraphPoint a, GraphPoint b)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                float* pSrc = (float*)&b;
                var remaining = Count;
                while (remaining-- > 0)
                {
                    *pDest -= *pSrc;
                    pSrc++;
                    pDest++;
                }

                return dest;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator /(GraphPoint a, float v)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                var remaining = Count;
                while (remaining-- > 0)
                {
                    *pDest /= v;
                    pDest++;
                }

                return dest;
            }
        }

        #endregion

    }

    internal readonly record struct EventWrapper(ProfilingEvent Event, bool Begin, TimeSpan Stamp) : System.IComparable<EventWrapper>
    {
        public int CompareTo(EventWrapper other) => Stamp.CompareTo(other.Stamp);
    }

    /// <summary> Object containing a sample's data </summary>
    internal readonly struct SampleInstance
    {
        public readonly string Id;
        public readonly int Depth;
        public readonly TimeSpan Start;
        public readonly double Duration;
        public readonly long? DeltaMemAlloc;

        internal SampleInstance(string id, int depth, TimeSpan start, double duration, long? deltaMemAlloc)
        {
            Id = id;
            Depth = depth;
            Start = start;
            Duration = duration;
            DeltaMemAlloc = deltaMemAlloc;
        }
    }

    /// <summary>
    /// A collection of <see cref="SampleInstance"/> for a specific thread,
    /// samples are aggregated and once <see cref="SetReady"/> called,
    /// will be pushed to <see cref="Displayed"/>.
    /// </summary>
    sealed class ThreadSampleCollection
    {
        /// <summary>
        /// Current depth (sample within sample) of this thread: deepened when a sample opens,
        /// restored when it closes.
        /// </summary>
        private int _depth;

        /// <summary>Opens a nested sample scope: returns the row for the new sample and deepens the nesting.</summary>
        internal int EnterScope() => _depth++;

        /// <summary>Closes the innermost sample scope.</summary>
        internal void ExitScope() => _depth--;

        /// <summary>
        /// Display-ready samples: samples that have ended before the end of the frame.
        /// </summary>
        internal IReadOnlyList<SampleInstance> Displayed => _displayed;

        readonly object _bufferLock = new();
        List<SampleInstance> _buffered = [];
        List<SampleInstance> _displayed = [];

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
            lock (_bufferLock)
                _buffered.Add(sampleInstance);
        }

        /// <summary> Clear any samples buffered </summary>
        internal void ClearBuffered()
        {
            lock (_bufferLock)
                _buffered.Clear();
        }
    }

    sealed class PerfMonitorAutoSampler : GameSystem
    {
        readonly PerfMonitor _monitor;

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
            base.Draw(gameTime);
        }
    }
}