using Stride.Engine;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.CommunityToolkit.Rendering.Instancing;

/// <summary>
/// An <see cref="EntityInstancing"/> that owns its GPU buffers, so a scene where nothing moves costs
/// no upload bandwidth at all.
/// </summary>
/// <remarks>
/// <para>
/// On the normal <see cref="InstancingUserArray"/> path the engine re-uploads every instance matrix
/// each frame, even when they are identical to the last frame's - two matrices per instance, so
/// 2.5 MB per frame at 20,000 instances. Deriving from <see cref="InstancingUserBuffer"/> instead
/// puts the buffers under user control, and this class uploads only when the gather actually ran.
/// </para>
/// <para>
/// It requires an <see cref="InstancingBufferUploadRenderer"/> in the graphics compositor, which
/// <c>AddInstancingBufferUpload</c> registers in the right place. Without it nothing is ever drawn,
/// because the buffers are never created. Register it once and add every buffered instancing to it.
/// </para>
/// <para>
/// Pass the gather to the constructor to choose its behaviour - most usefully
/// <c>BepuEntityInstancing</c>, which adds the sleep skip that makes the saving worthwhile.
/// The buffers are released by <see cref="Dispose"/>; the engine never releases user-owned buffers.
/// </para>
/// </remarks>
public class BufferedEntityInstancing : InstancingUserBuffer, IInstancing, IDisposable
{
    private readonly EntityInstancing _gather;

    private bool _needUpload;
    private Buffer[]? _retiredThisFrame;
    private Buffer[]? _retiredLastFrame;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance backed by the given gather, or a plain <see cref="EntityInstancing"/>.
    /// </summary>
    /// <param name="gather">
    /// The instancing that collects the matrices. Use <c>BepuEntityInstancing</c> for physics bodies
    /// so settled scenes skip both the gather and the upload.
    /// </param>
    public BufferedEntityInstancing(EntityInstancing? gather = null) => _gather = gather ?? new EntityInstancing();

    /// <summary>Gets the instancing that collects the matrices.</summary>
    public EntityInstancing Gather => _gather;

    /// <summary>Gets the number of registered instances.</summary>
    public int RegisteredInstanceCount => _gather.RegisteredInstanceCount;

    /// <inheritdoc cref="EntityInstancing.LastUpdateMilliseconds" />
    public double LastUpdateMilliseconds => _gather.LastUpdateMilliseconds;

    /// <inheritdoc cref="EntityInstancing.UpdateSkippedLastFrame" />
    public bool UpdateSkippedLastFrame => _gather.UpdateSkippedLastFrame;

    /// <summary>Gets a value indicating whether the last frame sent no data to the GPU.</summary>
    public bool UploadSkippedLastFrame { get; private set; }

    /// <inheritdoc />
    /// <remarks>Instance matrices are already in world space, so the master's own transform is ignored.</remarks>
    public override ModelTransformUsage ModelTransformUsage => ModelTransformUsage.Ignore;

    /// <inheritdoc cref="EntityInstancing.AddInstance" />
    public bool AddInstance(Entity entity) => _gather.AddInstance(entity);

    /// <inheritdoc cref="EntityInstancing.RemoveInstance" />
    public bool RemoveInstance(Entity entity) => _gather.RemoveInstance(entity);

    /// <inheritdoc cref="EntityInstancing.Clear" />
    public void Clear() => _gather.Clear();

    /// <summary>
    /// Called by Stride's instancing processor once per frame, possibly on a worker thread. Does CPU
    /// work only; the GPU work happens in <see cref="InstancingBufferUploadRenderer"/>.
    /// </summary>
    /// <remarks>
    /// Hides rather than overrides <see cref="InstancingUserBuffer.Update"/>, which is not virtual;
    /// the processor calls it through <see cref="IInstancing"/>, which this class re-implements.
    /// </remarks>
    public new void Update()
    {
        _gather.Update();

        // Clamp to the buffers the render feature already holds. EnsureCapacity has not run for this
        // frame yet, so a frame that grows the set must not claim more instances than currently fit -
        // the render feature would bind a buffer too small for the count, or none at all
        var capacity = InstanceWorldBuffer?.ElementCount ?? 0;

        InstanceCount = Math.Min(_gather.InstanceCount, capacity);
        BoundingBox = _gather.BoundingBox;

        if (!_gather.UpdateSkippedLastFrame)
        {
            _needUpload = true;
        }
    }

    /// <summary>
    /// Creates or grows the GPU buffers, on the main thread before the render feature extracts them.
    /// </summary>
    /// <param name="device">The graphics device to allocate from.</param>
    internal void EnsureCapacity(GraphicsDevice device)
    {
        // Retirement protects the managed wrapper, not the GPU: the frame that grew the set still has
        // the old Buffer bound for drawing, and disposing zeroes its native handles. GPU-side lifetime
        // needs no help here - Stride's Vulkan backend fences native destruction behind the frame
        // fence, and D3D11 defers it natively - so disposing one frame after the last bind is enough
        if (_retiredLastFrame is not null)
        {
            foreach (var buffer in _retiredLastFrame) buffer.Dispose();
        }

        _retiredLastFrame = _retiredThisFrame;
        _retiredThisFrame = null;

        var needed = _gather.RegisteredInstanceCount;

        if (needed == 0 || (InstanceWorldBuffer is not null && InstanceWorldBuffer.ElementCount >= needed)) return;

        if (InstanceWorldBuffer is not null)
        {
            _retiredThisFrame = [InstanceWorldBuffer, InstanceWorldInverseBuffer];
        }

        var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(needed, 64));

        InstanceWorldBuffer = CreateMatrixBuffer(device, capacity);
        InstanceWorldInverseBuffer = CreateMatrixBuffer(device, capacity);
        _needUpload = true;
    }

    /// <summary>
    /// Uploads the gathered matrices if anything changed, on the main thread before the scene is drawn.
    /// </summary>
    /// <param name="commandList">The command list to record the upload on.</param>
    internal void Upload(CommandList commandList)
    {
        if (_gather.InstanceCount == 0)
        {
            // Nothing to send and nothing pending: don't leave the flag armed while the scene is
            // empty, the next gather re-arms it when instances come back
            _needUpload = false;
            UploadSkippedLastFrame = true;

            return;
        }

        var count = Math.Min(_gather.InstanceCount, InstanceWorldBuffer?.ElementCount ?? 0);

        if (!_needUpload || count <= 0)
        {
            UploadSkippedLastFrame = true;

            return;
        }

        InstanceWorldBuffer!.SetData(commandList, (ReadOnlySpan<Matrix>)_gather.WorldMatrices.AsSpan(0, count));
        InstanceWorldInverseBuffer.SetData(commandList, (ReadOnlySpan<Matrix>)_gather.WorldInverseMatrices.AsSpan(0, count));

        _needUpload = false;
        UploadSkippedLastFrame = false;
    }

    /// <summary>
    /// Releases the GPU buffers. The engine never releases user-owned buffers, so without this they
    /// live until the graphics device does.
    /// </summary>
    /// <remarks>
    /// Call once the master entity is gone and no frame is in flight, such as after <c>Game.Run</c>
    /// returns. Registering the instancing with a compositor renderer after disposing it is not valid.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the GPU buffers.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;

        foreach (var buffer in _retiredLastFrame ?? []) buffer.Dispose();
        foreach (var buffer in _retiredThisFrame ?? []) buffer.Dispose();

        _retiredLastFrame = null;
        _retiredThisFrame = null;

        InstanceWorldBuffer?.Dispose();
        InstanceWorldInverseBuffer?.Dispose();
        InstanceWorldBuffer = null!;
        InstanceWorldInverseBuffer = null!;

        InstanceCount = 0;
        _needUpload = false;
        _disposed = true;
    }

    private static Buffer CreateMatrixBuffer(GraphicsDevice device, int elementCount)
        => Buffer.New<Matrix>(device, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
}