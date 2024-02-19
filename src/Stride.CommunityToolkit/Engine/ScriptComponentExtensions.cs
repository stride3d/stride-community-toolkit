using Stride.Engine;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

public static class ScriptComponentExtensions
{
    /// <summary>
    /// Retrieves the time elapsed since the last game update in seconds.
    /// </summary>
    /// <param name="scriptComponent">The script component used to access game timing information.</param>
    /// <returns>The delta time in seconds as a single-precision floating-point number.</returns>
    public static float DeltaTime(this ScriptComponent scriptComponent)
    {
        return (float)scriptComponent.Game.UpdateTime.Elapsed.TotalSeconds;
    }

    /// <summary>
    /// Retrieves the camera named "Main" from the <see cref="GraphicsCompositor"/>. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <returns>The <see cref="CameraComponent"/> named "Main", if found; otherwise, null.</returns>
    [Obsolete("Use GetGCCamera instead or use Scene.GetCamera")]
    public static CameraComponent? GetCamera(this ScriptComponent scriptComponent)
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
    /// Retrieves the camera named "Main" from the <see cref="GraphicsCompositor"/>. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <returns>The <see cref="CameraComponent"/> named "Main", if found; otherwise, null.</returns>
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
    /// Retrieves the camera from the <see cref="GraphicsCompositor"/> with the specified name. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <param name="cameraName">The name of the camera to retrieve.</param>
    /// <returns>The <see cref="CameraComponent"/> with the given name, if found; otherwise, null.</returns>
    [Obsolete("Use GetGCCamera instead or use Scene.GetCamera")]
    public static CameraComponent? GetCamera(this ScriptComponent scriptComponent, string cameraName)
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
    /// Retrieves the camera from the <see cref="GraphicsCompositor"/> with the specified name. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <param name="cameraName">The name of the camera to retrieve.</param>
    /// <returns>The <see cref="CameraComponent"/> with the given name, if found; otherwise, null.</returns>
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
    /// Gets the first camera from the <see cref="GraphicsCompositor"/>. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <returns>The <see cref="CameraComponent"/> with the given name, if found; otherwise, null.</returns>
    [Obsolete("Use GetFirstGCCamera instead or use Scene.GetCamera")]
    public static CameraComponent GetFirstCamera(this ScriptComponent scriptComponent)
    {
        SceneCameraSlotCollection cameraCollection = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        return cameraCollection[0].Camera;
    }

    /// <summary>
    /// Gets the first camera from the <see cref="GraphicsCompositor"/>. Note that the camera might not be available during the first 2-3 frames.
    /// </summary>
    /// <remarks>
    /// Ensure that the GraphicsCompositor is initialized with cameras; otherwise, this method will fail.
    /// </remarks>
    /// <param name="scriptComponent">The script component from which to access the GraphicsCompositor.</param>
    /// <returns>The <see cref="CameraComponent"/> with the given name, if found; otherwise, null.</returns>
    public static CameraComponent GetFirstGCCamera(this ScriptComponent scriptComponent)
    {
        SceneCameraSlotCollection cameraCollection = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        return cameraCollection[0].Camera;
    }
}