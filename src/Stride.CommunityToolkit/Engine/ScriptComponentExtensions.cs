using Stride.Engine;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for <see cref="ScriptComponent"/>: the frame time, and the cameras the <see cref="GraphicsCompositor"/> renders with.
/// </summary>
public static class ScriptComponentExtensions
{
    /// <summary>
    /// Retrieves the time elapsed since the last game update in seconds.
    /// </summary>
    /// <param name="scriptComponent">The <see cref="ScriptComponent"/> used to access game timing information.</param>
    /// <returns>The time elapsed since the last game update, in seconds, as a single-precision floating-point number.</returns>
    public static float DeltaTime(this ScriptComponent scriptComponent)
        => (float)scriptComponent.Game.UpdateTime.Elapsed.TotalSeconds;

    /// <summary>
    /// The camera in the <see cref="GraphicsCompositor"/>'s slot of the given name, <see cref="CameraDefaults.MainCameraName"/>
    /// by default, or <see langword="null"/> when no slot has that name or the slot has no camera yet.
    /// </summary>
    /// <param name="scriptComponent">The script whose scene system holds the compositor.</param>
    /// <param name="cameraName">The name of the camera slot; <see cref="CameraDefaults.MainCameraName"/> when omitted.</param>
    /// <returns>The slot's <see cref="CameraComponent"/>, or <see langword="null"/>.</returns>
    /// <remarks>
    /// This reads the compositor's camera slots, which is what the scene is rendered with, not the scene's entities;
    /// <see cref="SceneExtensions.GetCamera(Scene, string)"/> searches those. A slot is bound to its camera once the
    /// camera component is enabled in a scene, which can be a frame or two after the game starts, so a script's
    /// first updates may get <see langword="null"/>.
    /// </remarks>
    public static CameraComponent? GetCamera(this ScriptComponent scriptComponent, string cameraName = CameraDefaults.MainCameraName)
    {
        foreach (var slot in scriptComponent.SceneSystem.GraphicsCompositor.Cameras)
        {
            if (slot.Name == cameraName) return slot.Camera;
        }

        return null;
    }

    /// <summary>
    /// The camera in the <see cref="GraphicsCompositor"/>'s first slot, whatever it is named, or <see langword="null"/>
    /// when the compositor has no camera slot or the slot has no camera yet.
    /// </summary>
    /// <param name="scriptComponent">The script whose scene system holds the compositor.</param>
    /// <returns>The first slot's <see cref="CameraComponent"/>, or <see langword="null"/>.</returns>
    /// <remarks>The usual choice when there is one camera; see <see cref="GetCamera"/> for when the slot is bound.</remarks>
    public static CameraComponent? GetFirstCamera(this ScriptComponent scriptComponent)
    {
        var cameras = scriptComponent.SceneSystem.GraphicsCompositor.Cameras;

        return cameras.Count > 0 ? cameras[0].Camera : null;
    }
}