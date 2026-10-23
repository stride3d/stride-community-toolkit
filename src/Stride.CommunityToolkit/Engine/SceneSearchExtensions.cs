using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Finds components anywhere in a scene or in the whole running game, the scene-wide counterpart of
/// <see cref="EntitySearchExtensions"/>.
/// </summary>
/// <remarks>
/// These walk every entity each time they are called, so they are for a lookup at start or on an event,
/// not for every frame. A manager that many scripts need is better registered once as a service and
/// read back with <c>Services.GetService&lt;T&gt;()</c>. Disabled components are included; filter on
/// <c>Enabled</c> where that matters.
/// </remarks>
public static class SceneSearchExtensions
{
    /// <summary>
    /// Every component of type <typeparamref name="T"/> on any entity of the scene, its children and,
    /// by default, its child scenes.
    /// </summary>
    /// <typeparam name="T">The component type; derived types match too.</typeparam>
    /// <param name="scene">The scene to search.</param>
    /// <param name="includeChildScenes">Whether the scenes under <see cref="Scene.Children"/> are searched as well.</param>
    /// <returns>The components, in scene order; empty when there are none.</returns>
    public static IEnumerable<T> GetComponentsInScene<T>(this Scene scene, bool includeChildScenes = true) where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(scene);

        foreach (var entity in scene.Entities)
        {
            foreach (var component in entity.GetComponentsInDescendantsAndSelf<T>(includeDisabled: true))
            {
                yield return component;
            }
        }

        if (!includeChildScenes) yield break;

        foreach (var child in scene.Children)
        {
            foreach (var component in child.GetComponentsInScene<T>(includeChildScenes: true))
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// The first component of type <typeparamref name="T"/> in the scene, or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The component type; derived types match too.</typeparam>
    /// <param name="scene">The scene to search.</param>
    /// <param name="includeChildScenes">Whether the scenes under <see cref="Scene.Children"/> are searched as well.</param>
    /// <returns>The first match in scene order, or <see langword="null"/>.</returns>
    public static T? GetComponentInScene<T>(this Scene scene, bool includeChildScenes = true) where T : EntityComponent
        => scene.GetComponentsInScene<T>(includeChildScenes).FirstOrDefault();

    /// <summary>
    /// Every component of type <typeparamref name="T"/> on any entity the game is running, across every
    /// loaded scene.
    /// </summary>
    /// <typeparam name="T">The component type; derived types match too.</typeparam>
    /// <param name="game">The game.</param>
    /// <returns>The components, in the order the engine registered their entities; empty when there are none.</returns>
    /// <remarks>
    /// Reads the scene instance, so it covers every scene that is loaded, child scenes included, and only
    /// entities that are in a scene: an entity built but not yet added is not found.
    /// </remarks>
    public static IEnumerable<T> GetComponentsInScene<T>(this IGame game) where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(game);

        var sceneInstance = game.GetSceneSystem().SceneInstance;

        if (sceneInstance is null) yield break;

        foreach (var entity in sceneInstance)
        {
            foreach (var component in entity.OfType<T>())
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// The first component of type <typeparamref name="T"/> anywhere in the running game, or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The component type; derived types match too.</typeparam>
    /// <param name="game">The game.</param>
    /// <returns>The first match, or <see langword="null"/>.</returns>
    public static T? GetComponentInScene<T>(this IGame game) where T : EntityComponent
        => game.GetComponentsInScene<T>().FirstOrDefault();
}