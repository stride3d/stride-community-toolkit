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
internal sealed class InspectorCollectionsView
{
    private readonly Inspector _inspector;

    // Cache to handle dictionary add() commands
    private readonly WeakReference<object?> _dicAddCommandTarget = new(null);
    private (object? key, object? value) _dicAddCommandData;

    internal InspectorCollectionsView(Inspector inspector)
    {
        _inspector = inspector;
    }

    internal void Draw(object target, IEnumerable ienum, int hashcodeSource)
    {
        using (UIndent())
        {
            if (TryDrawAsIList(target, hashcodeSource))
                return;

            if (TryDrawAsIDictionary(target, hashcodeSource))
                return;

            Spacing();
            TextDisabled("As Enumerable");
            int index = 0;
            try
            {
                foreach (object? o in ienum)
                {
                    object? o2 = o;
                    using (UIndent())
                        _inspector.DrawValue("-", ref o2, true, (hashcodeSource, index).GetHashCode());
                    index++;
                }
            }
            catch (Exception e)
            {
                object? str = $"x Exception: {e.Message}";
                using (UIndent())
                    _inspector.DrawValue("-", ref str, true, (hashcodeSource, index).GetHashCode());
            }
        }
        Spacing();
    }

    private bool TryDrawAsIDictionary(object target, int hashcodeSource)
    {
        var typeData = _inspector.GetTypeData(target.GetType());
        if (typeData.AsDictionary == null)
            return false;
        var data = typeData.AsDictionary.Value;
        Spacing();
        TextDisabled("As Dictionary");
        // Most of the management here is done through reflection
        // as the type might not implement IDictionary but just IDictionary<T> ...
        using (UIndent())
        {
            if (!DrawDictionaryEntries(target, data, hashcodeSource))
                return false;

            DrawDictionaryAddControls(target, data, hashcodeSource);
        }

        return true;
    }

    private bool DrawDictionaryEntries(object target, (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue) data, int hashcodeSource)
    {
              // IDictionary.Keys
                var keys = (data.getKey?.Invoke(target, null) as IEnumerable)?.GetEnumerator();
                // IDictionary.Values
                var values = (data.getValue?.Invoke(target, null) as IEnumerable)?.GetEnumerator();
                if (keys == null || values == null)
                    return false;

                bool removeKey = false;
                object? keyToRemove = null;

                bool changeKey = false;
                object? keyToChange = null;
                object? valueOfKeyToChange = null;

                int index = 0;
                while (keys.MoveNext() && values.MoveNext())
                {
                    var key = keys.Current;
                    var value = values.Current;
                    // hashcode with index: key is guaranteed to be unique and constant but not its ToString()
                    int newHash = (hashcodeSource, index).GetHashCode();
                    using (ID(newHash))
                    {
                        SetCursorPosX(GetCursorPosX() - Inspector.DUMMY_WIDTH);
                        if (Button("x"))
                        {
                            removeKey = true;
                            keyToRemove = key;
                        }
                    }
                    SameLine();
                    if (_inspector.DrawValue(key?.ToString() ?? "null", ref value, false, newHash))
                    {
                        changeKey = true;
                        keyToChange = key;
                        valueOfKeyToChange = value;
                    }

                    index++;
                }

                if (removeKey)
                {
                    target.GetType().GetMethod(nameof(IDictionary.Remove), [data.key])
                        ?.Invoke(target, [keyToRemove]);
                }

                if (changeKey)
                {
                    // IDictionary[ keyToChange ] = valueOfKeyToChange
                    var parameters = new[] { keyToChange, valueOfKeyToChange };
                    target.GetType().GetProperty("Item", data.value, [data.key])?
                        .SetMethod?.Invoke(target, parameters);
                }
        return true;
    }

    private void DrawDictionaryAddControls(object target, (Type key, Type value, MethodInfo? getKey, MethodInfo? getValue) data, int hashcodeSource)
    {
        // Show upcoming key and value
            if (_dicAddCommandTarget.TryGetTarget(out var addActionTarget) && addActionTarget == target)
            {
                (object? key, object? value) = _dicAddCommandData;
                _inspector.DrawValue("Upcoming Key:", ref key, false, hashcodeSource);
                _inspector.DrawValue("Upcoming Value:", ref value, false, hashcodeSource);
                _dicAddCommandData = (key, value);
                if (Button("Cancel"))
                {
                    _dicAddCommandData = (null, null);
                    _dicAddCommandTarget.SetTarget(null);
                }
                SameLine();
                if (Button("Add"))
                {
                    var parameters = new[] { key, value };
                    target.GetType().GetMethod(nameof(IDictionary.Add), [data.key, data.value])
                        ?.Invoke(target, parameters);

                    _dicAddCommandData = (null, null);
                    _dicAddCommandTarget.SetTarget(null);
                }
            }
            // Prepare an add to the dictionary: create an editable instance for key and value
            else if (Button("+", new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing())))
            {
                _dicAddCommandData = (_inspector.GetTypeData(data.key).NewObject(), _inspector.GetTypeData(data.value).NewObject());
                _dicAddCommandTarget.SetTarget(target);
            }
    }

    private bool TryDrawAsIList(object target, int hashcodeSource)
    {
        var typeData = _inspector.GetTypeData(target.GetType());
        if (typeData.AsList == null)
            return false;
        Spacing();
        TextDisabled("As List");
        // Most of the management here is done through reflection
        // as the type might not implement IList but just IList<T> ...
        using (UIndent())
        {
            int i = 0;
            int? indexToRemove = null;
            int? indexToChange = null;
            object? objectToAssign = null;
            foreach (object? o in (IEnumerable)target)
            {
                object? o2 = o;
                using (ID($"{o}{i}"))
                {
                    SetCursorPosX(GetCursorPosX() - Inspector.DUMMY_WIDTH);
                    if (Button("x"))
                        indexToRemove = i;
                }
                SameLine();
                if (_inspector.DrawValue($"{i}:", ref o2, false, hashcodeSource))
                {
                    indexToChange = i;
                    objectToAssign = o2;
                }

                i++;
            }

            // Calling 'this[int indexToChange] = objectToAssign'
            if (indexToChange != null)
            {
                MethodInfo? listAccessor;
                if (target.GetType().IsArray)
                {
                    listAccessor = target.GetType().GetMethod("SetValue", [typeof(object), typeof(int)]);
                    listAccessor?.Invoke(target, [objectToAssign, indexToChange.Value]);
                }
                else
                {
                    listAccessor = target.GetType().GetProperty("Item", typeData.AsList, [typeof(int)])?.SetMethod;
                    listAccessor?.Invoke(target, [indexToChange.Value, objectToAssign]);
                }
                if (listAccessor == null)
                    System.Console.WriteLine($"Couldn't find {nameof(listAccessor)} for {target.GetType()}");
            }
            // Calling 'RemoveAt(int index)'
            if (indexToRemove != null)
            {
                target.GetType().GetMethod(nameof(IList.RemoveAt), [typeof(int)])?.Invoke(target, [indexToRemove.Value]);
            }

            // Calling 'Add(ObjectType object)'
            if (Button("+", new Vector2(GetContentRegionAvail().X, GetTextLineHeightWithSpacing())))
            {
                var valueType = typeData.AsList;
                var value = _inspector.GetTypeData(typeData.AsList).NewObject();
                target.GetType().GetMethod(nameof(IList.Add), [valueType])?.Invoke(target, [value]);
            }
        }

        return true;
    }
}