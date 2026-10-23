using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Extension methods for <see cref="IEnumerable{EntityComponent}"/> and <see cref="IEnumerable{ActivableEntityComponent}"/>.
/// </summary>
public static class EntityComponentCollectionExtensions
{
    /// <summary>
    /// Enables all <see cref="ActivableEntityComponent"/> in the collection.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="components">A collection of <see cref="ActivableEntityComponent"/> to enable.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="components"/> is <see langword="null"/>.</exception>
    public static void Enable<T>(this IEnumerable<T> components) where T : ActivableEntityComponent
        => components.SetEnabled(_ => true);

    /// <summary>
    /// Disables all <see cref="ActivableEntityComponent"/> in the collection.
    /// </summary>
    /// <inheritdoc cref="Enable{T}(IEnumerable{T})"/>
    public static void Disable<T>(this IEnumerable<T> components) where T : ActivableEntityComponent
        => components.SetEnabled(_ => false);

    /// <summary>
    /// Toggles the <see cref="ActivableEntityComponent.Enabled"/> state of all <see cref="ActivableEntityComponent"/> in the collection.
    /// </summary>
    /// <inheritdoc cref="Enable{T}(IEnumerable{T})"/>
    public static void Toggle<T>(this IEnumerable<T> components) where T : ActivableEntityComponent
        => components.SetEnabled(enabled => !enabled);

    private static void SetEnabled<T>(this IEnumerable<T> components, Func<bool, bool> next) where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(components);

        foreach (var component in components)
            component.Enabled = next(component.Enabled);
    }
}
