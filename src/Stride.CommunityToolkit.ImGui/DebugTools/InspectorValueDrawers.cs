using System.Diagnostics.CodeAnalysis;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// The leaf-value widgets <see cref="Inspector"/> draws: one ImGui control per primitive type, the
/// enum combo, and the bit twiddling enum editing needs. Split out of <see cref="Inspector"/>;
/// everything here is stateless.
/// </summary>
internal static class InspectorValueDrawers
{
    /// <summary>
    /// Draws the editor for a primitive scalar value. Returns <see langword="true"/> when
    /// <paramref name="value"/> is of a type handled here - whether or not it changed;
    /// <paramref name="valueChanged"/> says whether it did.
    /// </summary>
    internal static bool TryDrawScalar(ref object? value, out bool valueChanged)
    {
        switch (value)
        {
            // if(valueChanged) => to cast / generate garbage only when the value changed
            case bool v: valueChanged = Checkbox("", ref v); if (valueChanged) { value = v; } return true;
            case string v: valueChanged = InputText("", ref v, 99); if (valueChanged) { value = v; } return true;
            case float v: valueChanged = DragFloat("", ref v, RelativeDragSpeed(v)); if (valueChanged) { value = v; } return true;
            case double v: valueChanged = InputDouble("", ref v); if (valueChanged) { value = v; } return true;
            case int v: valueChanged = InputInt("", ref v); if (valueChanged) { value = v; } return true;
            default: return TryDrawIntegerScalar(ref value, out valueChanged);
        }
    }

    /// <summary>
    /// The scalar types ImGui has no native widget for: edited as int and cast back afterwards.
    /// </summary>
    private static bool TryDrawIntegerScalar(ref object? value, out bool valueChanged)
    {
        // Every case follows the same shape: edit through the closest type ImGui implements
        // natively (int), then cast back so the boxed value keeps its original type.
        switch (value)
        {
            case uint v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (uint)c; } return true; }
            case long v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (long)c; } return true; }
            case ulong v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (ulong)c; } return true; }
            case short v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (short)c; } return true; }
            case ushort v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (ushort)c; } return true; }
            case byte v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (byte)c; } return true; }
            case sbyte v: { var c = (int)v; valueChanged = InputInt("", ref c); if (valueChanged) { value = (sbyte)c; } return true; }
            default: valueChanged = false; return false;
        }
    }

    /// <summary>
    /// Draws the combo for an enum value, multi-select when the enum carries <see cref="FlagsAttribute"/>.
    /// </summary>
    internal static bool DrawEnum((bool flags, Array values) enumInfo, ref object? value)
    {
        var valueChanged = false;
        var (flags, values) = enumInfo;

        using (UCombo("", value?.ToString() ?? string.Empty, out var open))
        {
            if (!open) return false;

            foreach (var o in values)
            {
                var fieldValue = GetEnumBits(value!);
                var compValue = GetEnumBits(o);
                var selected = flags ? (fieldValue & compValue) == compValue : fieldValue == compValue;

                if (!Selectable(o.ToString(), selected)) continue;

                // A flag is toggled; a plain value replaces the whole thing
                if (flags) compValue = selected ? fieldValue & ~compValue : fieldValue | compValue;

                value = GetEnumValueFromBits(compValue, value!.GetType());
                valueChanged = true;
            }

            return valueChanged;
        }
    }

    // The value's bits as an unsigned long, whatever its underlying type; casts through the signed type keep the bit pattern
    private static ulong GetEnumBits(object enumValue)
    {
        var valueType = enumValue.GetType();

        if (valueType.IsEnum) valueType = Enum.GetUnderlyingType(valueType);

        return Type.GetTypeCode(valueType) switch
        {
            TypeCode.Int32 => (ulong)(int)enumValue,
            TypeCode.UInt32 => (uint)enumValue,
            TypeCode.Int64 => (ulong)(long)enumValue,
            TypeCode.UInt64 => (ulong)enumValue,
            TypeCode.Int16 => (ulong)(short)enumValue,
            TypeCode.UInt16 => (ushort)enumValue,
            TypeCode.Byte => (byte)enumValue,
            TypeCode.SByte => (ulong)(sbyte)enumValue,
            _ => throw new ArgumentException(valueType.ToString()),
        };
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "The method exists to box an arbitrary enum's underlying value; there is no single concrete return type.")]
    private static object GetEnumValueFromBits(ulong bits, Type enumType)
    {
        if (!enumType.IsEnum) throw new ArgumentException(enumType.ToString());

        return Type.GetTypeCode(Enum.GetUnderlyingType(enumType)) switch
        {
            TypeCode.Int32 => (int)bits,
            TypeCode.UInt32 => (uint)bits,
            TypeCode.Int64 => (long)bits,
            TypeCode.UInt64 => bits,
            TypeCode.Int16 => (short)bits,
            TypeCode.UInt16 => (ushort)bits,
            TypeCode.Byte => (byte)bits,
            TypeCode.SByte => (sbyte)bits,
            _ => throw new ArgumentException(enumType.ToString()),
        };
    }

    // A tenth of the value per pixel, so large values move fast and small ones move finely
    private static float RelativeDragSpeed(float currentValue) => MathF.Max(MathF.Abs(currentValue) * 0.1f, 0.001f);
}