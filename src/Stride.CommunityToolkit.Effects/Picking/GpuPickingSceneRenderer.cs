using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Effects.Picking;

/// <summary>
/// The compositor's half of GPU picking: on a frame with a request, draws the picking stage from
/// the main camera into a hidden target, scissored to the one pixel asked about, copies that pixel
/// into a staging texture and reads it back two frames later, when the GPU has long finished with
/// it, so the answer never stalls the frame.
/// </summary>
/// <remarks>
/// A camera renderer of its own, over the same camera slot as the main view, so the render system
/// culls and sorts a view for it - but only on frames with a request: idle, it adds no view, no
/// stage and no work. Game Studio's viewport does the same pass, reads it back at once and pays the
/// stall; a game cannot.
/// </remarks>
public sealed class GpuPickingSceneRenderer : SceneCameraRenderer
{
    /// <summary>The block the profiler shows the pass under.</summary>
    public static readonly ProfilingKey ProfilingKey = new("GpuPicking");

    private static readonly Color4 ProfileColor = new(0.9f, 0.4f, 0.7f, 1f);

    /// <summary>Staging textures in flight. Three slots give two frames between the copy and the read.</summary>
    private const int Ring = 3;

    private readonly Texture?[] _staging = new Texture?[Ring];
    private readonly Request?[] _inFlight = new Request?[Ring];
    private readonly Vector4[] _pixel = new Vector4[1];
    private int _frame;
    private int _sequence;

    // A parameterless constructor and settable properties, the shape every compositor renderer has:
    // the asset system builds one empty and fills its members, and Stride's analyzer holds a renderer
    // to that even when only code ever builds it

    /// <summary>The stage meshes are routed to for picking, already registered with the compositor.</summary>
    public required RenderStage Stage { get; init; }

    /// <summary>The mesh feature's half: the sub-feature that writes the ids and remembers the components.</summary>
    public required GpuPickingRenderFeature Feature { get; init; }

    /// <summary>The point to pick on the next frame, normalised over the viewport with (0, 0) at the top left; <see langword="null"/> asks for nothing.</summary>
    public Vector2? Pending { get; set; }

    /// <summary>Where the point comes from every frame while <see cref="Continuous"/> is on: the mouse, typically.</summary>
    public Func<Vector2>? FollowSource { get; set; }

    /// <summary>Whether to pick at <see cref="FollowSource"/> on every frame that has no explicit request.</summary>
    public bool Continuous { get; set; }

    /// <summary>The last answer read back, or <see langword="null"/> before the first one.</summary>
    public PickResult? Result { get; private set; }

    /// <inheritdoc/>
    protected override void CollectCore(RenderContext context)
    {
        if (Pending is null && Continuous && FollowSource is { } source)
        {
            Pending = source();
        }

        Feature.Active = Pending is not null;

        // Idle: no view for the render system to cull, no stage for it to sort
        if (Pending is null) return;

        base.CollectCore(context);

        RenderView.RenderStages.Add(Stage);
    }

    /// <inheritdoc/>
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        var slot = _frame % Ring;

        // The slot written two frames ago is read before this frame's request goes into its own
        ReadBack(drawContext, (slot + 1) % Ring);

        if (Pending is { } position && ResolveCamera(context) is not null)
        {
            using (context.PushRenderViewAndRestore(RenderView))
            {
                Render(context, drawContext, position, slot);
            }
        }

        Pending = null;
        _frame++;
    }

    private void Render(RenderContext context, RenderDrawContext drawContext, Vector2 position, int slot)
    {
        using var _ = drawContext.QueryManager.BeginProfile(ProfileColor, ProfilingKey);

        var width = Math.Max(1, (int)RenderView.ViewSize.X);
        var height = Math.Max(1, (int)RenderView.ViewSize.Y);
        var x = Math.Clamp((int)(position.X * width), 0, width - 1);
        var y = Math.Clamp((int)(position.Y * height), 0, height - 1);

        // The view's own size, so the pixel asked about is the pixel on screen, not a cell of a smaller grid
        var target = PushScopedResource(context.Allocator.GetTemporaryTexture2D(width, height, Stage.Output.RenderTargetFormat0));
        var depth = PushScopedResource(context.Allocator.GetTemporaryTexture2D(width, height, Stage.Output.DepthStencilFormat, TextureFlags.DepthStencil));
        var commandList = drawContext.CommandList;

        using (drawContext.PushRenderTargetsAndRestore())
        {
            // Cleared to id 0 and depth 1: the background reads as nothing
            commandList.Clear(target, Color4.Black);
            commandList.Clear(depth, DepthStencilClearOptions.DepthBuffer);
            commandList.ResourceBarrierTransition(target, BarrierLayout.RenderTarget);
            commandList.ResourceBarrierTransition(depth, BarrierLayout.DepthStencilWrite);
            commandList.SetRenderTargetAndViewport(depth, target);
            commandList.SetScissorRectangle(new Rectangle(x, y, 1, 1));
            context.RenderSystem.Draw(drawContext, RenderView, Stage);
            commandList.SetScissorRectangle(new Rectangle());
        }

        var staging = _staging[slot] ??= Texture.New2D(drawContext.GraphicsDevice, 1, 1, Stage.Output.RenderTargetFormat0, TextureFlags.None, 1, GraphicsResourceUsage.Staging);

        commandList.CopyRegion(target, 0, new ResourceRegion(x, y, 0, x + 1, y + 1, 1), staging, 0);

        _inFlight[slot] = new Request(position, x, y, width, height, RenderView.ViewProjection);
    }

    private void ReadBack(RenderDrawContext drawContext, int slot)
    {
        if (_inFlight[slot] is not { } request || _staging[slot] is not { } staging) return;

        _inFlight[slot] = null;

        if (!staging.GetData(drawContext.CommandList, _pixel)) return;

        Result = Decode(request, _pixel[0]);
    }

    private PickResult Decode(in Request request, Vector4 pixel)
    {
        var sequence = ++_sequence;
        var id = (int)MathF.Round(pixel.X);

        if (id == 0 || Feature.Find(id) is not { } component)
        {
            return new PickResult(sequence, request.Position, null, null, 0, 0, 0, pixel.W, null);
        }

        var packed = (int)MathF.Round(pixel.Z);

        // The pixel's centre, unprojected through the view that drew it, at the depth it wrote
        var clip = new Vector3((request.X + 0.5f) / request.Width * 2f - 1f, 1f - (request.Y + 0.5f) / request.Height * 2f, pixel.W);
        var inverse = Matrix.Invert(request.ViewProjection);
        var world = Vector3.TransformCoordinate(clip, inverse);

        return new PickResult(sequence, request.Position, component.Entity, component, packed / 4096, packed % 4096, (int)MathF.Round(pixel.Y), pixel.W, world);
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        foreach (var staging in _staging)
        {
            staging?.Dispose();
        }

        Array.Clear(_staging);
        base.Destroy();
    }

    /// <summary>A pick in flight: what was asked, which pixel that was, and the matrix to unproject it with.</summary>
    private readonly record struct Request(Vector2 Position, int X, int Y, int Width, int Height, Matrix ViewProjection);
}