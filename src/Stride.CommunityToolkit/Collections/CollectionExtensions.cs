namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Provides a set of extension methods for common collection types, including <see cref="ICollection{T}"/>, <see cref="Queue{T}"/>,
/// and <see cref="Stack{T}"/>. These methods offer additional functionality for efficiently adding or manipulating multiple elements in bulk.
/// </summary>
/// <remarks>
/// These extensions simplify common tasks such as adding multiple elements to collections, queues, and stacks.
/// While built-in methods like <see cref="ICollection{T}.Add"/> and <see cref="Queue{T}.Enqueue"/> only handle single elements,
/// these methods support adding multiple elements at once, improving code readability and reducing verbosity.
///
/// The collection types covered by these extensions include:
/// <list type="bullet">
/// <item><description><see cref="ICollection{T}"/>: Add multiple elements to collections in bulk.</description></item>
/// <item><description><see cref="Queue{T}"/>: Enqueue multiple elements at once.</description></item>
/// <item><description><see cref="Stack{T}"/>: Push multiple elements onto a stack.</description></item>
/// </list>
///
/// All methods are null-safe and will throw <see cref="ArgumentNullException"/> if the target collection or input collection is <see langword="null"/>.
/// </remarks>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the <see cref="ICollection{T}"/>, allowing for nullable elements.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection. If <typeparamref name="T"/> is a reference type, <typeparamref name="T?"/> allows nullable elements.</typeparam>
    /// <param name="destination">The <see cref="ICollection{T}"/> to add items to. Can contain <see langword="null"/> elements.</param>
    /// <param name="collection">
    /// The collection whose elements should be added to the end of the <paramref name="destination"/>.
    /// It can contain elements that are <see langword="null"/> if <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <remarks>
    /// This extension is useful for adding range functionality to collections like <see cref="HashSet{T}"/> or <see cref="Queue{T}"/> that do not have <c>AddRange</c> by default.
    /// If <typeparamref name="T"/> is a reference type, <typeparamref name="T?"/> explicitly allows <see langword="null"/> values.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="destination"/> or <paramref name="collection"/> are <see langword="null"/>.</exception>
    public static void AddRange<T>(this ICollection<T?> destination, IEnumerable<T?> collection)
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
    /// <param name="queue">
    /// The <see cref="Queue{T}"/> to which items will be added. This collection can accept <see langword="null"/> elements,
    /// if <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <param name="collection">
    /// The collection whose elements should be added to the <paramref name="queue"/>. It can contain elements that are <see langword="null"/>,
    /// if <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="queue"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static void EnqueueRange<T>(this Queue<T?> queue, IEnumerable<T?> collection)
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
    /// <param name="stack">
    /// The <see cref="Stack{T}"/> to which items will be pushed. This stack can accept <see langword="null"/> elements,
    /// if <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <param name="collection">
    /// The collection whose elements should be pushed onto the <paramref name="stack"/>. It can contain elements that are <see langword="null"/>,
    /// if <typeparamref name="T"/> is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="stack"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static void PushRange<T>(this Stack<T?> stack, IEnumerable<T?> collection)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(collection);

        foreach (var item in collection)
        {
            stack.Push(item);
        }
    }
}
