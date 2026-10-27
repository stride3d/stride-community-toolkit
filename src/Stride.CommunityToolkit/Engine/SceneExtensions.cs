using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="Scene"/> class: finding its camera, and finding an entity by name.
/// </summary>
public static class SceneExtensions
{
    /// <summary>
    /// Finds the first entity with the given name among the scene's top-level entities and, with
    /// <paramref name="recursive"/>, among their descendants too.
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> to search.</param>
    /// <param name="name">The entity name to look for.</param>
    /// <param name="recursive">
    /// <see langword="false"/>, the default, searches only the scene's top-level entities; <see langword="true"/>
    /// also searches every child, grandchild and so on, in the order they appear.
    /// </param>
    /// <returns>The first entity with that name, or <see langword="null"/> if none has it.</returns>
    /// <remarks>
    /// Search once and keep the result, for example in <c>Start</c>; searching by name every frame is the
    /// slow way to reach an entity you could hold in a field. From an entity that is already in the scene,
    /// <see cref="EntityExtensions.FindEntity(Entity, string)"/> and
    /// <see cref="EntityExtensions.FindEntityRecursive(Entity, string)"/> call this on its scene.
    /// </remarks>
    public static Entity? FindEntity(this Scene scene, string name, bool recursive = false)
    {
        ArgumentNullException.ThrowIfNull(scene);

        foreach (var entity in scene.Entities)
        {
            if (entity.Name == name) return entity;

            if (recursive && entity.FindChild(name) is { } child) return child;
        }

        return null;
    }

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
    {
        foreach (var camera in scene.GetComponentsInScene<CameraComponent>(includeChildScenes: false))
        {
            if (camera.Entity.Name == name) return camera;
        }

        return null;
    }
}