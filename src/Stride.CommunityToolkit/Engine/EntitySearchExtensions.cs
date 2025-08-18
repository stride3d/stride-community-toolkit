using Stride.CommunityToolkit.Collections;
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
/// <item><description>Including or excluding disabled components.</description></item>
/// <item><description>Retrieving a single component or all components of a specified type.</description></item>
/// </list>
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// var component = entity.GetComponentInChildrenBFS&lt;MyComponent&gt;();
/// var allComponents = entity.GetComponentsInDescendants&lt;MyComponent&gt;();
/// </code>
/// </example>
/// <note type="note">
/// All methods throw <see cref="ArgumentNullException"/> if the provided entity is <c>null</c>.
/// </note>
public static class EntitySearchExtensions
{
    /// <summary>
    /// Searches the entity's children using a breadth-first search (BFS) to find the first component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="entity">The <see cref="Entity"/> to search within.</param>
    /// <returns>The first component of type <typeparamref name="T"/> found in the entity's children, or <c>null</c> if none exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="entity"/> is <c>null</c>.</exception>
    public static T? GetComponentInChildrenBFS<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Breadth-first search (BFS)
        var queue = new Queue<Entity>();

        queue.EnqueueRange(entity.GetChildren());

        return GetComponentInChildrenCore<T>(queue);
    }

    /// <summary>
    /// Performs a breadth first search of the entity and it's children for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The component or null if does no exist.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInChildrenAndSelf<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.Enqueue(entity);

        return GetComponentInChildrenCore<T>(queue);
    }

    private static T? GetComponentInChildrenCore<T>(Queue<Entity> queue)
        where T : EntityComponent
    {
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            var component = current.Get<T>();

            if (component != null)
            {
                return component;
            }

            var children = current.Transform.Children;

            for (int i = 0; i < children.Count; i++)
            {
                queue.Enqueue(children[i].Entity);
            }
        }

        return null;
    }

    /// <summary>
    /// Performs a breadth first search of the entities children for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>The component or null if does no exist.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInChildren<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.EnqueueRange(entity.GetChildren());

        return GetComponentInChildrenCore<T>(includeDisabled, queue);
    }

    /// <summary>
    /// Performs a breadth first search of the entity and it's children for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>The component or null if does no exist.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.Enqueue(entity);
        return GetComponentInChildrenCore<T>(includeDisabled, queue);
    }

    private static T? GetComponentInChildrenCore<T>(bool includeDisabled, Queue<Entity> queue)
        where T : ActivableEntityComponent
    {
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            var component = current.Get<T>();

            if (component != null && (component.Enabled || includeDisabled))
            {
                return component;
            }

            var children = current.Transform.Children;

            for (int i = 0; i < children.Count; i++)
            {
                queue.Enqueue(children[i].Entity);
            }
        }

        return null;
    }

    /// <summary>
    /// Performs a depth first search of the entities children for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInChildren<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.EnqueueRange(entity.GetChildren());

        return GetComponentsInChildrenCore<T>(queue);
    }

    /// <summary>
    /// Performs a depth first search of the entity and it's children for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInChildrenAndSelf<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.Enqueue(entity);
        queue.EnqueueRange(entity.GetChildren());

        return GetComponentsInChildrenCore<T>(queue);
    }

    private static IEnumerable<T> GetComponentsInChildrenCore<T>(Queue<Entity> queue)
        where T : EntityComponent
    {
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var component in current.GetAll<T>())
            {
                yield return component;
            }
        }
    }

    /// <summary>
    /// Performs a depth first search of the entities children for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInChildren<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.EnqueueRange(entity.GetChildren());

        return GetComponentsInChildrenCore<T>(queue);
    }

    /// <summary>
    /// Performs a depth first search of the entity and it's children for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //breadth first
        var queue = new Queue<Entity>();
        queue.Enqueue(entity);
        queue.EnqueueRange(entity.GetChildren());

        return GetComponentsInChildrenCore<T>(queue);
    }

    private static IEnumerable<T> GetComponentsInChildrenCore<T>(Queue<Entity> queue, bool includeDisabled)
        where T : ActivableEntityComponent
    {
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var component in current.GetAll<T>())
            {
                if (component.Enabled || includeDisabled)
                {
                    yield return component;
                }
            }
        }
    }

    /// <summary>
    /// Performs a depth first search of the entities decendants for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInDescendants<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //depth first
        var stack = new Stack<Entity>();
        stack.Push(entity);

        return GetComponentsInDescendantsCore<T>(stack, false);
    }

    /// <summary>
    /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInDescendantsAndSelf<T>(this Entity entity)
        where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //depth first
        var stack = new Stack<Entity>();
        stack.Push(entity);

        return GetComponentsInDescendantsCore<T>(stack, true);
    }

    private static IEnumerable<T> GetComponentsInDescendantsCore<T>(Stack<Entity> stack, bool includeSelf)
        where T : EntityComponent
    {
        var includeComponents = includeSelf;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (includeComponents)
            {
                foreach (var component in current.GetAll<T>())
                {
                    yield return component;
                }
            }

            var children = current.Transform.Children;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i].Entity);
            }
            includeComponents = true;
        }
    }

    /// <summary>
    /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInDescendants<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //depth first
        var stack = new Stack<Entity>();
        stack.Push(entity);

        return GetComponentsInDescendantsCore<T>(stack, includeDisabled, false);
    }

    /// <summary>
    /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInDescendantsAndSelf<T>(this Entity entity, bool includeDisabled = false)
        where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        //depth first
        var stack = new Stack<Entity>();
        stack.Push(entity);

        return GetComponentsInDescendantsCore<T>(stack, includeDisabled, true);
    }

    private static IEnumerable<T> GetComponentsInDescendantsCore<T>(Stack<Entity> stack, bool includeDisabled, bool includeSelf)
        where T : ActivableEntityComponent
    {
        var includeComponents = includeSelf;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (includeComponents)
            {
                foreach (var component in current.GetAll<T>())
                {
                    if (component.Enabled || includeDisabled)
                    {
                        yield return component;
                    }
                }
            }

            var children = current.Transform.Children;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                stack.Push(children[i].Entity);
            }
            includeComponents = true;
        }
    }

    /// <summary>
    /// Performs a search of the entity and it's ancestors for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The component or <c>null</c> if does no exist.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInParent<T>(this Entity entity) where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        var current = entity;

        do
        {
            var component = current.Get<T>();

            if (component != null)
            {
                return component;
            }

        } while ((current = current.GetParent()) != null);

        return null;
    }

    /// <summary>
    /// Performs a search of the entity and it's ancestors for a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>The component or <c>null</c> if does no exist.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static T? GetComponentInParent<T>(this Entity entity, bool includeDisabled = false) where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        var current = entity;

        do
        {
            var component = current.Get<T>();

            if (component != null && (component.Enabled || includeDisabled))
            {
                return component;
            }

        } while ((current = current.GetParent()) != null);

        return null;
    }

    /// <summary>
    /// Performs a search of the entity and it's ancestors for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInParent<T>(this Entity entity) where T : EntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        var current = entity;

        do
        {
            foreach (var component in current.GetAll<T>())
            {
                yield return component;
            }


        } while ((current = current.GetParent()) != null);
    }

    /// <summary>
    /// Performs a search of the entity and it's ancestors for all components of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of component.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
    /// <returns>An iteration on the components.</returns>
    /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
    public static IEnumerable<T> GetComponentsInParent<T>(this Entity entity, bool includeDisabled = false) where T : ActivableEntityComponent
    {
        ArgumentNullException.ThrowIfNull(entity);

        var current = entity;

        do
        {
            foreach (var component in current.GetAll<T>())
            {
                if (component.Enabled || includeDisabled)
                {
                    yield return component;
                }
            }


        } while ((current = current.GetParent()) != null);
    }
}