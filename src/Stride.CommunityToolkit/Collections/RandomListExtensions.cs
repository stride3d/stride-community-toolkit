namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Provides extension methods for the <see cref="Random"/> class to facilitate random selection and shuffling of collections.
/// </summary>
/// <remarks>
/// These extensions add functionality for randomly selecting an element from a collection or array, and for shuffling elements in place.
/// Useful for games, simulations, or any scenario requiring random operations on lists or arrays.
/// </remarks>
public static class RandomListExtensions
{
    /// <summary>
    /// Selects a random item from a given collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="random">An instance of <see cref="Random"/> used to generate the random selection.</param>
    /// <param name="collection">The collection to choose an item from.</param>
    /// <returns>A randomly chosen item from the <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="random"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="collection"/> is empty.
    /// </exception>
    public static T Choose<T>(this Random random, IList<T> collection)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(collection);

        return collection[random.Next(collection.Count)];
    }

    /// <summary>
    /// Selects a random item from a given array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="random">An instance of <see cref="Random"/> used to generate the random selection.</param>
    /// <param name="collection">The array to choose an item from.</param>
    /// <returns>A randomly chosen item from the <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="random"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="collection"/> is empty.
    /// </exception>
    public static T Choose<T>(this Random random, params T[] collection)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(collection);

        return collection[random.Next(collection.Length)];
    }

    /// <summary>
    /// Shuffles the elements of the specified collection in place using the Fisher-Yates shuffle algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="random">An instance of <see cref="Random"/> used to generate random indices for shuffling.</param>
    /// <param name="collection">The collection to shuffle.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="random"/> or <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    public static void Shuffle<T>(this Random random, IList<T> collection)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(collection);

        int n = collection.Count;

        while (n > 1)
        {
            n--;

            int k = random.Next(n + 1);

            T value = collection[k];

            collection[k] = collection[n];
            collection[n] = value;
        }
    }
}