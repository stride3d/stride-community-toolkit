using Stride.Engine;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for searching and retrieving <see cref="EntityComponent"/> instances
/// within an <see cref="Entity"/> hierarchy. This class includes methods for performing breadth-first
/// and depth-first searches to find components in children, descendants, and ancestors of an entity.
/// </summary>
/// <remarks>
/// The methods in this class support various search options, including:
/// <list type="bullet">
/// <item><description>Searching only in children or including the entity itself.</description></item>
/// <item><description>Including or excluding disabled components: an <see cref="ActivableEntityComponent"/> whose
/// <see cref="ActivableEntityComponent.Enabled"/> is <see langword="false"/> is skipped unless <c>includeDisabled</c>
/// is set. A component that cannot be disabled is always found.</description></item>
/// <item><description>Retrieving a single component or all components of a specified type.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// var component = entity.GetComponentInChildren&lt;MyComponent&gt;();
/// var allComponents = entity.GetComponentsInDescendants&lt;MyComponent&gt;();
/// </code>
/// </example>
/// <note type="note">
/// All methods throw <see cref="ArgumentNullException"/> if the provided entity is <c>null</c>.
/// </note>
public static class EntitySearchExtensions
{
    /// <summary>
    /// Searches the entity's descendants breadth-first for the first component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="entity">The <see cref="Entity"/> to search within.</param>
    /// <param name="includeDisabled">Whether an <see cref="ActivableEntityComponent"/> with <see cref="ActivableEntityComponent.Enabled"/> <see langword="false"/> counts.</param>
    /// <returns>The first component of type <typeparamref name="T"/> found in the entity's descendants, or <c>null</c> if none exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="entity"/> is <c>null</c>.</exception>
    public static T? GetComponentInChildren<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, BreadthFirst(entity, includeSelf: false), includeDisabled).FirstOrDefault();

    /// <summary>
    /// Performs a breadth-first search of the entity and its descendants for a component of the specified type.
    /// </summary>
    /// <inheritdoc cref="GetComponentInChildren{T}(Entity, bool)"/>
    public static T? GetComponentInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, BreadthFirst(entity, includeSelf: true), includeDisabled).FirstOrDefault();

    /// <summary>
    /// Gets all components of the specified type on the entity's direct children.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Whether an <see cref="ActivableEntityComponent"/> with <see cref="ActivableEntityComponent.Enabled"/> <see langword="false"/> counts.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInChildren<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, Children(entity), includeDisabled);

    /// <summary>
    /// Gets all components of the specified type on the entity and its direct children.
    /// </summary>
    /// <inheritdoc cref="GetComponentsInChildren{T}(Entity, bool)"/>
    public static IEnumerable<T> GetComponentsInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, Children(entity).Prepend(entity), includeDisabled);

    /// <summary>
    /// Performs a depth-first search of the entity's descendants for all components of the specified type.
    /// </summary>
    /// <inheritdoc cref="GetComponentsInChildren{T}(Entity, bool)"/>
    public static IEnumerable<T> GetComponentsInDescendants<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, DepthFirst(entity, includeSelf: false), includeDisabled);

    /// <summary>
    /// Performs a depth-first search of the entity and its descendants for all components of the specified type.
    /// </summary>
    /// <inheritdoc cref="GetComponentsInChildren{T}(Entity, bool)"/>
    public static IEnumerable<T> GetComponentsInDescendantsAndSelf<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, DepthFirst(entity, includeSelf: true), includeDisabled);

    /// <summary>
    /// Performs a depth-first search of the entity and then its descendants for the first component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Whether an <see cref="ActivableEntityComponent"/> with <see cref="ActivableEntityComponent.Enabled"/> <see langword="false"/> counts.</param>
    /// <returns>The component on the entity itself when it has one, otherwise the first found walking down each child in turn, or <c>null</c> if none exists.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInDescendantsAndSelf<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => entity.GetComponentsInDescendantsAndSelf<T>(includeDisabled).FirstOrDefault();

    /// <summary>
    /// Performs a search of the entity and its ancestors for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Whether an <see cref="ActivableEntityComponent"/> with <see cref="ActivableEntityComponent.Enabled"/> <see langword="false"/> counts.</param>
    /// <returns>The component, or <c>null</c> if none exists.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInParent<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => entity.GetComponentsInParent<T>(includeDisabled).FirstOrDefault();

    /// <summary>
    /// Performs a search of the entity and its ancestors for all components of the specified type.
    /// </summary>
    /// <inheritdoc cref="GetComponentsInChildren{T}(Entity, bool)"/>
    public static IEnumerable<T> GetComponentsInParent<T>(this Entity entity, bool includeDisabled = false) where T : EntityComponent
        => Components<T>(entity, SelfAndAncestors(entity), includeDisabled);

    /// <summary>
    /// The components of type <typeparamref name="T"/> on each entity of <paramref name="walk"/> in turn, the disabled
    /// ones only when asked for. The walks are lazy, so the null check happens here, at the call, not at the first step.
    /// </summary>
    private static IEnumerable<T> Components<T>(Entity entity, IEnumerable<Entity> walk, bool includeDisabled) where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Filter<T>(walk, includeDisabled);
    }

    private static IEnumerable<T> Filter<T>(IEnumerable<Entity> entities, bool includeDisabled) where T : EntityComponent
    {
        foreach (var entity in entities)
        {
            foreach (var component in entity.GetAll<T>())
            {
                if (includeDisabled || component is not ActivableEntityComponent { Enabled: false })
                    yield return component;
            }
        }
    }

    private static IEnumerable<Entity> Children(Entity entity)
    {
        foreach (var child in entity.Transform.Children)
            yield return child.Entity;
    }

    private static IEnumerable<Entity> BreadthFirst(Entity entity, bool includeSelf)
    {
        var queue = new Queue<Entity>();

        if (includeSelf)
            queue.Enqueue(entity);
        else
            foreach (var child in Children(entity)) queue.Enqueue(child);

        while (queue.TryDequeue(out var current))
        {
            yield return current;

            foreach (var child in current.Transform.Children)
                queue.Enqueue(child.Entity);
        }
    }

    private static IEnumerable<Entity> DepthFirst(Entity entity, bool includeSelf)
    {
        var stack = new Stack<Entity>();

        stack.Push(entity);

        while (stack.TryPop(out var current))
        {
            if (includeSelf)
                yield return current;

            includeSelf = true;

            // Pushed in reverse so the children come out in their own order
            var children = current.Transform.Children;

            for (var i = children.Count - 1; i >= 0; i--)
                stack.Push(children[i].Entity);
        }
    }

    private static IEnumerable<Entity> SelfAndAncestors(Entity entity)
    {
        for (var current = entity; current is not null; current = current.GetParent())
            yield return current;
    }
}
