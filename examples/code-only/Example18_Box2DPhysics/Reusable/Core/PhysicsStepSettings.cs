namespace Example18_Box2DPhysics.Reusable.Core;

/// <summary>
/// Configuration for fixed timestep simulation stepping. This is an extraction candidate from the
/// monolithic <c>Box2DSimulation</c> class. Values mirror the current defaults used in the example.
/// </summary>
public sealed record PhysicsStepSettings(
    int TargetHz = 60,
    int MaxStepsPerFrame = 3,
    int SubStepCount = 4,
    float TimeScale = 1f)
{
    /// <summary>
    /// Duration of one fixed step in seconds (derived from <see cref="TargetHz"/>).
    /// </summary>
    public float FixedDeltaSeconds => 1f / TargetHz;
}
