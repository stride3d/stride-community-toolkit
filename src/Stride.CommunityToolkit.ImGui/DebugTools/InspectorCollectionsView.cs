using System.Collections;
using System.Numerics;
using System.Reflection;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// The collection views of <see cref="Inspector"/>: an <see cref="IEnumerable"/> shown item by item,
/// with in-place editing, add and remove for types that expose <see cref="IList"/> or
/// <see cref="IDictionary"/> shapes. Split out of <see cref="Inspector"/>, which owns the member
/// tree; this owns the collection editing and the pending dictionary-add entry.
/// </summary>
/// <param name="inspector">The inspector whose values and type cache the views draw with.</param>
internal sealed class InspectorCollectionsView(Inspector inspector)
{
    // The dictionary an add is being prepared for, and the key and value entered so far
    private readonly WeakReference<object?> _dicAddCommandTarget = new(null);
    private (object? key, object? value) _dicAddCommandData;

    internal void Draw(object target, IEnumerable ienum, int hashcodeSource)
    {
        using (UIndent())
        {
            if (TryDrawAsIList(target, hashcodeSource)) return;
            if (TryDrawAsIDictionary(target, hashcodeSource)) return;

            Spacing();
            TextDisabled("As Enumerable");

            var index = 0;

            try
            {
                foreach (var o in ienum)
                {
                    var item = o;

                    using (UIndent()) inspector.DrawValue("-", ref item, true, (hashcodeSource, index).GetHashCode());

                    index++;
                }
            }
            catch (Exception e)
            {
                object? str = $"x Exception: {e.Message}";

                using (UIndent()) inspector.DrawValue("-", ref str, true, (hashcodeSource, index).GetHashCode());
            }
        }

        Spacing();
    }

    private bool TryDrawAsIDictionary(object target, int hashcodeSource)
    {
        var typeData = inspector.GetTypeData(target.GetType());

        if (typeData.AsDictionary is not { } data) return false;

        Spacing();
        TextDisabled("As Dictionary");

        // Most of the management here is done through reflection
        // as the type might not implement IDictionary but just IDictionary<T> ...
        using (UIndent())
        {
            if (!DrawDictionaryEntries(target, data, hashcodeSource)) return false;

            DrawDictionaryAddControls(target, data, hashcodeSource);
        }

        return true;
    }

    private bool DrawDictionaryEntries(object target, (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue) data, int hashcodeSource)
    {
        var keys = (data.getKey?.Invoke(target, null) as IEnumerable)?.GetEnumerator();
        var values = (data.getValue?.Invoke(target, null) as IEnumerable)?.GetEnumerator();

        if (keys is null || values is null) return false;

        // The edits are applied after the walk, so the dictionary is not changed while it is enumerated
        var removeKey = false;
        object? keyToRemove = null;
        var changeKey = false;
        object? keyToChange = null;
        object? valueOfKeyToChange = null;
        var index = 0;

        while (keys.MoveNext() && values.MoveNext())
        {
            var key = keys.Current;
            var value = values.Current;

            // Hashed with the index: the key is unique and constant, its ToString() need not be
            var newHash = (hashcodeSource, index).GetHashCode();

            using (ID(newHash))
            {
                SetCursorPosX(GetCursorPosX() - Inspector.DummyWidth);

                if (Button("x"))
                {
                    removeKey = true;
                    keyToRemove = key;
                }
            }

            SameLine();

            if (inspector.DrawValue(key?.ToString() ?? "null", ref value, false, newHash))
            {
                changeKey = true;
                keyToChange = key;
                valueOfKeyToChange = value;
            }

            index++;
        }

        if (removeKey)
        {
            target.GetType().GetMethod(nameof(IDictionary.Remove), [data.key])?.Invoke(target, [keyToRemove]);
        }

        if (changeKey)
        {
            // IDictionary[keyToChange] = valueOfKeyToChange
            target.GetType().GetProperty("Item", data.value, [data.key])?.SetMethod?.Invoke(target, [keyToChange, valueOfKeyToChange]);
        }

        return true;
    }

    private void DrawDictionaryAddControls(object target, (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue) data, int hashcodeSource)
    {
        // An add in progress: the upcoming key and value are shown and edited until Add or Cancel
        if (_dicAddCommandTarget.TryGetTarget(out var addActionTarget) && addActionTarget == target)
        {
            var (key, value) = _dicAddCommandData;

            inspector.DrawValue("Upcoming Key:", ref key, false, hashcodeSource);
            inspector.DrawValue("Upcoming Value:", ref value, false, hashcodeSource);
            _dicAddCommandData = (key, value);

            if (Button("Cancel"))
            {
                _dicAddCommandData = (null, null);
                _dicAddCommandTarget.SetTarget(null);
            }

            SameLine();

            if (Button("Add"))
            {
                target.GetType().GetMethod(nameof(IDictionary.Add), [data.key, data.value])?.Invoke(target, [key, value]);

                _dicAddCommandData = (null, null);
                _dicAddCommandTarget.SetTarget(null);
            }
        }
        // Prepare an add to the dictionary: create an editable instance for key and value
        else if (Button("+", new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing())))
        {
            _dicAddCommandData = (inspector.GetTypeData(data.key).NewObject(), inspector.GetTypeData(data.value).NewObject());
            _dicAddCommandTarget.SetTarget(target);
        }
    }

    private bool TryDrawAsIList(object target, int hashcodeSource)
    {
        var typeData = inspector.GetTypeData(target.GetType());

        if (typeData.AsList is not { } itemType) return false;

        Spacing();
        TextDisabled("As List");

        // Most of the management here is done through reflection
        // as the type might not implement IList but just IList<T> ...
        using (UIndent())
        {
            var i = 0;
            int? indexToRemove = null;
            int? indexToChange = null;
            object? objectToAssign = null;

            foreach (var o in (IEnumerable)target)
            {
                var item = o;

                using (ID($"{o}{i}"))
                {
                    SetCursorPosX(GetCursorPosX() - Inspector.DummyWidth);

                    if (Button("x")) indexToRemove = i;
                }

                SameLine();

                if (inspector.DrawValue($"{i}:", ref item, false, hashcodeSource))
                {
                    indexToChange = i;
                    objectToAssign = item;
                }

                i++;
            }

            // Calling 'this[int indexToChange] = objectToAssign'
            if (indexToChange is { } changed)
            {
                MethodInfo? listAccessor;

                if (target.GetType().IsArray)
                {
                    listAccessor = target.GetType().GetMethod("SetValue", [typeof(object), typeof(int)]);
                    listAccessor?.Invoke(target, [objectToAssign, changed]);
                }
                else
                {
                    listAccessor = target.GetType().GetProperty("Item", itemType, [typeof(int)])?.SetMethod;
                    listAccessor?.Invoke(target, [changed, objectToAssign]);
                }

                if (listAccessor is null) Console.WriteLine($"Couldn't find {nameof(listAccessor)} for {target.GetType()}");
            }

            // Calling 'RemoveAt(int index)'
            if (indexToRemove is { } removed)
            {
                target.GetType().GetMethod(nameof(IList.RemoveAt), [typeof(int)])?.Invoke(target, [removed]);
            }

            // Calling 'Add(ObjectType object)'
            if (Button("+", new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing())))
            {
                target.GetType().GetMethod(nameof(IList.Add), [itemType])?.Invoke(target, [inspector.GetTypeData(itemType).NewObject()]);
            }
        }

        return true;
    }
}