using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// A camera that draws into a texture instead of the window: the texture, the camera, and the
/// compositor pieces that join them, as <see cref="RenderTextureCameraExtensions.AddRenderTextureCamera"/>
/// built them. Dispose it to take the camera out of the compositor again and release the texture.
/// </summary>
/// <remarks>
/// The texture is whatever a texture is good for: the emissive map of a monitor's material, the fill
/// of a shape, a sprite. It holds the previous frame's picture, because the feed draws after the main
/// view - one frame of lag nobody can see, and the price of the main view being able to show it.
/// </remarks>
public sealed class RenderTextureCamera : IDisposable
{
    private readonly GraphicsCompositor _compositor;
    private bool _disposed;

    internal RenderTextureCamera(GraphicsCompositor compositor, Texture texture, Entity entity, CameraComponent camera, SceneCameraSlot slot, SceneCameraRenderer renderer, ForwardRenderer forward)
    {
        _compositor = compositor;
        Texture = texture;
        Entity = entity;
        Camera = camera;
        Slot = slot;
        Renderer = renderer;
        Forward = forward;
    }

    /// <summary>The texture the camera draws into, a render target and a shader resource in one.</summary>
    public Texture Texture { get; }

    /// <summary>The entity carrying the camera: move it, parent it, aim it like any other.</summary>
    public Entity Entity { get; }

    /// <summary>The camera component, for its projection, clip planes and field of view.</summary>
    public CameraComponent Camera { get; }

    /// <summary>The compositor slot the camera is bound to, one of its own.</summary>
    public SceneCameraSlot Slot { get; }

    /// <summary>The renderer in the compositor's top-level collection that draws this feed.</summary>
    public SceneCameraRenderer Renderer { get; }

    /// <summary>
    /// The forward renderer behind the feed, sharing the compositor's stages with the main view. Its
    /// <see cref="ForwardRenderer.PostEffects"/> is where a feed gets its own look.
    /// </summary>
    public ForwardRenderer Forward { get; }

    /// <summary>Takes the feed out of the compositor and releases the texture. The camera entity is left where it is.</summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        if (_compositor.Game is SceneRendererCollection renderers)
        {
            renderers.Children.Remove(Renderer);
        }

        _compositor.Cameras.Remove(Slot);
        Renderer.Dispose();
        Texture.Dispose();
    }
}