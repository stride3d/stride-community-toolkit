using Stride.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using ImGuiDir = Hexa.NET.ImGui.ImGuiDir;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// Inspector window for debugging and modifying object properties.
/// </summary>
public class Inspector : BaseWindow
{
    internal const float DummyWidth = 19;
    private const float Indentation = DummyWidth + 8;

    /// <summary>Array of all possible <see cref="Filter"/> values</summary>
    private static readonly Filter[] _filterValues = Enum.GetValues<Filter>();

    /// <summary>Any live inspectors</summary>
    private static readonly List<Inspector> _inspectors = [];

    /// <summary>A UI handler function to draw and modify values</summary>
    public delegate bool ValueHandler(string label, ref object value);

    /// <summary>Add your drawing functions to explicitly override drawing for objects of the given type</summary>
    public static ConcurrentDictionary<Type, ValueHandler> ValueDrawingHandlers { get; } = new();

    private readonly Dictionary<Type, TypeCache> _cachedTypeData = [];
    /// <summary>Opened sub object of the inspected object in the tree view</summary>
    private readonly HashSet<int> _openedId = [];
    /// <summary>Lets not keep references from being GCed</summary>
    private readonly WeakReference<object?> _target = new(null);
    private readonly InspectorCollectionsView _collectionsView;

    // Settings
    /// <summary>When <see langword="true"/>, <see cref="FindFreeInspector"/> skips this inspector, so its target is kept.</summary>
    public bool Locked;
    /// <summary>Show specialized interface to handle IEnumerable types</summary>
    public bool EnumerableView = true;
    /// <summary>
    /// For <see cref="Target"/> of type <see cref="System.Type"/>, return the content of 'static type.*' instead of 'typeof(type).*'
    /// </summary>
    public bool TypeAsStatic = true;

    /// <summary>Members shown within the interface</summary>
    public Filter MemberFilter
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            _cachedTypeData.Clear();
        }
    } = Filter.Public | Filter.Inherited | Filter.Properties | Filter.Fields | Filter.Instance;

    /// <summary>The object to inspect</summary>
    public object? Target
    {
        get => _target.TryGetTarget(out var target) ? target : null;
        set
        {
            _target.SetTarget(value);
            _openedId.Clear();
        }
    }

    /// <summary>
    /// Creates a new inspector window and registers it with the game's systems. Prefer <see cref="FindFreeInspector"/> to reuse an open window that is not <see cref="Locked"/>.
    /// </summary>
    /// <param name="services">The game's service registry, which must already contain an <see cref="ImGuiSystem"/>.</param>
    public Inspector(IServiceRegistry services) : base(services)
    {
        _collectionsView = new InspectorCollectionsView(this);
        _inspectors.Add(this);
    }

    /// <summary>
    /// Returns the first live inspector that is not <see cref="Locked"/>, creating one if none is free.
    /// </summary>
    /// <param name="services">The game's service registry, used when a new inspector has to be created.</param>
    /// <returns>An inspector whose <see cref="Target"/> can be set.</returns>
    public static Inspector FindFreeInspector(IServiceRegistry services)
    {
        foreach (var inspector in _inspectors)
        {
            if (!inspector.Locked) return inspector;
        }

        return new Inspector(services);
    }

    /// <inheritdoc />
    protected override void OnDestroy() => _inspectors.Remove(this);

    /// <inheritdoc />
    protected override void OnDraw(bool collapsed)
    {
        if (collapsed) return;

        Checkbox("Locked", ref Locked);

        using (UCombo("Filter", MemberFilter.ToString(), out var open))
        {
            if (open)
            {
                foreach (var flag in _filterValues)
                {
                    var selected = (MemberFilter & flag) == flag;

                    if (Selectable(flag.ToString(), selected)) MemberFilter = selected ? MemberFilter & ~flag : MemberFilter | flag;
                }
            }
        }

        Checkbox("Enumerable view", ref EnumerableView);
        SameLine();
        Checkbox("Type as static ref", ref TypeAsStatic);

        Spacing();

        TextUnformatted($"Inspecting [{Target ?? "null"}]");
        Separator();

        using (Child())
        {
            if (Target is { } target) DrawMembers(target, target.GetType().GetHashCode());
        }
    }

    private bool DrawMembers(object target, int hashcodeSource)
    {
        var type = TypeAsStatic && target is Type asType ? asType : target.GetType();
        var hasChanged = false;

        using (UIndent(Indentation))
        {
            foreach (var member in GetTypeData(type).FilteredMembers)
            {
                var (value, readOnly) = GetMemberValue(member, target);

                if (XMLDocumentation.TryGetSummary(member, out var summary))
                {
                    SetCursorPosX(-0.5f);
                    Button("?");

                    if (IsItemHovered())
                    {
                        using (Tooltip()) TextUnformatted(summary);
                    }

                    SameLine();
                }

                if (DrawValue(member.Name, ref value, readOnly, hashcodeSource) && !readOnly)
                {
                    hasChanged = true;
                    SetMemberValue(member, target, value);
                }
            }

            if (EnumerableView && target is IEnumerable ienum) _collectionsView.Draw(target, ienum, hashcodeSource);
        }

        // structs have to bubble up their changes since the object
        // we get is not pointing to the source but is a copy of it instead
        return hasChanged && target.GetType().IsValueType;
    }

    private static (object? value, bool readOnly) GetMemberValue(MemberInfo member, object target)
    {
        try
        {
            return member switch
            {
                FieldInfo field => (field.GetValue(target), field.IsInitOnly),
                PropertyInfo { CanRead: true } property => (property.GetValue(target), !property.CanWrite),
                Type type => (type, true),
                _ => throw new NotSupportedException($"UI handler for type {member.GetType()} not implemented"),
            };
        }
        catch (Exception e)
        {
            return ($"x Exception: {e.Message}", true);
        }
    }

    private static void SetMemberValue(MemberInfo member, object target, object? value)
    {
        try
        {
            switch (member)
            {
                case FieldInfo field: field.SetValue(target, value); break;
                case PropertyInfo property: property.SetValue(target, value); break;
                default: throw new NotSupportedException();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    internal bool DrawValue(string constantName, ref object? value, bool readOnly, int hashcodeSource)
    {
        // A deterministic id for this member at this place in the hierarchy: the parent's id combined with the
        // member name. Hash codes are not unique, but this has worked well enough so far
        var memberInHierarchyId = (hashcodeSource, constantName).GetHashCode();

        using (ID(memberInHierarchyId))
        {
            if (value is null)
            {
                Dummy(new Vector2(DummyWidth, 1));

                using (UColumns(2))
                {
                    Dummy(new Vector2(DummyWidth, 1));
                    SameLine();
                    TextUnformatted(constantName);
                    NextColumn();
                    TextUnformatted("null");
                }

                return false;
            }

            var type = TypeAsStatic && value is Type asType ? asType : value.GetType();

            if (ValueDrawingHandlers.TryGetValue(type, out var handler))
            {
                // The public handler contract takes a non-null value; it is only reached past the null check above
                object handled = value;
                var handledChanged = handler(constantName, ref handled);

                value = handled;

                return handledChanged;
            }

            var typeData = GetTypeData(type);
            var isObject = Type.GetTypeCode(type) == TypeCode.Object;
            var recursable = isObject && (typeData.FilteredMembers.Length > 0 || ReadableIEnumerable(value));
            var recurse = recursable && _openedId.Contains(memberInHierarchyId);

            using (UColumns(2))
            {
                DrawRecurseArrow(recursable, recurse, memberInHierarchyId);
                SameLine();
                TextUnformatted(constantName);
                NextColumn();

                if (isObject && type.IsClass)
                {
                    // A complex object: the button swaps the inspected target to it
                    if (Button(value.ToString())) Target = value;
                }
                else if (!readOnly && InspectorValueDrawers.TryDrawScalar(ref value, out var valueChanged))
                {
                    return valueChanged;
                }
                else if (typeData.AsEnum is { } enumInfo)
                {
                    return InspectorValueDrawers.DrawEnum(enumInfo, ref value);
                }
                else
                {
                    // Anything else is read-only text. TryDrawScalar never writes null; the compiler loses that across the ref
                    TextUnformatted(value!.ToString());
                }
            }

            // Sub-members hash from this member's id, so their ids differ per branch
            return recurse && DrawMembers(value!, memberInHierarchyId);
        }
    }

    private void DrawRecurseArrow(bool recursable, bool recurse, int memberInHierarchyId)
    {
        if (!recursable)
        {
            Dummy(new Vector2(DummyWidth, 1));
        }
        else if (ArrowButton("", recurse ? ImGuiDir.Down : ImGuiDir.Right) && !_openedId.Remove(memberInHierarchyId))
        {
            _openedId.Add(memberInHierarchyId);
        }
    }

    internal TypeCache GetTypeData(Type type)
    {
        if (!_cachedTypeData.TryGetValue(type, out var data)) _cachedTypeData[type] = data = new TypeCache(type, MemberFilter);

        return data;
    }

    private static bool ReadableIEnumerable(object source)
    {
        if (source is IEnumerable ienum)
        {
            foreach (var _ in ienum) return true;
        }

        return false;
    }

    /// <summary>
    /// Which members of the inspected object are listed. A member is shown only when every category it belongs to is included, so <c>Public | Fields | Instance</c> shows public instance fields and nothing else.
    /// </summary>
    [Flags]
    public enum Filter : uint
    {
        /// <summary>Fields.</summary>
        Fields = 1,
        /// <summary>Readable properties without index parameters.</summary>
        Properties = Fields << 1,
        /// <summary>Nested types.</summary>
        SubTypes = Properties << 1,
        /// <summary>Members with public accessibility.</summary>
        Public = SubTypes << 1,
        /// <summary>Members with any accessibility other than public.</summary>
        NonPublic = Public << 1,
        /// <summary>Static members.</summary>
        Static = NonPublic << 1,
        /// <summary>Instance members.</summary>
        Instance = Static << 1,
        /// <summary>Members declared on a base type rather than on the inspected type itself.</summary>
        Inherited = Instance << 1,
    }

    /// <summary>What the inspector needs to know about one type, resolved once per type and filter.</summary>
    internal sealed class TypeCache(Type type, Filter filter)
    {
        internal readonly MemberInfo[] FilteredMembers = [.. GetAllMembers(type).Where(m => PassesFilter(type, filter, m))];
        internal readonly (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue)? AsDictionary = DictionaryShape(type);
        internal readonly Type? AsList = ListItemType(type);
        internal readonly (bool flags, Array values)? AsEnum = type.IsEnum ? (type.IsDefined(typeof(FlagsAttribute)), Enum.GetValues(type)) : null;

        internal object? NewObject() => type == typeof(string) ? string.Empty : type.IsValueType ? Activator.CreateInstance(type) : type.GetConstructor(Type.EmptyTypes)?.Invoke(null);

        // The key and value types of an IDictionary<,> (or object for a plain IDictionary), with the Keys and Values getters
        private static (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue)? DictionaryShape(Type type)
        {
            var generics = GenericArguments(type, typeof(IDictionary<,>));

            if (generics is null && !typeof(IDictionary).IsAssignableFrom(type)) return null;

            var (key, value) = generics is null ? (typeof(object), typeof(object)) : (generics[0], generics[1]);

            return (key, value, Getter(type, nameof(IDictionary.Keys)), Getter(type, nameof(IDictionary.Values)));
        }

        private static Type? ListItemType(Type type)
        {
            var generics = GenericArguments(type, typeof(IList<>));

            return generics is not null ? generics[0] : typeof(IList).IsAssignableFrom(type) ? typeof(object) : null;
        }

        private static Type[]? GenericArguments(Type type, Type definition)
            => type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition)?.GenericTypeArguments;

        private static MethodInfo? Getter(Type type, string property) => type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetMethod;

        private static bool PassesFilter(Type classType, Filter filter, MemberInfo member)
        {
            // The categories the member belongs to; it is shown only when the filter includes every one of them
            var categories = classType != member.DeclaringType ? Filter.Inherited : 0;

            switch (member)
            {
                case FieldInfo field when !IsBackingField(field):
                    categories |= Filter.Fields | Category(field.IsStatic, field.IsPublic);
                    break;
                case PropertyInfo { GetMethod: { } getter } when getter.GetParameters().Length == 0:
                    categories |= Filter.Properties | Category(getter.IsStatic, getter.IsPublic);
                    break;
                case Type inner:
                    categories |= Filter.SubTypes | Category(inner.IsAbstract && inner.IsSealed, inner.IsPublic || inner.IsNestedPublic);
                    break;
                default:
                    return false;
            }

            return (categories & filter) == categories;
        }

        private static Filter Category(bool isStatic, bool isPublic) => (isStatic ? Filter.Static : Filter.Instance) | (isPublic ? Filter.Public : Filter.NonPublic);

        private static bool IsBackingField(FieldInfo field)
            => field.IsPrivate && field.Name[0] == '<' && field.Name.EndsWith(">k__BackingField") && field.IsDefined(typeof(CompilerGeneratedAttribute), true);

        /// <summary>Reflection doesn't provide private inherited fields for some reason, this resolves that issue</summary>
        private static IEnumerable<MemberInfo> GetAllMembers(Type type)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) yield return member;

            for (Type? current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)) yield return member;
            }
        }
    }
}