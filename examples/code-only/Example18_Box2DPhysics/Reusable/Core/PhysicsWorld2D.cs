using Box2D.NET;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Example18_Box2DPhysics.Reusable.Core;

/// <summary>
/// Generic 2D physics world wrapper around Box2D.NET intended for future extraction into a reusable
/// toolkit library. This class deliberately avoids any dependency on Stride <c>Entity</c> types.
/// </summary>
/// <remarks>
/// Current example code uses <c>Box2DSimulation</c>; during refactor this class will absorb the
/// world + stepping responsibilities while a separate bridge handles Stride entity synchronization.
/// Existing comments and logic in the original class will be migrated here gradually.
/// </remarks>
public class PhysicsWorld2D : IDisposable
{
    private B2WorldId _worldId;
    private PhysicsStepSettings _settings;

    private double _accumulator;

    // Exposed tuning (mirrors record values but mutable for runtime adjustments)
    public int TargetHz
    {
        get => _settings.TargetHz;
        set => _settings = _settings with { TargetHz = value };
    }
    public int MaxStepsPerFrame
    {
        get => _settings.MaxStepsPerFrame;
        set => _settings = _settings with { MaxStepsPerFrame = value };
    }
    public int SubStepCount
    {
        get => _settings.SubStepCount;
        set => _settings = _settings with { SubStepCount = value };
    }
    public float TimeScale
    {
        get => _settings.TimeScale;
        set => _settings = _settings with { TimeScale = value };
    }

    /// <summary>
    /// Creates a new physics world with default gravity (0, -10).
    /// </summary>
    public PhysicsWorld2D(PhysicsStepSettings? settings = null)
    {
        _settings = settings ?? new PhysicsStepSettings();
        var def = b2DefaultWorldDef();
        def.gravity = new Box2D.NET.B2Vec2(0f, -10f);
        _worldId = b2CreateWorld(ref def);
    }

    /// <summary>
    /// Gets the native world id.
    /// </summary>
    public B2WorldId WorldId => _worldId;

    /// <summary>
    /// Adjusts global gravity.
    /// </summary>
    public void SetGravity(float x, float y) => b2World_SetGravity(_worldId, new Box2D.NET.B2Vec2(x, y));

    /// <summary>
    /// Advances the simulation using a fixed timestep accumulator strategy.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed real time seconds since last call.</param>
    public int Step(float deltaSeconds, Action<float>? perFixedStep = null)
    {
        if (deltaSeconds <= 0f) return 0;
        var scaled = deltaSeconds * _settings.TimeScale;
        _accumulator += scaled;

        var fixedStep = _settings.FixedDeltaSeconds;
        int performed = 0;

        while (_accumulator >= fixedStep && performed < _settings.MaxStepsPerFrame)
        {
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
    }
}