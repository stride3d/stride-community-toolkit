using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.Extensions;
public static class CollectionExtensions
{
    public static Span<T> AsSpan<T>(this List<T> list)
    {
        return CollectionsMarshal.AsSpan(list);
    }

    public static Span<T> AsSpan<T>(this List<T> list, int startIndex, int length)
    {
        var listSpan = CollectionsMarshal.AsSpan(list);
        return listSpan.Slice(startIndex, length);
    }

    public static T[] GetInternalArray<T>(this List<T> list) => ArrayAccessor<T>.Getter(list);
}

internal static class ArrayAccessor<T>
{
    public static Func<List<T>, T[]> Getter;

    static ArrayAccessor()
    {
        var dm = new DynamicMethod("get", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(T[]), new Type[] { typeof(List<T>) }, typeof(ArrayAccessor<T>), true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance));
        il.Emit(OpCodes.Ret);
        Getter = (Func<List<T>, T[]>)dm.CreateDelegate(typeof(Func<List<T>, T[]>));
    }
}