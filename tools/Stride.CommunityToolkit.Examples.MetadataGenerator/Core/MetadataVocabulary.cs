using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// The closed sets of values the metadata schema accepts.
/// </summary>
/// <remarks>
/// This is the single place new levels, categories or languages are added, as promised by
/// <c>docs/contributing/examples/metadata-schema.md</c>. Matching is case- and spelling-exact: <c>Beginners</c>
/// is an error rather than a synonym for <c>Beginner</c>, because a silently accepted variant produces
/// a second landing page that nobody notices.
/// </remarks>
public static class MetadataVocabulary
{
    /// <summary>The language assumed when <c>language:</c> is omitted.</summary>
    public const string DefaultLanguage = "csharp";

    /// <summary>Your first code-only Stride app.</summary>
    public const string GettingStarted = "Getting Started";

    /// <summary>One new concept on top of the base scene.</summary>
    public const string Beginner = "Beginner";

    /// <summary>A Stride subsystem used directly, or several concepts combined.</summary>
    public const string Intermediate = "Intermediate";

    /// <summary>Engine extension points, third-party integration, or multi-project work.</summary>
    public const string Advanced = "Advanced";

    /// <summary>Published but unclassified. Sorts last.</summary>
    public const string Other = "Other";

    /// <summary>
    /// Teaching levels, in presentation order. <c>Other</c> means "published but unclassified" and
    /// sorts last; it is not a substitute for <c>enabled: false</c> or <c>docs: false</c>.
    /// </summary>
    public static readonly string[] Levels = [GettingStarted, Beginner, Intermediate, Advanced, Other];

    /// <summary>
    /// Topic categories. A category names the *lesson*, not the scenery: a keyboard-menu example that
    /// happens to spawn shapes is <c>Input</c>, and an instancing example is <c>Performance</c> whether it
    /// draws cubes or physics bodies. <c>Geometry</c> and <c>Debug</c> have no members yet but are
    /// earmarked for the <c>Example05_*</c> and <c>Example08_*</c> families.
    /// </summary>
    public static readonly string[] Categories =
    [
        "Shapes",
        "Geometry",
        "Physics",
        "Rendering",
        "Performance",
        "Text",
        "UI",
        "Input",
        "Scripts",
        "Networking",
        "Debug",
        "Game"
    ];

    /// <summary>Source languages, matching the file extensions the scanner understands.</summary>
    public static readonly string[] Languages = ["csharp", "fsharp", "vb"];

    /// <summary>The lowest accepted <c>complexity</c>.</summary>
    public const int MinComplexity = 1;

    /// <summary>The highest accepted <c>complexity</c>.</summary>
    public const int MaxComplexity = 5;

    /// <summary>
    /// Every key the schema recognises, in the camelCase form used in YAML.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="ExampleMetadata"/> itself so the two cannot drift, excluding members
    /// marked <see cref="YamlIgnoreAttribute"/>, which the generator populates rather than the author.
    /// </remarks>
    public static readonly IReadOnlySet<string> KnownKeys = BuildKnownKeys();

    /// <summary>
    /// Suggests the correctly spelled key for an unrecognised one, matching case-insensitively.
    /// </summary>
    /// <param name="unknownKey">The key as it was written in the YAML.</param>
    /// <returns>The known key it most likely meant, or <see langword="null"/> if there is no close match.</returns>
    public static string? SuggestKey(string unknownKey)
        => KnownKeys.FirstOrDefault(known => string.Equals(known, unknownKey, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlySet<string> BuildKnownKeys()
    {
        var namingConvention = CamelCaseNamingConvention.Instance;

        var keys = typeof(ExampleMetadata)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<YamlIgnoreAttribute>() is null)
            .Select(property => namingConvention.Apply(property.Name));

        return new HashSet<string>(keys, StringComparer.Ordinal);
    }
}