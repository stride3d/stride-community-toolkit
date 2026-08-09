using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NumericsMatrix = System.Numerics.Matrix4x4;

namespace Example22_Instancing_EntityTransform;

/// <summary>
/// A faster alternative to <see cref="InstancingEntityTransform"/>, prototyped for PLAN.md Phase 1.
/// </summary>
/// <remarks>
/// Same idea - collect world matrices from entities every frame - but with the hot path rebuilt:
/// <list type="bullet">
/// <item>Caches <see cref="TransformComponent"/> references at registration, so the gather loop
/// chases one pointer per instance instead of three (entity -> transform -> matrix).</item>
/// <item>Gathers, inverts and computes the bounding box in a single parallel pass.</item>
/// <item>Inverts with a cheap rigid-transform inverse (physics bodies never scale); set
/// <see cref="AssumeRigidTransforms"/> to false for a SIMD general inverse instead.</item>
/// <item>Grows arrays by powers of two instead of reallocating exact-size every growth.</item>
/// <item>Removes instances with an O(1) swap-remove instead of an O(n) list scan.</item>
/// <item>Skips all CPU work when every registered <see cref="BodyComponent"/> is asleep and the
/// instance set is unchanged. The engine still re-uploads the (unchanged) GPU buffers each frame;
/// eliminating that is Phase 2 in PLAN.md.</item>
/// </list>
/// It cannot plug into <see cref="InstanceComponent"/> auto-registration because
/// InstancingEntityTransform's AddInstance/RemoveInstance are internal to Stride.Engine, so
/// entities are registered explicitly with <see cref="AddInstance"/>.
/// </remarks>
public class FastEntityTransformInstancing : InstancingUserArray
{
    private readonly List<TransformComponent> transforms = new();
    private readonly List<BodyComponent?> bodies = new();
    private readonly Dictionary<Entity, int> indexOf = new();

    private Matrix[] world = Array.Empty<Matrix>();
    private Matrix[] worldInverse = Array.Empty<Matrix>();

    private BoundingBox boundingBox = BoundingBox.Empty;
    private bool structureDirty;
    private int instancesWithoutBody;
    private readonly object mergeLock = new();

    // The instances are already in world space, so the master's own transform must not be applied
    public override ModelTransformUsage ModelTransformUsage { get => ModelTransformUsage.Ignore; }

    public override BoundingBox BoundingBox => boundingBox;

    /// <summary>
    /// When true (default), inverses use a rigid-transform fast path: transpose the rotation and
    /// rotate-negate the translation. Exact for rotation + translation with no scale, which is what
    /// physics bodies produce. Set to false if instances can be scaled or sheared.
    /// </summary>
    public bool AssumeRigidTransforms { get; set; } = true;

    /// <summary>
    /// Below this instance count the gather runs sequentially: Update may already be on an engine
    /// worker thread (the InstancingProcessor dispatches masters in parallel), so forking to the
    /// thread pool only pays off once there is enough work per instance to amortise the scheduling.
    /// Default is a guess pending the Phase 3 benchmarks (PLAN.md); tune there, not here.
    /// </summary>
    public int ParallelThreshold { get; set; } = 2048;

    /// <summary>CPU cost of the last <see cref="Update"/> call, for the overlay.</summary>
    public double LastUpdateMilliseconds { get; private set; }

    /// <summary>True when the last frame skipped all work because every body was asleep.</summary>
    public bool SleepSkippedLastFrame { get; private set; }

    public void AddInstance(Entity entity)
    {
        if (indexOf.ContainsKey(entity)) return;

        var body = entity.Get<BodyComponent>();
        if (body is null) instancesWithoutBody++;

        indexOf.Add(entity, transforms.Count);
        transforms.Add(entity.Transform);
        bodies.Add(body);
        structureDirty = true;
    }

    public bool RemoveInstance(Entity entity)
    {
        if (!indexOf.TryGetValue(entity, out var index)) return false;

        if (bodies[index] is null) instancesWithoutBody--;

        // Swap-remove: order is rebuilt every frame anyway, so stability is not required
        var last = transforms.Count - 1;
        if (index != last)
        {
            transforms[index] = transforms[last];
            bodies[index] = bodies[last];
            indexOf[transforms[index].Entity] = index;
        }

        transforms.RemoveAt(last);
        bodies.RemoveAt(last);
        indexOf.Remove(entity);
        structureDirty = true;
        return true;
    }

    public void Clear()
    {
        transforms.Clear();
        bodies.Clear();
        indexOf.Clear();
        instancesWithoutBody = 0;
        structureDirty = true;
    }

    public override void Update()
    {
        var start = Stopwatch.GetTimestamp();
        var count = transforms.Count;

        if (count == 0)
        {
            UpdateWorldMatrices(world, 0);
            boundingBox = BoundingBox.Empty;
            structureDirty = false;
            SleepSkippedLastFrame = false;
            LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            return;
        }

        // Sleep-skip: nothing joined or left, and Bepu says nothing moved, so last frame's
        // matrices, inverses and bounding box are all still valid
        if (!structureDirty && instancesWithoutBody == 0 && AllBodiesAsleep())
        {
            SleepSkippedLastFrame = true;
            LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            return;
        }

        SleepSkippedLastFrame = false;

        if (world.Length < count)
        {
            var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(count, 64));
            world = new Matrix[capacity];
            worldInverse = new Matrix[capacity];
        }

        // Gather + invert + bounding box, fused into one pass; parallel only when the count is
        // high enough to amortise the thread-pool scheduling (see ParallelThreshold).
        // Captured once so every range of one update uses the same inverse implementation even if
        // the property is flipped mid-flight
        var rigid = AssumeRigidTransforms;
        Vector3 min, max;

        if (count < ParallelThreshold)
        {
            GatherRange(0, count, rigid, out min, out max);
        }
        else
        {
            var sharedMin = new Vector3(float.MaxValue);
            var sharedMax = new Vector3(float.MinValue);

            Parallel.ForEach(Partitioner.Create(0, count), range =>
            {
                GatherRange(range.Item1, range.Item2, rigid, out var localMin, out var localMax);

                // Once per range (a handful per core), so contention is negligible
                lock (mergeLock)
                {
                    Vector3.Min(ref sharedMin, ref localMin, out sharedMin);
                    Vector3.Max(ref sharedMax, ref localMax, out sharedMax);
                }
            });

            min = sharedMin;
            max = sharedMax;
        }

        UpdateWorldMatrices(world, count);
        WorldInverseMatrices = worldInverse;
        boundingBox = new BoundingBox(min, max);
        structureDirty = false;
        LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    /// <summary>
    /// Gathers world matrices, inverses and position bounds for one index range. Shared by the
    /// sequential and parallel paths.
    /// </summary>
    private void GatherRange(int from, int to, bool rigid, out Vector3 min, out Vector3 max)
    {
        var localTransforms = CollectionsMarshal.AsSpan(transforms);
        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);

        for (var i = from; i < to; i++)
        {
            ref var m = ref world[i];
            m = localTransforms[i].WorldMatrix;

            if (rigid)
            {
                InvertRigid(ref m, out worldInverse[i]);
            }
            else
            {
                // Stride Matrix and System.Numerics.Matrix4x4 are both 16 sequential floats,
                // so reinterpreting gets the hardware-accelerated invert for free
                NumericsMatrix.Invert(Unsafe.As<Matrix, NumericsMatrix>(ref m), out var inverted);
                worldInverse[i] = Unsafe.As<NumericsMatrix, Matrix>(ref inverted);
            }

            var position = m.TranslationVector;
            Vector3.Min(ref min, ref position, out min);
            Vector3.Max(ref max, ref position, out max);
        }
    }

    private bool AllBodiesAsleep()
    {
        foreach (var body in CollectionsMarshal.AsSpan(bodies))
        {
            if (body!.Awake) return false;
        }

        return true;
    }

    /// <summary>
    /// Inverse of a rigid transform (rotation + translation, no scale): transpose the 3x3 rotation
    /// and rotate-negate the translation. Roughly an order of magnitude cheaper than a general
    /// 4x4 inverse.
    /// </summary>
    /// <remarks>
    /// Takes <c>ref</c> rather than <c>in</c> (and never mutates) because Stride's Matrix is not a
    /// readonly struct: analyzers flag non-readonly structs passed by <c>in</c>, and Stride's own
    /// math API (e.g. Matrix.Invert) uses ref for the same reason. Field-only reads make the two
    /// identical in codegen here.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvertRigid(ref Matrix m, out Matrix result)
    {
        result.M11 = m.M11; result.M12 = m.M21; result.M13 = m.M31; result.M14 = 0f;
        result.M21 = m.M12; result.M22 = m.M22; result.M23 = m.M32; result.M24 = 0f;
        result.M31 = m.M13; result.M32 = m.M23; result.M33 = m.M33; result.M34 = 0f;

        // Translation: -t * R^T (Stride uses row-vector convention, v' = v * M)
        result.M41 = -(m.M41 * m.M11 + m.M42 * m.M12 + m.M43 * m.M13);
        result.M42 = -(m.M41 * m.M21 + m.M42 * m.M22 + m.M43 * m.M23);
        result.M43 = -(m.M41 * m.M31 + m.M42 * m.M32 + m.M43 * m.M33);
        result.M44 = 1f;
    }
}

/// <summary>
/// Stock <see cref="InstancingEntityTransform"/> behaviour with a stopwatch around
/// <see cref="Update"/>, so the overlay can show stock and fast update costs side by side.
/// <see cref="InstanceComponent"/> auto-registration still works because this IS an
/// InstancingEntityTransform.
/// </summary>
public class TimedInstancingEntityTransform : InstancingEntityTransform
{
    public double LastUpdateMilliseconds { get; private set; }

    public override void Update()
    {
        var start = Stopwatch.GetTimestamp();
        base.Update();
        LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
}

/// <summary>Which flavour of cube a drop produces. See Program.cs.</summary>
public enum CubeKind
{
    /// <summary>Stock instancing: InstanceComponent + InstancingEntityTransform master.</summary>
    Instanced,
    /// <summary>Fast instancing: registered with the FastEntityTransformInstancing master.</summary>
    FastInstanced,
    /// <summary>Fast instancing plus user-managed GPU buffers: FastBufferedEntityTransformInstancing.</summary>
    FastBuffered,
    /// <summary>No instancing: own ModelComponent, one draw call per cube.</summary>
    Plain
}