using Hexa.NET.ImGui;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using TimeSpan = System.TimeSpan;

namespace Stride.CommunityToolkit.ImGui;

public class PerfMonitor : BaseWindow
{
    public float GraphHeight = 48;
    public float FrameHeight = 128;
    public bool PauseEval;
    public bool PauseOnLargeDelta;
    public bool MonitorSampleAlloc;

    /// <summary>
    /// Circumvent <see cref="_cpuSamples"/> dictionary access access but
    /// only works for <see cref="_threadStaticMonitor"/>
    /// </summary>
    [System.ThreadStatic] static ThreadSampleCollection _threadStaticCollection;

    /// <summary> Owner of <see cref="_threadStaticCollection"/> </summary>
    static PerfMonitor _threadStaticMonitor;


    static readonly ProfilingEventType[] PROFILING_EVENT_TYPES = (ProfilingEventType[])System.Enum.GetValues(typeof(ProfilingEventType));
    static readonly ProfilingKey _dummyKey = new("dummy");


    // Work agnostic data
    readonly Dictionary<Thread, ThreadSampleCollection> _cpuSamples = [];
    LightweightTimer _timer = LightweightTimer.StartNew();
    (TimeSpan start, double duration) _cpuFrame;

    // Stride-specific data
    readonly List<EventWrapper> _sorter = [];
    CancellationTokenSource _stopProfiling;
    (List<SampleInstance> samples, TimeSpan start, double duration, int depth) _gpu = ([], default, default, default), _stride = ([], default, default, default);

    GraphPoint _graphAggregated;
    GraphPoint[] _graph = new GraphPoint[256];
    GraphPoint Average => _graphAggregated / _graph.Length;

    Vector2? _windowSize = new Vector2(420f, 240f);
    PerfSampler? _frame;

    PerfMonitorAutoSampler _autoSampler;
    PerfSampler _update, _draw;

    /// <summary>
    /// Place within a using statement to monitor the code within it.
    /// Creates a string each call which will produces unneeded garbage,
    /// see <see cref="Sample(string, bool)"/> for alloc less version.
    /// </summary>
    public PerfSampler Sample(
        bool sample = true,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = null,
        [CallerFilePath] string filePath = null) => Sample($"{filePath} . {member}:{S(line)}", sample);



    /// <summary> Place within a using statement to monitor the code within it </summary>
    public PerfSampler Sample(string id, bool sample = true)
    {
        return sample ? new PerfSampler(id, this) : new PerfSampler();
    }



    protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoMove |
                                                       ImGuiWindowFlags.NoSavedSettings |
                                                       ImGuiWindowFlags.NoFocusOnAppearing;

    protected override Vector2? WindowPos => new Vector2(1f, 1f);
    protected override Vector2? WindowSize => _windowSize;

    Vector2 GetGraphSize() => new(MaxWidth(), GraphHeight * Scale);
    static float MaxWidth() => GetContentRegionAvail().X;



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



    public PerfMonitor(Stride.Core.IServiceRegistry services) : base(services)
    {
        Visible = true;
        DrawOrder = int.MaxValue;
        Enabled = true;
        UpdateOrder = int.MaxValue;
        _autoSampler = new PerfMonitorAutoSampler(this);
    }



    protected override void OnDestroy()
    {
        if (IsStrideProfilingAll())
            Profiler.DisableAll();
        if (_threadStaticMonitor == this)
            _threadStaticMonitor = null;
        _autoSampler?.Dispose();
        _autoSampler = null;
    }



    protected override void OnDraw(bool collapsed)
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(ImGuiPass)}"))
        {
            ImGuiPass(collapsed);
        }
    }



    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _update.Dispose();
    }



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

        if (CollapsingHeader("Frame", ImGuiTreeNodeFlags.DefaultOpen))
        {
            using (Child())
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

                var buttonSize = new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing());
                bool profiling = IsStrideProfilingAll();
                if (Button(profiling ? "Stop Profiling" : "Profile Stride", buttonSize))
                {
                    if (profiling)
                    {
                        _stopProfiling.Cancel();
                        Profiler.DisableAll();
                    }
                    else
                    {
                        _stopProfiling = new CancellationTokenSource();
                        StartProcessingMarkers(_sorter, _stopProfiling.Token);
                        Profiler.EnableAll();
                    }
                }


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
                }
            }
        }

        // Leave it as dynamic after first set
        _windowSize = null;
    }

    static async void StartProcessingMarkers(List<EventWrapper> _sortedList, CancellationToken token)
    {
        ChannelReader<ProfilingEvent> events = Profiler.Subscribe();
        try
        {
            while (token.IsCancellationRequested == false)
            {
                ProfilingEvent perfEvent = await events.ReadAsync(token);
                EventWrapper begin = new(perfEvent, true, perfEvent.TimeStamp);
                EventWrapper end = new(perfEvent, false, perfEvent.TimeStamp + perfEvent.ElapsedTime);
                lock (_sortedList)
                {
                    var index = _sortedList.BinarySearch(begin);
                    if (index < 0)
                        _sortedList.Insert(~index, begin);
                    else
                        _sortedList.Insert(index, begin);
                    var index2 = _sortedList.BinarySearch(end);
                    if (index2 < 0)
                        _sortedList.Insert(~index2, end);
                    else
                        _sortedList.Insert(index2, end);
                }
            }
        }
        finally
        {
            Profiler.Unsubscribe(events);
        }
    }



    static void DrawSample(Vector2 corner, float maxWidth, SampleInstance sample, TimeSpan start, double duration)
    {
        const float MIN_SIZE = 2f;
        float height = GetTextLineHeightWithSpacing();
        // Get ratio of this sample compared to total frame duration
        float size = (float)(sample.Duration / duration);
        size *= maxWidth; // Fit ratio to window
        size = size < MIN_SIZE ? MIN_SIZE : size;
        // Compute offset from the window's edge
        float pos = (float)(sample.Start - start).TotalMilliseconds;
        pos /= (float)duration;
        pos *= maxWidth;
        // outside of view:left
        if (pos + size < MIN_SIZE)
            size += pos + size + MIN_SIZE;
        // outside of view:right
        if (pos > maxWidth - MIN_SIZE)
            pos = maxWidth - MIN_SIZE;

        SetCursorPos(corner + new Vector2(pos, sample.Depth * height));
        Button(sample.Id, new Vector2(size, height));
        if (IsItemHovered())
        {
            using (Tooltip())
            {
                TextUnformatted(sample.DeltaMemAlloc.HasValue
                    ? $"{sample.Id}:\n{S(sample.Duration)}ms - {Ts(sample.DeltaMemAlloc)} byte(s)"
                    : $"{sample.Id}:\n{S(sample.Duration)}ms");
            }
        }
    }


    void EndFrame()
    {
        using (Sample($"{nameof(PerfSampler)}:{nameof(EndFrame)}"))
        {
            _threadStaticMonitor ??= this;
            if (_frame == null)
                Guaranteed(_cpuSamples, Thread.CurrentThread).Depth++;
            else
                _frame?.Dispose();
            _frame = new PerfSampler("Frame", this, 0);

            using (Sample($"{nameof(PerfSampler)}:StrideProfilerParsing")) // Manage stride-specific profiler events
            {
                if (PauseEval)
                {
                    lock (_sorter)
                        _sorter.Clear();
                }
                else
                {
                    TimeSpan min = TimeSpan.MaxValue, max = TimeSpan.MinValue;

                    _stride.samples.Clear();
                    _gpu.samples.Clear();

                    lock (_sorter)
                    {
                        foreach (var e in _sorter)
                        {
                            ref var data = ref e.Event.IsGPUEvent() ? ref _gpu : ref _stride;

                            if (e.Begin)
                            {
                                data.depth++;
                                continue;
                            }

                            var end = e.Event.TimeStamp + e.Event.ElapsedTime;
                            min = min <= e.Event.TimeStamp ? min : e.Event.TimeStamp;
                            max = max > end ? max : end;
                            var sample = new SampleInstance(e.Event.Key.Name, data.depth, e.Event.TimeStamp, e.Event.ElapsedTime.TotalMilliseconds, null);
                            data.samples.Add(sample);
                            data.depth--;
                        }
                        _sorter.Clear();
                    }

                    if (_stride.samples.Count > 0)
                        _stride = _stride with { start = min, duration = (max - min).TotalMilliseconds };

                    if (_gpu.samples.Count > 0)
                        _gpu = _gpu with { start = min, duration = (max - min).TotalMilliseconds };
                }
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

            const double MB = (1 << 20);
            GraphPoint newPoint = new()
            {
                FrameTime = (float)_cpuFrame.duration,
                TotalManagedMB = (float)(System.GC.GetTotalMemory(false) / MB),
                DrawCalls = GraphicsDevice.FrameDrawCalls,
                BufferMemMB = (float)(GraphicsDevice.BuffersMemory / MB),
                TexMemMB = (float)(GraphicsDevice.TextureMemory / MB)
            };

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
    }



    static string S(float val, string format = null)
    {
        return val.ToString(format ?? "F2", System.Globalization.CultureInfo.CurrentCulture);
    }



    static string S(double val, string format = null)
    {
        return val.ToString(format ?? "F2", System.Globalization.CultureInfo.CurrentCulture);
    }



    static string Ts<T>(T val)
    {
        return val.ToString();
    }



    static bool IsStrideProfilingAll()
    {
        Profiler.Disable(_dummyKey);
        // With the given disabled key this function will return true if EnableAll is set
        return Profiler.IsEnabled(_dummyKey);
    }



    /// <summary> Guarantees that this key exist and returns at least a default new() value </summary>
    static TValue Guaranteed<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
        if (dictionary.TryGetValue(key, out TValue value) == false)
        {
            value = new TValue();
            dictionary.Add(key, value);
        }

        return value;
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
                _depth = _target.Depth;
                _target.Depth++;
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
                _target.Depth--;
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

        // Math operations are made in bulk, the struct is read as an array of floats, applies the operation
        // over each elements and outputs the result of those operations as a new instance.

        #region OPERATOR

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator +(in GraphPoint a, in GraphPoint b)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                fixed (GraphPoint* pSrcTmp = &b)
                {
                    float* pSrc = (float*)pSrcTmp;
                    var remaining = Count;
                    while (remaining-- > 0)
                    {
                        *pDest += *pSrc;
                        pSrc++;
                        pDest++;
                    }
                }

                return dest;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator -(in GraphPoint a, in GraphPoint b)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                fixed (GraphPoint* pSrcTmp = &b)
                {
                    float* pSrc = (float*)pSrcTmp;
                    var remaining = Count;
                    while (remaining-- > 0)
                    {
                        *pDest -= *pSrc;
                        pSrc++;
                        pDest++;
                    }
                }

                return dest;
            }
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator /(in GraphPoint a, in float v)
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



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphPoint operator *(in GraphPoint a, in float v)
        {
            unsafe
            {
                GraphPoint dest = a;
                float* pDest = (float*)&dest;
                var remaining = Count;
                while (remaining-- > 0)
                {
                    *pDest *= v;
                    pDest++;
                }

                return dest;
            }
        }

        #endregion

    }



    readonly record struct EventWrapper(ProfilingEvent Event, bool Begin, TimeSpan Stamp) : System.IComparable<EventWrapper>
    {
        public int CompareTo(EventWrapper other) => Stamp.CompareTo(other.Stamp);
    }



    /// <summary> Object containing a sample's data </summary>
    readonly struct SampleInstance(string id, int depth, TimeSpan start, double duration, long? deltaMemAlloc)
    {
        public readonly string Id = id;
        public readonly int Depth = depth;
        public readonly TimeSpan Start = start;
        public readonly double Duration = duration;
        public readonly long? DeltaMemAlloc = deltaMemAlloc;
    }



    /// <summary> Object to hold Stride's profiler samples until they are marked as done </summary>
    struct TemporaryStrideSample
    {
        public ProfilingEventType Type;
        public TimeSpan? Start;
        public TimeSpan? Duration;
        public int Depth;
    }



    /// <summary>
    /// A collection of <see cref="SampleInstance"/> for a specific thread,
    /// samples are aggregated and once <see cref="SetReady"/> called,
    /// will be pushed to <see cref="Displayed"/>.
    /// </summary>
    class ThreadSampleCollection
    {
        /// <summary>
        /// Current depth (sample within sample) of this thread,
        /// should be incremented when starting and decremented
        /// when ending a sample by the <see cref="PerfSampler"/>.
        /// </summary>
        public int Depth;

        /// <summary>
        /// Display-ready samples: samples that have ended before the end of the frame.
        /// </summary>
        public IReadOnlyList<SampleInstance> Displayed => _displayed;

        readonly object _bufferLock = new();
        List<SampleInstance> _buffered = [];
        List<SampleInstance> _displayed = [];



        /// <summary> We received all of the data for this frame, set it has ready </summary>
        public void SetReady()
        {
            lock (_bufferLock)
            {
                (_displayed, _buffered) = (_buffered, _displayed);
                _buffered.Clear();
            }
        }



        /// <summary> The given sample has finished and is ready to be displayed </summary>
        public void Add(SampleInstance sampleInstance)
        {
            lock (_bufferLock)
                _buffered.Add(sampleInstance);
        }



        /// <summary> Clear any samples buffered </summary>
        public void ClearBuffered()
        {
            lock (_bufferLock)
                _buffered.Clear();
        }
    }



    class PerfMonitorAutoSampler : GameSystem
    {
        readonly PerfMonitor _monitor;



        public PerfMonitorAutoSampler(PerfMonitor monitor) : base(monitor.Services)
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
