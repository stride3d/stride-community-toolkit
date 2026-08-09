using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Buffer = Stride.Graphics.Buffer;

namespace Example22_Instancing_EntityTransform;

/// <summary>
/// Phase 2 of PLAN.md: <see cref="FastEntityTransformInstancing"/> plus user-managed GPU buffers,
/// so a settled pile costs zero CPU AND zero upload bandwidth per frame.
/// </summary>
/// <remarks>
/// On the <see cref="InstancingUserArray"/> path (Phase 1) the engine re-uploads both matrix
/// buffers every frame even when the sleep-skip did no CPU work - 2.5 MB/frame at 20k instances.
/// Deriving from <see cref="InstancingUserBuffer"/> instead flips the render feature into
/// "buffers managed by user" mode: it binds whatever buffers we provide and never uploads.
/// <para>
/// The frame order (see GraphicsCompositor.DrawCore) is: entity processors (which call
/// <see cref="Update"/> and gather) -> compositor Collect -> render feature Extract/Prepare ->
/// compositor Draw. <see cref="InstancingBufferUploadRenderer"/> hooks Collect to create or grow
/// the buffers on the main thread before Extract sees them, and hooks Draw - inserted BEFORE the
/// scene camera renderer - to upload with the frame's command list before the scene is drawn, so
/// the data is same-frame fresh. When the gather sleep-skips, no upload happens at all.
/// </para>
/// <para>
/// Two frame-boundary rules keep the engine happy. <see cref="InstancingUserBuffer.InstanceCount"/>
/// is clamped to the capacity of the buffers the processor has already handed to the render
/// feature, because Extract throws on a null buffer with a positive count; a growth frame therefore
/// draws the old capacity once and catches up next frame. And replaced buffers are disposed two
/// Collects later, because the growth frame still has the old buffer bound for drawing.
/// </para>
/// </remarks>
public class FastBufferedEntityTransformInstancing : InstancingUserBuffer, IInstancing, IDisposable
{
    private readonly FastEntityTransformInstancing gather = new();

    private bool needUpload;
    private Buffer[]? retiredThisFrame;
    private Buffer[]? retiredLastFrame;

    /// <summary>See <see cref="FastEntityTransformInstancing.AssumeRigidTransforms"/>.</summary>
    public bool AssumeRigidTransforms
    {
        get => gather.AssumeRigidTransforms;
        set => gather.AssumeRigidTransforms = value;
    }

    /// <summary>CPU cost of the last gather, for the overlay.</summary>
    public double LastUpdateMilliseconds => gather.LastUpdateMilliseconds;

    /// <summary>True when the last frame skipped the gather because every body was asleep.</summary>
    public bool SleepSkippedLastFrame => gather.SleepSkippedLastFrame;

    /// <summary>True when the last frame sent no buffer data to the GPU.</summary>
    public bool UploadSkippedLastFrame { get; private set; }

    /// <summary>The full registered instance count, unlike the possibly clamped InstanceCount.</summary>
    public int RegisteredInstanceCount => gather.InstanceCount;

    public void AddInstance(Entity entity) => gather.AddInstance(entity);

    public bool RemoveInstance(Entity entity) => gather.RemoveInstance(entity);

    public void Clear() => gather.Clear();

    /// <summary>
    /// Called by the InstancingProcessor each frame (possibly on a worker thread - CPU work only
    /// here, all GPU work lives in the upload renderer). Re-implements <see cref="IInstancing"/>
    /// because <see cref="InstancingUserBuffer.Update"/> is not virtual.
    /// </summary>
    public new void Update()
    {
        gather.Update();

        // Clamp to the buffers the processor is about to hand to the render feature this frame;
        // EnsureCapacity has not run yet, so a growth frame must not claim more than fits
        var capacity = InstanceWorldBuffer?.ElementCount ?? 0;
        InstanceCount = Math.Min(gather.InstanceCount, capacity);
        BoundingBox = gather.BoundingBox;

        if (!gather.SleepSkippedLastFrame)
        {
            needUpload = true;
        }
    }

    /// <summary>
    /// Creates or grows the GPU buffers. Runs on the main thread during compositor Collect,
    /// before the render feature's Extract.
    /// </summary>
    internal void EnsureCapacity(GraphicsDevice device)
    {
        // Retirement protects the MANAGED wrapper, not the GPU: the growth frame's RenderInstancing
        // still binds the old Buffer object in Prepare, and Dispose zeroes its native handles.
        // GPU-side lifetime needs no help - Stride's Vulkan backend fences native destruction
        // (TemporaryResourceCollector in GraphicsDevice.Vulkan.cs) and D3D11 defers it natively,
        // so disposing one frame after the last bind is enough
        if (retiredLastFrame is not null)
        {
            foreach (var buffer in retiredLastFrame) buffer.Dispose();
        }

        retiredLastFrame = retiredThisFrame;
        retiredThisFrame = null;

        var needed = gather.InstanceCount;

        if (needed == 0 || (InstanceWorldBuffer is not null && InstanceWorldBuffer.ElementCount >= needed)) return;

        if (InstanceWorldBuffer is not null)
        {
            // This frame's RenderInstancing still points at the old buffers; retire, don't dispose
            retiredThisFrame = [InstanceWorldBuffer, InstanceWorldInverseBuffer];
        }

        var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(needed, 64));

        InstanceWorldBuffer = CreateMatrixBuffer(device, capacity);
        InstanceWorldInverseBuffer = CreateMatrixBuffer(device, capacity);
        needUpload = true;
    }

    /// <summary>
    /// Uploads the gathered matrices if anything changed since the last upload. Runs on the main
    /// thread during compositor Draw, before the scene camera renderer records the scene.
    /// </summary>
    internal void Upload(CommandList commandList)
    {
        if (gather.InstanceCount == 0)
        {
            // Nothing to upload and nothing pending: don't leave the dirty flag armed while the
            // scene is empty; the next gather re-arms it when instances come back
            needUpload = false;
            UploadSkippedLastFrame = true;
            return;
        }

        var count = Math.Min(gather.InstanceCount, InstanceWorldBuffer?.ElementCount ?? 0);

        if (!needUpload || count <= 0)
        {
            UploadSkippedLastFrame = true;
            return;
        }

        InstanceWorldBuffer!.SetData(commandList, (ReadOnlySpan<Matrix>)gather.WorldMatrices.AsSpan(0, count));
        InstanceWorldInverseBuffer.SetData(commandList, (ReadOnlySpan<Matrix>)gather.WorldInverseMatrices.AsSpan(0, count));

        needUpload = false;
        UploadSkippedLastFrame = false;
    }

    private static Buffer CreateMatrixBuffer(GraphicsDevice device, int elementCount)
        => Buffer.New<Matrix>(device, elementCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);

    /// <summary>
    /// Releases the GPU buffers. Unlike the <see cref="InstancingUserArray"/> path, the engine's
    /// InstancingProcessor never disposes user-owned buffers, so without this a recreated scene
    /// would leak them until the graphics device goes down. Call it once the master entity is gone
    /// and no frame is in flight (e.g. after Game.Run returns); at Phase 3 this should be wired to
    /// component removal instead.
    /// </summary>
    public void Dispose()
    {
        foreach (var buffer in retiredLastFrame ?? []) buffer.Dispose();
        foreach (var buffer in retiredThisFrame ?? []) buffer.Dispose();
        retiredLastFrame = null;
        retiredThisFrame = null;

        InstanceWorldBuffer?.Dispose();
        InstanceWorldInverseBuffer?.Dispose();
        InstanceWorldBuffer = null!;
        InstanceWorldInverseBuffer = null!;
        InstanceCount = 0;
        needUpload = false;
    }
}

/// <summary>
/// The compositor hook that gives <see cref="FastBufferedEntityTransformInstancing"/> its two
/// main-thread, correctly-ordered entry points: buffer management in Collect (before Extract) and
/// upload in Draw (before the scene renderer, when inserted first in the renderer collection).
/// </summary>
public class InstancingBufferUploadRenderer : SceneRendererBase
{
    /// <summary>The instancing types this renderer manages buffers and uploads for.</summary>
    /// <remarks>
    /// DataMemberIgnore matters: SceneRendererBase is [DataContract(Inherited = true)], so without
    /// it Stride's assembly processor tries to generate a serializer for the target type and its
    /// raw GPU buffer fields, and fails the build.
    /// </remarks>
    [DataMemberIgnore]
    public List<FastBufferedEntityTransformInstancing> Targets { get; } = [];

    protected override void CollectCore(RenderContext context)
    {
        foreach (var target in Targets)
        {
            target.EnsureCapacity(context.GraphicsDevice);
        }
    }

    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        foreach (var target in Targets)
        {
            target.Upload(drawContext.CommandList);
        }
    }
}