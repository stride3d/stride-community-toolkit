using BenchmarkDotNet.Attributes;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Benchmarks.Instancing;

/// <summary>
/// Compares the per-frame instancing gather of Stride's <see cref="InstancingEntityTransform"/>
/// against <see cref="EntityInstancing"/>, and finds the count at which going parallel starts to pay.
/// </summary>
/// <remarks>
/// <para>
/// This measures the work done every frame for a master whose instances are moving: read each
/// instance's world matrix, invert it, and merge the bounding box. It deliberately does not cover the
/// sleep skip, which needs a Bepu simulation and is worth far more than any of this when it applies -
/// see the in-game measurements in Example22's PLAN.md.
/// </para>
/// <para>
/// <see cref="Stock"/> reimplements <see cref="InstancingEntityTransform.Update"/>, whose registration
/// hooks are internal to Stride.Engine, but does the inverse and bounding-box half by calling the real
/// <see cref="InstancingUserArray.Update"/>, so the baseline is engine code rather than an imitation.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class InstancingGatherBenchmarks
{
    /// <summary>Instance counts spanning the sequential/parallel crossover, which sits near 4096.</summary>
    [Params(256, 1024, 2048, 4096, 8192, 32768)]
    public int N;

    private readonly List<InstanceComponent> _stockInstances = [];
    private InstancingUserArray _stockInstancing = null!;
    private Matrix[] _stockMatrices = [];

    private EntityInstancing _sequential = null!;
    private EntityInstancing _parallel = null!;
    private EntityInstancing _parallelGeneralInverse = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _stockInstancing = new InstancingUserArray();
        _sequential = new EntityInstancing { ParallelThreshold = int.MaxValue };
        _parallel = new EntityInstancing { ParallelThreshold = 1 };
        _parallelGeneralInverse = new EntityInstancing { ParallelThreshold = 1, AssumeRigidTransforms = false };

        for (var i = 0; i < N; i++)
        {
            // One entity set per participant, all with identical transforms, so no benchmark is
            // helped or hurt by another's cache state
            _stockInstances.Add(CreateInstanceComponent(i));
            _sequential.AddInstance(CreateEntity(i));
            _parallel.AddInstance(CreateEntity(i));
            _parallelGeneralInverse.AddInstance(CreateEntity(i));
        }

        // Take the first-frame array growth out of the measurement for every participant
        Stock();
        _sequential.Update();
        _parallel.Update();
        _parallelGeneralInverse.Update();
    }

    /// <summary>Stride's current per-frame cost: sequential gather, scalar inverse.</summary>
    [Benchmark(Baseline = true)]
    public int Stock()
    {
        // InstancingEntityTransform.Update
        var maxInstanceCount = _stockInstances.Count;

        if (_stockMatrices.Length < maxInstanceCount)
        {
            _stockMatrices = new Matrix[maxInstanceCount];
        }

        var instanceCount = 0;

        for (var i = 0; i < maxInstanceCount; i++)
        {
            var instance = _stockInstances[i];

            if (instance.Enabled)
            {
                _stockMatrices[instanceCount++] = instance.Entity.Transform.WorldMatrix;
            }
        }

        // InstancingUserArray.Update - the real engine inverse and bounding-box loop
        _stockInstancing.UpdateWorldMatrices(_stockMatrices, instanceCount);
        _stockInstancing.Update();

        return _stockInstancing.InstanceCount;
    }

    /// <summary>Cached transforms and rigid inverse, single-threaded.</summary>
    [Benchmark]
    public int Fast_Sequential()
    {
        _sequential.Update();

        return _sequential.InstanceCount;
    }

    /// <summary>Cached transforms and rigid inverse, across the thread pool.</summary>
    [Benchmark]
    public int Fast_Parallel()
    {
        _parallel.Update();

        return _parallel.InstanceCount;
    }

    /// <summary>Parallel, but with the SIMD general inverse instead of the rigid fast path.</summary>
    [Benchmark]
    public int Fast_Parallel_GeneralInverse()
    {
        _parallelGeneralInverse.Update();

        return _parallelGeneralInverse.InstanceCount;
    }

    private static Entity CreateEntity(int seed)
    {
        var entity = new Entity();

        entity.Transform.WorldMatrix = CreateRigidTransform(seed);

        return entity;
    }

    private static InstanceComponent CreateInstanceComponent(int seed)
    {
        var entity = CreateEntity(seed);
        var instance = new InstanceComponent();

        // Master is deliberately left unset: assigning it would try to register with an
        // InstancingComponent, and only Entity and Enabled matter for the gather being measured
        entity.Add(instance);

        return instance;
    }

    private static Matrix CreateRigidTransform(int seed)
    {
        var random = new Random(seed);

        var rotation = Quaternion.RotationYawPitchRoll(
            random.NextSingle() * MathF.Tau,
            random.NextSingle() * MathF.Tau,
            random.NextSingle() * MathF.Tau);

        var translation = new Vector3(
            random.NextSingle() * 100f - 50f,
            random.NextSingle() * 100f - 50f,
            random.NextSingle() * 100f - 50f);

        return Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);
    }
}