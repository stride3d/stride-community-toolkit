namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Extension Methods for collection types.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="ICollection{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="destination">The <see cref="ICollection{T}"/> to add items to.</param>
    /// <param name="collection">
    /// The collection whose elements should be added to the end of the <paramref name="destination"/>. It can contain elements that are <see langword="null"/>, if type <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="destination"/> or <paramref name="collection"/> are <see langword="null"/>.</exception>
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
    /// Enqueues the elements of the specified collection to the <see cref="Queue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="queue">The <see cref="Queue{T}"/> to add items to.</param>
    /// <param name="collection">
    /// The collection whose elements should be added to the <paramref name="queue"/>. It can contain elements that are <see langword="null"/>, if type <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="queue"/> or <paramref name="collection"/> are <see langword="null"/>.</exception>
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
    /// Pushes the elements of the specified collection to the <see cref="Stack{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="stack">The <see cref="Stack{T}"/> to add items to.</param>
    /// <param name="collection">
    /// The collection whose elements should be pushed on to the <paramref name="stack"/>. It can contain elements that are <see langword="null"/>, if type <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="stack"/> or <paramref name="collection"/> are <see langword="null"/>.</exception>
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
