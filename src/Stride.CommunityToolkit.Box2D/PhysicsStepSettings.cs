namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// The stepping of a <see cref="PhysicsWorld2D"/>: the fixed step, the catch-up cap, Box2D's sub-steps and the
/// time scale, changed at any time through <see cref="PhysicsWorld2D.Settings"/> or <see cref="Box2DSimulation.Settings"/>.
/// The worker count is fixed when the world is created.
/// </summary>
public sealed record PhysicsStepSettings
{
    /// <summary>Target simulation frequency in hertz; the fixed step size is derived from it. Defaults to 60.</summary>
    public int TargetHz
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 60;

    /// <summary>Maximum number of fixed steps processed per frame, to avoid the spiral of death. Defaults to 3.</summary>
    public int MaxStepsPerFrame
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 3;

    /// <summary>Box2D sub-step count passed to each world step. Defaults to 4.</summary>
    public int SubStepCount
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    } = 4;

    /// <summary>Multiplier applied to incoming delta time before fixed-step accumulation. Defaults to 1.</summary>
    public float TimeScale
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 1f;

    /// <summary>
    /// Number of solver worker threads, fixed when the world is created, so it is set in the initializer only.
    /// 0 (the default) resolves to min(8, processor count / 2), the Box2D samples' convention; 1 steps
    /// single-threaded. Box2D is deterministic across worker counts.
    /// </summary>
    public int WorkerCount
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    }

    /// <summary>Duration of one fixed step in seconds (derived from <see cref="TargetHz"/>).</summary>
    public float FixedDeltaSeconds => 1f / TargetHz;
}