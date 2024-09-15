namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Extension methods for <see cref="IDictionary{TKey,TValue}"/>.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Adds items from one dictionary to the other.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="source">The dictionary items are copied from.</param>
    /// <param name="target">The dictionary items are added to.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="target"/> are <see langword="null"/>.</exception>
    public static void MergeInto<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target)
    {
        ArgumentNullException.ThrowIfNull(source);

        ArgumentNullException.ThrowIfNull(target);

        foreach (var item in source)
        {
            target[item.Key] = item.Value;
        }
    }

    /// <summary>
    /// Gets the element with the specified key or a default value if it is not in the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get element from.</param>
    /// <param name="key">The key of the element to get.</param>
    /// <param name="defaultValue">The value to return if element with specified key does not exist in the <paramref name="dictionary"/>.</param>
    /// <returns>The element with the specified key, or the default value.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (dictionary.TryGetValue(key, out TValue? result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the element with the specified key or adds it if it is not in the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get element from.</param>
    /// <param name="key">The key of the element to get.</param>
    /// <param name="getValue">The callback delegate to return value if element with specified key does not exist in the <paramref name="dictionary"/>.</param>
    /// <returns>The element with the specified key, or the added value.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="dictionary"/> or <paramref name="getValue"/> are <see langword="null"/>.</exception>
    public static TValue? GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> getValue)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        ArgumentNullException.ThrowIfNull(getValue);

        if (!dictionary.TryGetValue(key, out TValue? result))
        {
            dictionary[key] = result = getValue(key);
        }

        return result;
    }

    /// <summary>
    /// Gets the element with the specified key in the dictionary or the new value returned from the <paramref name="getValue"/> callback.
    /// If the <paramref name="shouldAdd"/> callback returns <see langword="true"/> then the new value is added to the dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get element from.</param>
    /// <param name="key">The key of the element to get.</param>
    /// <param name="getValue">The callback delegate to return value if element with specified key does not exist in the <paramref name="dictionary"/>.</param>
    /// <param name="shouldAdd">The callback delegate to determine if the new value should be added to the <paramref name="dictionary"/>.</param>
    /// <returns>The element with the specified key, or the new value.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="dictionary"/>, <paramref name="getValue"/> or <paramref name="shouldAdd"/> are <see langword="null"/>.</exception>
    public static TValue? GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> getValue, Func<TValue, bool> shouldAdd)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        ArgumentNullException.ThrowIfNull(getValue);

        ArgumentNullException.ThrowIfNull(shouldAdd);

        if (!dictionary.TryGetValue(key, out TValue? result))
        {
            result = getValue(key);

            if (shouldAdd(result))
            {
                dictionary[key] = result;
            }
        }

        return result;
    }

    /// <summary>
    /// Increments integer value in a dictionary by 1.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get element from.</param>
    /// <param name="key">The key of the element to increment and get.</param>
    /// <returns>The element incremented by 1 with the specified key.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static int Increment<TKey>(this IDictionary<TKey, int> dictionary, TKey key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        dictionary[key] = dictionary.TryGetValue(key, out int result) ? result += 1 : result = 1;

        return result;
    }
}