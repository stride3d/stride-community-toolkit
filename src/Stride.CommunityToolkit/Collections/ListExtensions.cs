namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Extensions for resizing and ensuring capacity of <see cref="List{T}"/> without allocations per element.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Ensures the list contains at least <paramref name="size"/> items by growing it with default values.
    /// Does nothing if <see cref="List{T}.Count"/> is already greater than or equal to <paramref name="size"/>.
    /// </summary>
    public static void EnsureSize<T>(this List<T> list, int size)
    {
        ArgumentNullException.ThrowIfNull(list);

        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (list.Count >= size) return;

        list.Capacity = Math.Max(list.Capacity, size);

        int toAdd = size - list.Count;

        for (int i = 0; i < toAdd; i++)
        {
            list.Add(default!);
        }
    }

    /// <summary>
    /// Sets the list count to <paramref name="size"/>, adding default values or removing trailing items as needed.
    /// </summary>
    public static void SetCount<T>(this List<T> list, int size)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (list.Count < size)
        {
            EnsureSize(list, size);
        }
        else if (list.Count > size)
        {
            list.RemoveRange(size, list.Count - size);
        }
    }
}