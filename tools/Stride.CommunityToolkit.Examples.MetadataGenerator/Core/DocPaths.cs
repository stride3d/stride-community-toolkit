namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// The filenames, relative paths and labels the documentation site uses.
/// </summary>
/// <remarks>
/// Every path is relative to <c>docs/manual/code-only/examples/</c>, which is where every generated
/// page lives. Keeping them in one place means the depth of that folder is stated once rather than
/// counted out in each template.
/// </remarks>
public static class DocPaths
{
    /// <summary>The shared includes folder, from an example page.</summary>
    public const string IncludesFolder = "../../../includes/manual/examples";

    /// <summary>The examples source tree, from an example page.</summary>
    public const string ExamplesFolder = "../../../../examples/code-only";

    /// <summary>Where example screenshots live, from an example page.</summary>
    public const string MediaFolder = "media";

    /// <summary>The repository, for "View on GitHub" links.</summary>
    public const string GitHubExamplesUrl = "https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only";

    /// <summary>The landing pages that existed before levels replaced the Basic/Advance split.</summary>
    /// <remarks>
    /// They stay in the repository as <c>redirect_url</c> stubs. The redirect is a generated HTML page,
    /// not a build-time rewrite, so deleting the file would break the URL rather than redirect it.
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, string> LegacyLandingPages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["basic-examples.md"] = "getting-started-examples.md",
        ["advance-examples.md"] = "advanced-examples.md",
        ["basic-examples-fs.md"] = "getting-started-examples-fs.md",
        ["basic-examples-vb.md"] = "getting-started-examples-vb.md"
    };

    /// <summary>
    /// Gets the file suffix that distinguishes a language's pages, empty for C#.
    /// </summary>
    /// <param name="language">One of <c>csharp</c>, <c>fsharp</c>, <c>vb</c>.</param>
    /// <returns>The suffix, for example <c>-fs</c>.</returns>
    public static string LanguageSuffix(string language) => language switch
    {
        "fsharp" => "-fs",
        "vb" => "-vb",
        _ => string.Empty
    };

    /// <summary>
    /// Gets the display name of a language.
    /// </summary>
    /// <param name="language">One of <c>csharp</c>, <c>fsharp</c>, <c>vb</c>.</param>
    /// <returns>The name used in page titles and toc entries.</returns>
    public static string LanguageName(string language) => language switch
    {
        "fsharp" => "F#",
        "vb" => "Visual Basic",
        _ => "C#"
    };

    /// <summary>
    /// Gets the DocFX code-include tag for a language.
    /// </summary>
    /// <param name="language">One of <c>csharp</c>, <c>fsharp</c>, <c>vb</c>.</param>
    /// <returns>The tag name, for example <c>code-fsharp</c>.</returns>
    public static string CodeTag(string language) => language switch
    {
        "fsharp" => "code-fsharp",
        "vb" => "code-vb",
        _ => "code-csharp"
    };

    /// <summary>
    /// Gets the landing page filename for a language and level group.
    /// </summary>
    /// <param name="language">The group's language.</param>
    /// <param name="level">The group's level.</param>
    /// <returns>A filename such as <c>intermediate-examples-fs.md</c>.</returns>
    public static string LandingPage(string language, string level)
        => $"{Slugify(level)}-examples{LanguageSuffix(language)}.md";

    /// <summary>
    /// Turns a display value into a kebab-case filename part.
    /// </summary>
    /// <param name="value">For example <c>Getting Started</c>.</param>
    /// <returns>For example <c>getting-started</c>.</returns>
    public static string Slugify(string value)
        => string.Join('-', value.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}