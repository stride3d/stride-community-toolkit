using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// Decides which lines of a running example's output are shown. Shader and effect compilation warnings
/// are hidden unless <c>SHOW_WARNINGS</c> is <c>1</c> or <c>true</c>. Shared by both launchers.
/// </summary>
public static partial class OutputFilter
{
    // Source-generated rather than constructed at runtime: the matcher is built at compile time, so startup
    // pays neither the pattern parse nor the RegexOptions.Compiled IL emission
    [GeneratedRegex(@"\bwarning\b", RegexOptions.IgnoreCase)]
    private static partial Regex Warning();

    [GeneratedRegex(@"\b(effect|shader|hlsl|fx|mixin|compiler)\b.*\bwarning\b|\bwarning\b.*\b(effect|shader|hlsl|fx|mixin|compiler)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ShaderWarning();

    /// <summary>Whether <c>SHOW_WARNINGS</c> asks for every warning to be shown.</summary>
    public static bool ShowAllWarnings { get; } = Environment.GetEnvironmentVariable("SHOW_WARNINGS") is { } value
        && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the line is a compiler warning of any kind.</summary>
    public static bool IsWarning(string line) => Warning().IsMatch(line);

    /// <summary>Whether the line is a shader or effect warning that should be hidden.</summary>
    public static bool IsSuppressed(string line) => !ShowAllWarnings && IsWarning(line) && ShaderWarning().IsMatch(line);
}