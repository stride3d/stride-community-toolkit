using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Collections;

/// <summary>
/// Extensions for resizing and ensuring capacity of <see cref="List{T}"/> without allocations per element.
/// </summary>
/// <remarks>
/// Both go through <see cref="CollectionsMarshal.SetCount{T}"/>, which resizes in one step, and clear the slots
/// they add, since the marshal leaves whatever a shrunken list left in them.
/// </remarks>
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

        Grow(list, size);
    }

    /// <summary>
    /// Sets the list count to <paramref name="size"/>, adding default values or removing trailing items as needed.
    /// </summary>
    public static void SetCount<T>(this List<T> list, int size)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentOutOfRangeException.ThrowIfNegative(size);

        if (list.Count < size)
            Grow(list, size);
        else
            CollectionsMarshal.SetCount(list, size);
    }

    private static void Grow<T>(List<T> list, int size)
    {
        var count = list.Count;

        CollectionsMarshal.SetCount(list, size);
        CollectionsMarshal.AsSpan(list)[count..].Clear();
    }
}