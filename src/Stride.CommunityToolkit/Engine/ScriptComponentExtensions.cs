using Stride.Engine;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for <see cref="ScriptComponent"/> to enhance interactions with game timing and cameras in the <see cref="GraphicsCompositor"/>.
/// </summary>
public static class ScriptComponentExtensions
{
    /// <summary>
    /// Retrieves the time elapsed since the last game update in seconds.
    /// </summary>
    /// <param name="scriptComponent">The <see cref="ScriptComponent"/> used to access game timing information.</param>
    /// <returns>The time elapsed since the last game update, in seconds, as a single-precision floating-point number.</returns>
    public static float DeltaTime(this ScriptComponent scriptComponent)
    {
        return (float)scriptComponent.Game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Retrieves the camera named "Main" from the <see cref="GraphicsCompositor"/>.
    /// Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <param name="scriptComponent">The <see cref="ScriptComponent"/> from which to access the <see cref="GraphicsCompositor"/>.</param>
    /// <returns>The <see cref="CameraComponent"/> named "Main", if found; otherwise, null.</returns>
    /// <remarks>
    /// Ensure that the <see cref="GraphicsCompositor"/> is initialized with cameras before calling this method, or it may return null.
    /// </remarks>
    public static CameraComponent? GetGCCamera(this ScriptComponent scriptComponent)
    {
        var cameraCollection = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        foreach (var sceneCamera in cameraCollection)
        {
            if (sceneCamera.Name == "Main")
            {
                return sceneCamera.Camera;
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves a camera from the <see cref="GraphicsCompositor"/> with the specified name.
    /// Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <param name="scriptComponent">The <see cref="ScriptComponent"/> from which to access the <see cref="GraphicsCompositor"/>.</param>
    /// <param name="cameraName">The name of the camera to retrieve.</param>
    /// <returns>The <see cref="CameraComponent"/> with the specified name, if found; otherwise, null.</returns>
    /// <remarks>
    /// Ensure that the <see cref="GraphicsCompositor"/> is initialized with cameras before calling this method, or it may return null.
    /// </remarks>
    public static CameraComponent? GetGCCamera(this ScriptComponent scriptComponent, string cameraName)
    {
        var cameraCollection = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        foreach (var sceneCamera in cameraCollection)
        {
            if (sceneCamera.Name == cameraName)
            {
                return sceneCamera.Camera;
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the first camera from the <see cref="GraphicsCompositor"/>.
    /// Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <param name="scriptComponent">The <see cref="ScriptComponent"/> from which to access the <see cref="GraphicsCompositor"/>.</param>
    /// <returns>The first <see cref="CameraComponent"/> in the camera collection.</returns>
    /// <remarks>
    /// Ensure that the <see cref="GraphicsCompositor"/> is initialized with cameras before calling this method.
    /// </remarks>
    public static CameraComponent GetFirstGCCamera(this ScriptComponent scriptComponent)
    {
        var cameraCollection = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        return cameraCollection[0].Camera;
    }
}