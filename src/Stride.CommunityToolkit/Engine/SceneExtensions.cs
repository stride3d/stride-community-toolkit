using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="Scene"/> class to enhance camera-related functionality.
/// </summary>
public static class SceneExtensions
{
    /// <summary>
    /// Retrieves the first <see cref="CameraComponent"/> found in the scene.
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> in which to search for the camera.</param>
    /// <returns>
    /// The first <see cref="CameraComponent"/> found in the scene, or <c>null</c> if no camera is present.
    /// </returns>
    /// <remarks>
    /// Searches every top-level entity of the scene and all of its descendants; disabled cameras count.
    /// </remarks>
    public static CameraComponent? GetCamera(this Scene scene)
        => scene.GetComponentInScene<CameraComponent>(includeChildScenes: false);

    /// <summary>
    /// Retrieves the first <see cref="CameraComponent"/> in the scene that belongs to an entity with the specified name.
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> in which to search for the camera.</param>
    /// <param name="name">The name of the <see cref="Entity"/> containing the desired camera.</param>
    /// <returns>
    /// The first <see cref="CameraComponent"/> found with the specified entity name, or <c>null</c> if no matching camera is found.
    /// </returns>
    /// <remarks>
    /// Searches every top-level entity of the scene and all of its descendants; disabled cameras count.
    /// </remarks>
    public static CameraComponent? GetCamera(this Scene scene, string name)
        => scene.GetComponentsInScene<CameraComponent>(includeChildScenes: false).FirstOrDefault(camera => camera.Entity.Name == name);
}
