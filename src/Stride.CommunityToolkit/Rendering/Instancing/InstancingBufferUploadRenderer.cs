using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Instancing;

/// <summary>
/// Manages and uploads the GPU buffers of every registered <see cref="BufferedEntityInstancing"/>.
/// </summary>
/// <remarks>
/// <para>
/// A buffered instancing needs two things that can only happen on the main thread at specific points
/// in the frame, which is what this renderer provides. Stride's frame order is: entity processors
/// gather, then the compositor collects, then render features extract and prepare, then the
/// compositor draws. Buffers are therefore created or grown during collect - before the render
/// feature reads them - and uploaded during draw.
/// </para>
/// <para>
/// It must run before the renderer that draws the scene, so the upload is recorded ahead of the draw
/// calls that read it; otherwise the frame renders the previous frame's positions.
/// <c>AddInstancingBufferUpload</c> inserts it in the right place.
/// </para>
/// </remarks>
public class InstancingBufferUploadRenderer : SceneRendererBase
{
    /// <summary>Gets the instancings whose buffers this renderer manages.</summary>
    /// <remarks>
    /// <see cref="DataMemberIgnoreAttribute"/> matters: <see cref="SceneRendererBase"/> is a data
    /// contract, and without it Stride's assembly processor tries to generate a serializer for the
    /// GPU buffers behind these and fails the build.
    /// </remarks>
    [DataMemberIgnore]
    public List<BufferedEntityInstancing> Targets { get; } = [];

    /// <inheritdoc />
    protected override void CollectCore(RenderContext context)
    {
        base.CollectCore(context);

        foreach (var target in Targets)
        {
            target.EnsureCapacity(context.GraphicsDevice);
        }
    }

    /// <inheritdoc />
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
    {
        foreach (var target in Targets)
        {
            target.Upload(drawContext.CommandList);
        }
    }
}