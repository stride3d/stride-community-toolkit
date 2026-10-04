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
    /// <summary>Array of all possible <see cref="Filter"/> values</summary>
    static readonly Filter[] _filterValues = Enum.GetValues<Filter>();
    internal const float DUMMY_WIDTH = 19;
    const float INDENTATION2 = DUMMY_WIDTH + 8;

    /// <summary>A UI handler function to draw and modify values</summary>
    public delegate bool ValueHandler(string label, ref object value);
    /// <summary>Add your drawing functions to explicitly override drawing for objects of the given type</summary>
    public static ConcurrentDictionary<Type, ValueHandler> ValueDrawingHandlers { get; } = new();

    /// <summary>Any live inspectors</summary>
    static readonly List<Inspector> _inspectors = [];


    readonly Dictionary<Type, TypeCache> _cachedTypeData = [];
    /// <summary>Opened sub object of the inspected object in the tree view</summary>
    readonly HashSet<int> _openedId = [];
    /// <summary>Lets not keep references from being GCed</summary>
    readonly WeakReference<object?> _target = new(null);


    // Settings
    /// <summary>When <see langword="true"/>, <see cref="FindFreeInspector"/> skips this inspector, so its target is kept.</summary>
    public bool Locked = false;
    /// <summary>Show specialized interface to handle IEnumerable types</summary>
    public bool EnumerableView = true;
    /// <summary>
    /// For <see cref="Target"/> of type <see cref="System.Type"/>, return the content of 'static type.*' instead of 'typeof(type).*'
    /// </summary>
    public bool TypeAsStatic = true;

    /// <summary>Members shown within the interface</summary>
    public Filter MemberFilter
    {
        get => _memberFilter;
        set
        {
            if (_memberFilter == value)
                return;
            _memberFilter = value;
            _cachedTypeData.Clear();
        }
    }

    Filter _memberFilter = Filter.Public | Filter.Inherited | Filter.Properties | Filter.Fields | Filter.Instance;

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


    readonly InspectorCollectionsView _collectionsView;

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
        foreach (Inspector inspector in _inspectors)
        {
            if (!inspector.Locked)
                return inspector;
        }

        return new Inspector(services);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        _inspectors.Remove(this);
    }

    /// <inheritdoc />
    protected override void OnDraw(bool collapsed)
    {
        if (collapsed)
            return;

        Checkbox("Locked", ref Locked);
        using (UCombo("Filter", MemberFilter.ToString(), out bool open))
        {
            if (open)
            {
                foreach (Filter o in _filterValues)
                {
                    bool selected = (MemberFilter & o) == o;
                    if (Selectable(o.ToString(), selected))
                    {
                        if (selected) // unselect this value
                            MemberFilter = MemberFilter & ~o;
                        else // select new value
                            MemberFilter = MemberFilter | o;
                    }
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
            if (Target is { } target)
                DrawMembers(target, target.GetType().GetHashCode());
        }
    }

    bool DrawMembers(object target, int hashcodeSource)
    {
        if (target == null)
            return false;

        Type type = TypeAsStatic && target is Type ? (Type)target : target.GetType();
        MemberInfo[] members = GetTypeData(type).FilteredMembers;

        bool hasChanged = false;
        using (UIndent(INDENTATION2))
        {
            foreach (var member in members)
            {
                GetMemberValue(member, target, out object? value, out bool readOnly);

                if (XMLDocumentation.TryGetSummary(member, out var summary))
                {
                    SetCursorPosX(-0.5f);
                    Button("?");
                    if (IsItemHovered())
                    {
                        using (Tooltip())
                            TextUnformatted(summary);
                    }

                    SameLine();
                }

                bool changed = DrawValue(member.Name, ref value, readOnly, hashcodeSource);
                if (changed && !readOnly)
                {
                    hasChanged = true;
                    SetMemberValue(member, target, value);
                }
            }
            if (EnumerableView && target is IEnumerable ienum)
                _collectionsView.Draw(target, ienum, hashcodeSource);
        }

        // structs have to bubble up their changes since the object
        // we get is not pointing to the source but is a copy of it instead
        return hasChanged && target.GetType().IsValueType;
    }

    static void GetMemberValue(MemberInfo member, object target, out object? value, out bool readOnly)
    {
        try
        {
            if (member is FieldInfo fi)
            {
                value = fi.GetValue(target);
                readOnly = fi.IsInitOnly;
            }
            else if (member is PropertyInfo pi && pi.CanRead)
            {
                value = pi.GetValue(target);
                readOnly = !pi.CanWrite;
            }
            else if (member is Type asType)
            {
                value = asType;
                readOnly = true;
            }
            else
            {
                throw new NotSupportedException($"UI handler for type {member.GetType()} not implemented");
            }
        }
        catch (Exception e)
        {
            value = $"x Exception: {e.Message}";
            readOnly = true;
        }
    }

    static void SetMemberValue(MemberInfo member, object target, object? value)
    {
        try
        {
            if (member is FieldInfo fi)
                fi.SetValue(target, value);
            else if (member is PropertyInfo pi)
                pi.SetValue(target, value);
            else
                throw new NotSupportedException();
        }
        catch (Exception e)
        {
            Console.Out?.WriteLine(e);
        }
    }

    internal bool DrawValue(string constantName, ref object? value, bool readOnly, int hashcodeSource)
    {
        // Deterministic way to provide a hashcode in a hierarchic/recursive manner
        // The hashcode created here, properly create one specific code for this object at this place in the hierarchy
        // of course hashcodes still aren't unique but this should work well enough for now
        int memberInHierarchyId = (hashcodeSource, constantName).GetHashCode();
        using (ID(memberInHierarchyId))
        {
            if (value == null)
            {
                Dummy(new Vector2(DUMMY_WIDTH, 1));
                using (UColumns(2))
                {
                    Dummy(new Vector2(DUMMY_WIDTH, 1));
                    SameLine();
                    TextUnformatted(constantName);
                    NextColumn();
                    TextUnformatted("null");
                }
                return false;
            }
            Type type = TypeAsStatic && value is Type ? (Type)value : value.GetType();
            TypeCache typeData = GetTypeData(type);
            bool valueChanged = false;
            if (ValueDrawingHandlers.TryGetValue(type, out var handler))
            {
                // The public handler contract takes a non-null value; it is only reached past the null check above
                object handled = value;
                valueChanged = handler(constantName, ref handled);
                value = handled;
                return valueChanged;
            }

            bool recursable = IsRecursable(type, typeData, value);

            bool recurse = recursable && _openedId.Contains(memberInHierarchyId);

            using (UColumns(2))
            {
                // Present button to recurse through value
                DrawRecurseArrow(recursable, recurse, memberInHierarchyId);

                SameLine();
                TextUnformatted(constantName);

                NextColumn();

                // Complex object: present button to swap inspect target to this object ?
                if (Type.GetTypeCode(type) == TypeCode.Object && type.IsClass)
                {
                    if (Button(value.ToString()))
                        Target = value;
                    goto RECURSE;
                }
                // Basic value type: Present UI handler for values
                else if (!readOnly && InspectorValueDrawers.TryDrawScalar(ref value, out valueChanged))
                {
                    return valueChanged;
                }
                if (typeData.AsEnum != null)
                {
                    return InspectorValueDrawers.DrawEnum(typeData.AsEnum.Value, ref value);
                }

                // Otherwise, present basic read-only text
                // value is only reassigned by TryDrawScalar, which never writes null; the compiler loses that across the ref.
                TextUnformatted(value!.ToString());
            }

RECURSE:

            if (recurse) // Pass in this member's id to properly offset sub-members' hash
                valueChanged = valueChanged || DrawMembers(value, memberInHierarchyId);

            return valueChanged;
        }
    }

    static bool IsRecursable(Type type, TypeCache typeData, object value)
    {
        bool recursable = Type.GetTypeCode(type) == TypeCode.Object;
        return recursable && (typeData.FilteredMembers.Length > 0 || ReadableIEnumerable(value));
    }

    void DrawRecurseArrow(bool recursable, bool recurse, int memberInHierarchyId)
    {
        if (recursable)
        {
            if (ArrowButton("", recurse ? ImGuiDir.Down : ImGuiDir.Right))
            {
                if (recurse)
                    _openedId.Remove(memberInHierarchyId);
                else
                    _openedId.Add(memberInHierarchyId);
            }
        }
        else
        {
            Dummy(new Vector2(DUMMY_WIDTH, 1));
        }
    }

    internal TypeCache GetTypeData(Type t)
    {
        if (_cachedTypeData.TryGetValue(t, out var output))
            return output;

        output = new TypeCache(t, MemberFilter);
        _cachedTypeData.Add(t, output);
        return output;
    }

    static bool ReadableIEnumerable(object source)
    {
        if (source is IEnumerable ienum)
        {
            foreach (object o in ienum)
                return true;
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

    internal sealed class TypeCache
    {
        internal readonly MemberInfo[] FilteredMembers;
        internal readonly (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue)? AsDictionary;
        internal readonly Type? AsList;
        internal readonly (bool flags, Array values)? AsEnum;
        readonly Type _type;
        readonly Filter _filter;

        internal TypeCache(Type t, Filter filter)
        {
            _type = t;
            _filter = filter;
            FilteredMembers = GetAllMembers(_type).Where(m => PassesFilter(_type, m)).ToArray();

            var generics = GetGenericsFromBaseType(_type, typeof(IDictionary<,>));
            if (generics == null && typeof(IDictionary).IsAssignableFrom(_type))
                AsDictionary = (typeof(object), typeof(object), null, null);
            else if (generics != null)
                AsDictionary = (generics[0], generics[1], null, null);

            if (AsDictionary != null)
            {
                (Type key, Type value, _, _) = AsDictionary.Value;
                // IDictionary.Keys
                var getKey = _type.GetProperty(nameof(IDictionary.Keys), BindingFlags.Public | BindingFlags.Instance)?.GetMethod;
                // IDictionary.Values
                var getValue = _type.GetProperty(nameof(IDictionary.Values), BindingFlags.Public | BindingFlags.Instance)?.GetMethod;
                AsDictionary = (key, value, getKey, getValue);
            }

            generics = GetGenericsFromBaseType(_type, typeof(IList<>));
            if (generics == null && typeof(IList).IsAssignableFrom(_type))
                AsList = typeof(object);
            else if (generics != null)
                AsList = generics[0];

            if (_type.IsEnum)
            {
                AsEnum = (_type.IsDefined(typeof(FlagsAttribute)), Enum.GetValues(_type));
            }
        }

        static Type[]? GetGenericsFromBaseType(Type impl, Type type)
        {
            Type? t = impl.GetInterfaces()
                .Where(i => i.IsGenericType)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == type);
            return t?.GenericTypeArguments;
        }

        internal object? NewObject()
        {
            if (_type == typeof(string))
                return string.Empty;
            return _type.IsValueType ? Activator.CreateInstance(_type) : _type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        }

        bool PassesFilter(Type classType, MemberInfo m)
        {
            if (!(m is FieldInfo || m is PropertyInfo || m is Type))
                return false;

            // Build the categories this member belongs to; it is shown only when the
            // current filter includes every one of them.
            Filter memberFilter = 0;

            if (classType != m.DeclaringType)
                memberFilter |= Filter.Inherited;

            if (m is FieldInfo fi)
            {
                if (IsBackingField(fi))
                    return false;

                memberFilter |= Filter.Fields;
                if (fi.IsStatic)
                    memberFilter |= Filter.Static;
                else
                    memberFilter |= Filter.Instance;

                if (fi.IsPublic)
                    memberFilter |= Filter.Public;
                else
                    memberFilter |= Filter.NonPublic;
            }

            if (m is PropertyInfo pi)
            {
                var method = pi.GetMethod;
                if (method == null || method.GetParameters().Length != 0)
                    return false;

                memberFilter |= Filter.Properties;
                if (method.IsStatic)
                    memberFilter |= Filter.Static;
                else
                    memberFilter |= Filter.Instance;

                if (method.IsPublic)
                    memberFilter |= Filter.Public;
                else
                    memberFilter |= Filter.NonPublic;
            }

            if (m is Type innerType)
            {
                memberFilter |= Filter.SubTypes;
                if (innerType.IsAbstract && innerType.IsSealed)
                    memberFilter |= Filter.Static;
                else
                    memberFilter |= Filter.Instance;

                if (innerType.IsPublic || innerType.IsNestedPublic)
                    memberFilter |= Filter.Public;
                else
                    memberFilter |= Filter.NonPublic;
            }

            // Every category of the member must be included in the active filter.
            return memberFilter != 0 && (memberFilter & _filter) == memberFilter;
        }

        static bool IsBackingField(FieldInfo fi)
        {
            if (!fi.IsPrivate)
                return false;

            if (fi.Name[0] != '<' || !fi.Name.EndsWith(">k__BackingField"))
                return false;

            return fi.IsDefined(typeof(CompilerGeneratedAttribute), true);
        }

        /// <summary>Reflection doesn't provide private inherited fields for some reason, this resolves that issue</summary>
        static IEnumerable<MemberInfo> GetAllMembers(Type t)
        {
            foreach (MemberInfo member in t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                yield return member;
            }
            for (Type? current = t; current != null; current = current.BaseType)
            {
                foreach (MemberInfo member in current.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    yield return member;
                }
            }
        }
    }
}