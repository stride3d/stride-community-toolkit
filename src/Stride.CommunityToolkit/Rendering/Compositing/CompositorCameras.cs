using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Rendering.Compositing;

/// <summary>
/// Finds the camera a scene renderer should draw with, for renderers that sit outside the camera
/// renderer and so have no current camera on the context.
/// </summary>
/// <remarks>
/// Tries the compositor's first camera slot, then walks the renderer tree for a camera renderer.
/// Game Studio's scene editor leaves the slots empty and hands its camera to the top-level
/// renderer instead. Returns <see langword="null"/> when neither has a camera.
/// </remarks>
public static class CompositorCameras
{
    /// <summary>The camera of the current compositor: its first slot, or its first camera renderer.</summary>
    public static CameraComponent? Find(RenderContext context)
    {
        if (context.Tags.Get(GraphicsCompositor.Current) is not { } compositor) return null;

        if (compositor.Cameras.Count > 0 && compositor.Cameras[0].Camera is { } slotCamera) return slotCamera;

        return FromRenderer(compositor.Game);
    }

    private static CameraComponent? FromRenderer(ISceneRenderer? renderer)
    {
        switch (renderer)
        {
            // The editor's top-level renderer derives from this one and carries the editor camera here
            case SceneExternalCameraRenderer external:
                return external.ExternalCamera;

            case SceneCameraRenderer cameraRenderer:
                return cameraRenderer.Camera?.Camera;

            case SceneRendererCollection collection:
                foreach (var child in collection.Children)
                {
                    if (FromRenderer(child) is { } found) return found;
                }

                return null;

            default:
                return null;
        }
    }
}