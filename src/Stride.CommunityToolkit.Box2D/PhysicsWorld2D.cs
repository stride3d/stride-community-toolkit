using Box2D.NET;
using Stride.Core.Diagnostics;
using static Box2D.NET.B2Diagnostics;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Engine-agnostic 2D physics world wrapper around Box2D.NET: owns the native world and advances it
/// on a fixed timestep. It deliberately avoids any dependency on Stride <c>Entity</c> types; pairing
/// bodies with entities is <see cref="Box2DStrideBridge"/>'s job.
/// </summary>
public sealed class PhysicsWorld2D : IDisposable
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Box2D");

    private B2WorldId _worldId;
    private PhysicsStepSettings _settings;
    private readonly Box2DTaskScheduler? _scheduler;

    private double _accumulator;

    // Box2D's diagnostics are process-wide, so they are routed once, before the first world exists.
    // The library's own handlers write to the console, which a windowed game never shows; Stride's
    // log reaches the debugger output and whatever else the game has subscribed to it.
    static PhysicsWorld2D()
    {
        b2SetLogFcn(LogMessage);
        b2SetAssertFcn(ReportAssert);
    }

    // The library only logs trouble - a pair buffer overflowing, a body going unstable - hence Warning
    private static void LogMessage(in string message) => Log.Warning(message.TrimEnd());

    // Asserts are compiled out of the Box2D.NET NuGet package (they need B2_ENABLE_ASSERT), so this
    // only hears from a build that has them. Returning non-zero keeps the library's own throw, so
    // the failure is logged and still stops the game rather than being swallowed
    private static int ReportAssert(string condition, string fileName, int lineNumber)
    {
        Log.Error($"Assertion failed: {condition} ({fileName}:{lineNumber})");

        return 1;
    }

    /// <summary>Target simulation frequency in hertz used to derive the fixed step size.</summary>
    public int TargetHz
    {
        get => _settings.TargetHz;
        set => _settings = _settings with { TargetHz = value };
    }

    /// <summary>Maximum number of fixed steps processed per frame, to avoid the spiral of death.</summary>
    public int MaxStepsPerFrame
    {
        get => _settings.MaxStepsPerFrame;
        set => _settings = _settings with { MaxStepsPerFrame = value };
    }

    /// <summary>Box2D sub-step count passed to each world step.</summary>
    public int SubStepCount
    {
        get => _settings.SubStepCount;
        set => _settings = _settings with { SubStepCount = value };
    }

    /// <summary>Multiplier applied to incoming delta time before fixed-step accumulation.</summary>
    public float TimeScale
    {
        get => _settings.TimeScale;
        set => _settings = _settings with { TimeScale = value };
    }

    /// <summary>
    /// Creates a new physics world with default gravity (0, -10).
    /// </summary>
    /// <param name="settings">Optional stepping configuration; defaults to <see cref="PhysicsStepSettings"/> defaults.</param>
    public PhysicsWorld2D(PhysicsStepSettings? settings = null)
    {
        _settings = settings ?? new PhysicsStepSettings();

        var def = b2DefaultWorldDef();

        def.gravity = new B2Vec2(0f, -10f);

        // b2DefaultWorldDef gives a single-threaded world - without task callbacks every solve runs
        // on the stepping thread, which is 4-5x slower for a large active scene. Measured on the
        // 10k-box stress pile: 25-43 ms per step single-threaded, 5.6-10.4 ms with 8 workers.
        var workerCount = ResolveWorkerCount(_settings.WorkerCount);

        if (workerCount > 1)
        {
            _scheduler = new Box2DTaskScheduler(workerCount);
            def.workerCount = workerCount;
            def.enqueueTask = _scheduler.Enqueue;
            def.finishTask = Box2DTaskScheduler.Finish;
        }

        _worldId = b2CreateWorld(in def);
    }

    /// <summary>
    /// The number of solver worker threads the world was created with (1 = single-threaded).
    /// </summary>
    public int WorkerCount => _scheduler?.WorkerCount ?? 1;

    // 0 = auto: the Box2D samples' convention. More is not better - on a 28-core machine, 14
    // workers measured slightly slower than 8 on the 10k-box pile. Box2D caps workers at 32.
    private static int ResolveWorkerCount(int requested)
        => requested > 0 ? Math.Min(requested, 32) : Math.Clamp(Environment.ProcessorCount / 2, 1, 8);

    /// <summary>
    /// Gets the native world id.
    /// </summary>
    public B2WorldId WorldId => _worldId;

    /// <summary>
    /// Adjusts global gravity.
    /// </summary>
    /// <param name="x">Gravity along the X axis.</param>
    /// <param name="y">Gravity along the Y axis (negative pulls down).</param>
    public void SetGravity(float x, float y) => b2World_SetGravity(_worldId, new B2Vec2(x, y));

    /// <summary>
    /// Advances the simulation using a fixed-timestep accumulator strategy.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed real time in seconds since the last call.</param>
    /// <param name="perFixedStep">Optional callback invoked after each fixed step with the step duration.</param>
    /// <param name="beforeFixedStep">Optional callback invoked before each fixed step with the step duration.</param>
    /// <returns>The number of fixed steps performed this call.</returns>
    public int Step(float deltaSeconds, Action<float>? perFixedStep = null, Action<float>? beforeFixedStep = null)
    {
        if (deltaSeconds <= 0f) return 0;

        _accumulator += deltaSeconds * _settings.TimeScale;

        var fixedStep = _settings.FixedDeltaSeconds;
        var performed = 0;

        while (_accumulator >= fixedStep && performed < _settings.MaxStepsPerFrame)
        {
            beforeFixedStep?.Invoke(fixedStep);
            b2World_Step(_worldId, fixedStep, _settings.SubStepCount);
            _accumulator -= fixedStep;
            performed++;
            perFixedStep?.Invoke(fixedStep);
        }

        return performed;
    }

    /// <summary>
    /// Disposes the underlying Box2D world.
    /// </summary>
    public void Dispose()
    {
        if (_worldId.index1 != 0)
        {
            b2DestroyWorld(_worldId);

            _worldId = default;
        }

        // After the world, so no step can still be dispatching work to the pool
        _scheduler?.Dispose();
    }
}