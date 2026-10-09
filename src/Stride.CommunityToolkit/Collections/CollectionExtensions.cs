namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Methods that add several elements at once to an <see cref="ICollection{T}"/>, <see cref="Queue{T}"/> or <see cref="Stack{T}"/>.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="destination">The <see cref="ICollection{T}"/> to add items to.</param>
    /// <param name="collection">The collection whose elements should be added to the end of the <paramref name="destination"/>.</param>
    /// <remarks>
    /// This extension is useful for adding range functionality to collections like <see cref="HashSet{T}"/> that do not have <c>AddRange</c> by default.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> or <paramref name="collection"/> are <see langword="null"/>.</exception>
    public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            destination.Add(item);
        }
    }

    /// <summary>
    /// Enqueues the elements of the specified collection into the <see cref="Queue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="queue">The <see cref="Queue{T}"/> to which items will be added.</param>
    /// <param name="collection">The collection whose elements should be added to the <paramref name="queue"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="queue"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            queue.Enqueue(item);
        }
    }

    /// <summary>
    /// Pushes the elements of the specified collection onto the <see cref="Stack{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="stack">The <see cref="Stack{T}"/> to which items will be pushed.</param>
    /// <param name="collection">The collection whose elements should be pushed onto the <paramref name="stack"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="stack"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            stack.Push(item);
        }
    }
}
