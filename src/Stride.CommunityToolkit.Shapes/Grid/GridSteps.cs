using System.Globalization;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The arithmetic of a reference grid: which step to draw lines at, which lines fall inside a
/// range, and how to write a line's value.
/// </summary>
internal static class GridSteps
{
    /// <summary>How far apart, in pixels, the major lines are aimed to be when the step is automatic.</summary>
    internal const float TargetMajorPixels = 100f;

    /// <summary>Above this many lines in one direction the minor lines are left out.</summary>
    internal const int MaximumLines = 240;

    /// <summary>
    /// The smallest step of the form 1, 2 or 5 times a power of ten that is at least
    /// <paramref name="raw"/>, so that a grid's lines land on round numbers at any zoom.
    /// </summary>
    /// <param name="raw">The step wanted, such as the world distance a hundred pixels cover.</param>
    /// <returns>The round step, or 1 when <paramref name="raw"/> is not a positive number.</returns>
    internal static float Nice(float raw)
    {
        if (!(raw > 0f) || float.IsInfinity(raw)) return 1f;

        var power = MathF.Pow(10f, MathF.Floor(MathF.Log10(raw)));
        var fraction = raw / power;

        // A little slack, so that a raw step of 2 computed as 2.0000002 is still 2 and not 5
        var nice = fraction <= 1.0001f ? 1f : fraction <= 2.0001f ? 2f : fraction <= 5.0001f ? 5f : 10f;

        return nice * power;
    }

    /// <summary>
    /// How many minor steps to cut a major step into so that the minor lines land on round numbers
    /// too: four for a step that starts with 2 (2 gives 0.5), five for one that starts with 1 or 5
    /// (1 gives 0.2, 5 gives 1).
    /// </summary>
    internal static int Divisions(float major)
    {
        if (!(major > 0f) || float.IsInfinity(major)) return 5;

        var leading = major / MathF.Pow(10f, MathF.Floor(MathF.Log10(major)));

        return leading is > 1.5f and < 3.5f ? 4 : 5;
    }

    /// <summary>
    /// The indices of the lines inside a range: line <c>i</c> lies at <c>i * step</c>.
    /// </summary>
    /// <returns>The first and last index, with <c>Last</c> below <c>First</c> when no line fits.</returns>
    internal static (int First, int Last) Range(float min, float max, float step)
    {
        if (!(step > 0f)) return (0, -1);

        return ((int)MathF.Ceiling(min / step - 0.0001f), (int)MathF.Floor(max / step + 0.0001f));
    }

    /// <summary>How many decimals a value needs to be told apart from its neighbour one step away.</summary>
    private static int Decimals(float step)
        => step >= 1f || !(step > 0f) ? 0 : (int)MathF.Ceiling(-MathF.Log10(step) - 0.0001f);

    /// <summary>A line's value as text, with the decimals its step needs and never a negative zero.</summary>
    internal static string Format(float value, float step)
    {
        var text = value.ToString("F" + Decimals(step), CultureInfo.InvariantCulture);

        return text.TrimStart('-').Trim('0', '.').Length == 0 ? text.TrimStart('-') : text;
    }
}