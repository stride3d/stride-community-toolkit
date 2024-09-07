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
    /// This method searches through the scene's entities and returns the first camera it finds.
    /// It performs a recursive search through child entities as well.
    /// </remarks>
    public static CameraComponent? GetCamera(this Scene scene)
    {
        var entities = scene.Entities;

        CameraComponent? camera = null;

        foreach (var entity in entities)
        {
            camera = entity.GetComponentInChildren<CameraComponent>();

            if (camera != null)
            {
                break;
            }
        }

        return camera;
    }

    /// <summary>
    /// Retrieves the first <see cref="CameraComponent"/> in the scene that belongs to an entity with the specified name.
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> in which to search for the camera.</param>
    /// <param name="name">The name of the <see cref="Entity"/> containing the desired camera.</param>
    /// <returns>
    /// The first <see cref="CameraComponent"/> found with the specified entity name, or <c>null</c> if no matching camera is found.
    /// </returns>
    /// <remarks>
    /// This method searches through the scene's entities for a camera that belongs to an entity with the given name.
    /// It performs a recursive search through child entities as well.
    /// </remarks>
    public static CameraComponent? GetCamera(this Scene scene, string name)
    {
        var entities = scene.Entities;

        CameraComponent? camera = null;

        foreach (var entity in entities)
        {
            camera = entity.GetComponentInChildren<CameraComponent>();

            if (camera != null && camera.Entity.Name == name)
            {
                break;
            }
        }

        return camera;
    }
}
