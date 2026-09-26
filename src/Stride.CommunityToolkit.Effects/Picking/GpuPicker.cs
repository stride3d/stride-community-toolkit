using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Effects.Picking;

/// <summary>
/// Answers "what is under this screen point" from the rendered frame rather than from physics: a
/// pass draws every mesh's id instead of its colour, and the pixel asked about is read back. No
/// collider is needed, and the answer is per pixel - an instanced flock, a procedural model, a
/// skinned character, all pickable. The answer arrives two frames after the request.
/// </summary>
/// <remarks>
/// <para>
/// Ask with <see cref="Request"/> once, or set <see cref="Continuous"/> to follow the mouse every
/// frame, and read <see cref="Result"/>; its <see cref="PickResult.Sequence"/> says when there is
/// a new one. The pass runs only on frames with a request, and costs the draw submission of the
/// meshes in view once more, scissored to a single pixel on the GPU.
/// </para>
/// <para>
/// Limits: meshes only - sprites, UI, particles and shapes are not picked; a cut-out material's
/// transparent pixels pick as solid, because only the pixel stage is swapped; the picker reads the
/// main camera, so a feed drawn into a texture needs its own.
/// </para>
/// </remarks>
public sealed class GpuPicker : IDisposable
{
    private readonly GraphicsCompositor _compositor;
    private readonly MeshRenderFeature _meshFeature;
    private readonly RenderStage _stage;
    private readonly GpuPickingRenderFeature _feature;
    private readonly SimpleGroupToRenderStageSelector _selector;
    private bool _disposed;

    internal GpuPicker(GraphicsCompositor compositor, MeshRenderFeature meshFeature, RenderStage stage, GpuPickingRenderFeature feature,
        SimpleGroupToRenderStageSelector selector, GpuPickingSceneRenderer renderer)
    {
        _compositor = compositor;
        _meshFeature = meshFeature;
        _stage = stage;
        _feature = feature;
        _selector = selector;
        Renderer = renderer;
    }

    /// <summary>The renderer doing the pass, for anyone who wants to move it in the compositor.</summary>
    public GpuPickingSceneRenderer Renderer { get; }

    /// <summary>The last answer, or <see langword="null"/> before the first request has been answered.</summary>
    public PickResult? Result => Renderer.Result;

    /// <summary>
    /// Whether to pick at the mouse on every frame that has no explicit request, so
    /// <see cref="Result"/> follows the pointer for hover effects. Off by default.
    /// </summary>
    public bool Continuous
    {
        get => Renderer.Continuous;
        set => Renderer.Continuous = value;
    }

    /// <summary>Which render groups are pickable. Everything by default; narrow it to keep decorative geometry out of the pass.</summary>
    public RenderGroupMask Pickable
    {
        get => _selector.RenderGroup;
        set => _selector.RenderGroup = value;
    }

    /// <summary>
    /// Asks what is under a screen point. The answer lands in <see cref="Result"/> two frames later.
    /// One request per frame; a second in the same frame replaces the first.
    /// </summary>
    /// <param name="screenPosition">The point, normalised over the viewport with (0, 0) at the top left and (1, 1) at the bottom right, as <c>Input.MousePosition</c> gives it.</param>
    public void Request(Vector2 screenPosition) => Renderer.Pending = screenPosition;

    /// <summary>Takes the pass out of the compositor and the mesh feature.</summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_compositor.Game is SceneRendererCollection renderers)
        {
            renderers.Children.Remove(Renderer);
        }

        _meshFeature.RenderStageSelectors.Remove(_selector);
        _meshFeature.RenderFeatures.Remove(_feature);

        // The stage stays: the render system cannot remove a stage once added (it throws), and an
        // unrouted stage costs nothing
        Renderer.Dispose();
    }
}