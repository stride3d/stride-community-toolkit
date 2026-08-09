using Stride.Engine;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NumericsMatrix = System.Numerics.Matrix4x4;

namespace Stride.CommunityToolkit.Rendering.Instancing;

/// <summary>
/// Draws many entities in a single draw call by collecting their world matrices every frame.
/// </summary>
/// <remarks>
/// <para>
/// This is a faster alternative to Stride's <see cref="InstancingEntityTransform"/>. Both do the same
/// job - a master entity carries the <see cref="ModelComponent"/> and an <see cref="InstancingComponent"/>,
/// while the instances contribute only their transforms - but this one keeps direct
/// <see cref="TransformComponent"/> references, gathers and inverts in one parallel pass, reuses its
/// arrays, and removes instances in constant time.
/// </para>
/// <para>
/// Instances are registered explicitly with <see cref="AddInstance"/> rather than by adding an
/// <see cref="InstanceComponent"/>, because the engine's registration hooks are internal to
/// Stride.Engine. Instance entities must not have a <see cref="ModelComponent"/> of their own, or
/// they are drawn twice - once individually and once by the master.
/// </para>
/// <para>
/// Use <c>BepuEntityInstancing</c> from Stride.CommunityToolkit.Bepu instead when the instances are
/// physics bodies: it skips the whole update while every body is asleep, which is where most of the
/// saving comes from for settled scenes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// game.AddInstancingSupport();
///
/// var instancing = new EntityInstancing();
/// var master = new Entity("Master") { new ModelComponent(model), new InstancingComponent { Type = instancing } };
/// master.Scene = scene;
///
/// foreach (var entity in crowd) instancing.AddInstance(entity);
/// </code>
/// </example>
public class EntityInstancing : InstancingUserArray
{
    private readonly List<TransformComponent> _transforms = [];
    private readonly Dictionary<Entity, int> _indexOf = [];
    private readonly Lock _mergeLock = new();

    private Matrix[] _world = [];
    private Matrix[] _worldInverse = [];
    private BoundingBox _boundingBox = BoundingBox.Empty;

    /// <summary>
    /// Gets the number of registered instances, which is not necessarily the number drawn: disabled
    /// or skipped frames aside, <see cref="InstancingUserArray.InstanceCount"/> is what the renderer uses.
    /// </summary>
    public int RegisteredInstanceCount => _transforms.Count;

    /// <summary>
    /// Gets a value indicating whether the registered instances changed since the last update.
    /// </summary>
    /// <remarks>
    /// <see cref="CanSkipUpdate"/> implementations must not skip while this is <see langword="true"/>.
    /// </remarks>
    protected bool StructureDirty { get; private set; }

    /// <summary>
    /// Gets the world matrices of the registered instances, in registration order.
    /// </summary>
    /// <remarks>Only the first <see cref="InstancingUserArray.InstanceCount"/> entries are valid.</remarks>
    protected ReadOnlySpan<Matrix> GatheredMatrices => _world.AsSpan(0, InstanceCount);

    /// <inheritdoc />
    /// <remarks>Instance matrices are already in world space, so the master's own transform is ignored.</remarks>
    public override ModelTransformUsage ModelTransformUsage => ModelTransformUsage.Ignore;

    /// <inheritdoc />
    public override BoundingBox BoundingBox => _boundingBox;

    /// <summary>
    /// Gets or sets a value indicating whether instance transforms are rigid - rotation and
    /// translation only, with no scale or shear. Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// A rigid inverse is roughly an order of magnitude cheaper than a general 4x4 inverse and is
    /// exact for physics bodies, which never scale. Set to <see langword="false"/> if instances can be
    /// scaled or sheared, which switches to a SIMD-accelerated general inverse. Leaving it
    /// <see langword="true"/> for scaled instances produces incorrect lighting, because the inverse
    /// matrices are what the shader uses to transform normals.
    /// </remarks>
    public bool AssumeRigidTransforms { get; set; } = true;

    /// <summary>
    /// Gets or sets the instance count from which the gather runs in parallel. Defaults to 2048.
    /// </summary>
    /// <remarks>
    /// Below this count it runs sequentially, because forking to the thread pool costs more than the
    /// work it spreads - and Stride's instancing processor already dispatches masters in parallel.
    /// The default is where the two met on the benchmark machine: sequential is six times faster at
    /// 256 instances, they draw level at 2048, and parallel is three times faster by 8192. Machines
    /// with different core counts will cross over elsewhere, so tune this if it matters; set it to
    /// <see cref="int.MaxValue"/> to always stay sequential.
    /// </remarks>
    public int ParallelThreshold { get; set; } = 2048;

    /// <summary>
    /// Gets how long the last <see cref="Update"/> took. Intended for diagnostics and on-screen counters.
    /// </summary>
    public double LastUpdateMilliseconds { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the last update was skipped because nothing had moved.
    /// </summary>
    /// <remarks>Always <see langword="false"/> here; see <c>BepuEntityInstancing</c>.</remarks>
    public bool UpdateSkippedLastFrame { get; private set; }

    /// <summary>
    /// Registers an entity as an instance. Its <see cref="TransformComponent"/> is captured once, so
    /// the entity must not be reparented into a different transform component afterwards.
    /// </summary>
    /// <param name="entity">The entity to draw as an instance. Must not carry a <see cref="ModelComponent"/>.</param>
    /// <returns><see langword="true"/> if it was added; <see langword="false"/> if it was already registered.</returns>
    public bool AddInstance(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_indexOf.TryAdd(entity, _transforms.Count)) return false;

        _transforms.Add(entity.Transform);
        StructureDirty = true;

        OnInstanceAdded(entity);

        return true;
    }

    /// <summary>
    /// Unregisters an entity. Removing the entity from the scene does not unregister it, unlike the
    /// engine's <see cref="InstanceComponent"/>.
    /// </summary>
    /// <param name="entity">The entity to stop drawing.</param>
    /// <returns><see langword="true"/> if it was registered.</returns>
    public bool RemoveInstance(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_indexOf.Remove(entity, out var index)) return false;

        // Swap-remove: the draw order is rebuilt every frame, so stability is not required
        var last = _transforms.Count - 1;

        if (index != last)
        {
            _transforms[index] = _transforms[last];
            _indexOf[_transforms[index].Entity] = index;
        }

        _transforms.RemoveAt(last);
        StructureDirty = true;

        OnInstanceRemoved(index, last);

        return true;
    }

    /// <summary>
    /// Unregisters every instance.
    /// </summary>
    public void Clear()
    {
        _transforms.Clear();
        _indexOf.Clear();
        StructureDirty = true;

        OnInstancesCleared();
    }

    /// <summary>
    /// Called by Stride's instancing processor once per frame, possibly on a worker thread.
    /// </summary>
    public override void Update()
    {
        var start = Stopwatch.GetTimestamp();
        var count = _transforms.Count;

        if (count == 0)
        {
            UpdateWorldMatrices(_world, 0);
            _boundingBox = BoundingBox.Empty;
            StructureDirty = false;
            UpdateSkippedLastFrame = false;
            LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            return;
        }

        if (!StructureDirty && CanSkipUpdate())
        {
            // Last frame's matrices, inverses and bounding box are all still valid
            UpdateSkippedLastFrame = true;
            LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            return;
        }

        UpdateSkippedLastFrame = false;

        if (_world.Length < count)
        {
            var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(count, 64));

            _world = new Matrix[capacity];
            _worldInverse = new Matrix[capacity];
        }

        // Captured once so every range of one update uses the same inverse implementation, even if
        // the property is changed while this is in flight
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

                // Once per range - a handful per core - so contention is negligible
                lock (_mergeLock)
                {
                    Vector3.Min(ref sharedMin, ref localMin, out sharedMin);
                    Vector3.Max(ref sharedMax, ref localMax, out sharedMax);
                }
            });

            min = sharedMin;
            max = sharedMax;
        }

        UpdateWorldMatrices(_world, count);
        WorldInverseMatrices = _worldInverse;
        _boundingBox = new BoundingBox(min, max);
        StructureDirty = false;
        LastUpdateMilliseconds = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }

    /// <summary>
    /// Determines whether this frame's gather can be skipped because no instance has moved since the
    /// last one.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> to reuse the previous frame's matrices, inverses and bounding box
    /// verbatim. The base implementation always returns <see langword="false"/>, because without
    /// physics there is no cheap way to know whether a transform changed.
    /// </returns>
    /// <remarks>
    /// Called only when the registered instances are unchanged, so implementations need not check
    /// <see cref="StructureDirty"/>. It must be cheaper than the gather it avoids, and must never
    /// return <see langword="true"/> when a transform has in fact changed - the frame would render
    /// stale positions.
    /// </remarks>
    protected virtual bool CanSkipUpdate() => false;

    /// <summary>Called after an instance is appended, for derived classes keeping parallel data.</summary>
    /// <param name="entity">The newly registered entity, now at index <see cref="RegisteredInstanceCount"/> - 1.</param>
    protected virtual void OnInstanceAdded(Entity entity) { }

    /// <summary>
    /// Called after an instance is removed by swapping the last one into its place, for derived
    /// classes keeping parallel data.
    /// </summary>
    /// <param name="index">The index that was vacated and has just been overwritten.</param>
    /// <param name="lastIndex">The index the surviving instance came from, now past the end.</param>
    /// <remarks>
    /// Derived data must mirror this exactly: copy <paramref name="lastIndex"/> to
    /// <paramref name="index"/> when they differ, then drop <paramref name="lastIndex"/>.
    /// </remarks>
    protected virtual void OnInstanceRemoved(int index, int lastIndex) { }

    /// <summary>Called after every instance is unregistered, for derived classes keeping parallel data.</summary>
    protected virtual void OnInstancesCleared() { }

    /// <summary>
    /// Gathers world matrices, inverses and position bounds for one index range. Shared by the
    /// sequential and parallel paths.
    /// </summary>
    private void GatherRange(int from, int to, bool rigid, out Vector3 min, out Vector3 max)
    {
        var transforms = CollectionsMarshal.AsSpan(_transforms);

        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);

        for (var i = from; i < to; i++)
        {
            ref var world = ref _world[i];

            world = transforms[i].WorldMatrix;

            if (rigid)
            {
                InvertRigid(ref world, out _worldInverse[i]);
            }
            else
            {
                // Stride's Matrix and System.Numerics.Matrix4x4 are both 16 sequential floats, so
                // reinterpreting gets the hardware-accelerated inverse for free
                NumericsMatrix.Invert(Unsafe.As<Matrix, NumericsMatrix>(ref world), out var inverted);

                _worldInverse[i] = Unsafe.As<NumericsMatrix, Matrix>(ref inverted);
            }

            var position = world.TranslationVector;

            Vector3.Min(ref min, ref position, out min);
            Vector3.Max(ref max, ref position, out max);
        }
    }

    /// <summary>
    /// Inverts a rigid transform by transposing the rotation and rotate-negating the translation.
    /// </summary>
    /// <remarks>
    /// Takes <c>ref</c> rather than <c>in</c>, and never mutates: Stride's <see cref="Matrix"/> is not
    /// a readonly struct, so <c>in</c> is flagged by analyzers and risks hidden defensive copies.
    /// Stride's own math API uses <c>ref</c> for the same reason.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void InvertRigid(ref Matrix m, out Matrix result)
    {
        result.M11 = m.M11; result.M12 = m.M21; result.M13 = m.M31; result.M14 = 0f;
        result.M21 = m.M12; result.M22 = m.M22; result.M23 = m.M32; result.M24 = 0f;
        result.M31 = m.M13; result.M32 = m.M23; result.M33 = m.M33; result.M34 = 0f;

        // Translation: -t * transpose(R), because Stride uses the row-vector convention v' = v * M
        result.M41 = -(m.M41 * m.M11 + m.M42 * m.M12 + m.M43 * m.M13);
        result.M42 = -(m.M41 * m.M21 + m.M42 * m.M22 + m.M43 * m.M23);
        result.M43 = -(m.M41 * m.M31 + m.M42 * m.M32 + m.M43 * m.M33);
        result.M44 = 1f;
    }
}