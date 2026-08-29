using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// Represents the metadata for a single example, extracted from its <c>---example-metadata</c> block.
/// </summary>
/// <remarks>
/// <para>
/// This is schema v1 (see <c>docs/contributing/examples/metadata-schema.md</c>). Every YAML-facing member is
/// nullable and settable on purpose: the type is a permissive parse target, and requirements are
/// enforced afterwards by <see cref="Services.MetadataValidator"/> so that every problem across every
/// example is reported in one pass. Using <c>required</c> members here would instead fail on the first
/// offending file with a deserializer error, which is exactly the experience the validator exists to
/// avoid.
/// </para>
/// <para>
/// Members marked <see cref="YamlIgnoreAttribute"/> are populated by the generator, not by the author.
/// The set of YAML-facing member names is also what
/// <see cref="Services.MetadataValidator"/> diffs against to detect unknown or mis-cased keys.
/// </para>
/// </remarks>
public class ExampleMetadata
{
    // --- identity ---------------------------------------------------------

    /// <summary>Gets or sets the short, shareable, kebab-case identifier used as the doc filename and URL.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the localised titles, keyed by language code (<c>en</c>, <c>cs</c>).</summary>
    public Dictionary<string, string>? Title { get; set; }

    // --- classification ---------------------------------------------------

    /// <summary>Gets or sets the teaching level. One of <see cref="Core.MetadataVocabulary.Levels"/>.</summary>
    public string? Level { get; set; }

    /// <summary>Gets or sets the topic. One of <see cref="Core.MetadataVocabulary.Categories"/>.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the relative difficulty, 1 to 5.</summary>
    public int? Complexity { get; set; }

    /// <summary>Gets or sets the sort order within the example's <c>(language, level)</c> group.</summary>
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the source language. One of <see cref="Core.MetadataVocabulary.Languages"/>;
    /// defaults to <c>csharp</c> when omitted.
    /// </summary>
    public string? Language { get; set; }

    // --- content ----------------------------------------------------------

    /// <summary>Gets or sets the localised descriptions, keyed by language code (<c>en</c>, <c>cs</c>).</summary>
    public Dictionary<string, string>? Description { get; set; }

    /// <summary>Gets or sets the bullet list rendered as "The Program.cs file shows how to:".</summary>
    public List<string>? Concepts { get; set; }

    /// <summary>Gets or sets the free-form topic tags. The <see cref="Level"/> does not belong here.</summary>
    public List<string>? Tags { get; set; }

    /// <summary>Gets or sets related examples, as project names. Resolved into <see cref="RelatedSlugs"/>.</summary>
    public List<string>? Related { get; set; }

    // --- docs generation --------------------------------------------------

    /// <summary>Gets or sets whether a doc page is generated for this example. Defaults to <see langword="true"/>.</summary>
    public bool? Docs { get; set; }

    /// <summary>
    /// Gets or sets the screenshot filename, resolved against the docs media folder.
    /// Defaults to <c>&lt;slug&gt;.webp</c> when omitted.
    /// </summary>
    public string? Media { get; set; }

    /// <summary>Gets or sets the display name used in <c>toc.yml</c>. Falls back to the English title.</summary>
    public string? TocName { get; set; }

    /// <summary>
    /// Gets or sets whether the automated screenshot run captures this example.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Set to <see langword="false"/> for anything that cannot produce a meaningful frame on its own:
    /// the SignalR pair needs a running server, and an input-driven example shows an empty scene until
    /// someone presses a key.
    /// </remarks>
    public bool? Screenshot { get; set; }

    /// <summary>
    /// Gets or sets which frame the screenshot run captures. Defaults to the capture system's own value.
    /// </summary>
    /// <remarks>
    /// A frame index, not a delay. Raise it for a scene that needs longer to settle, lower it for one
    /// that has already scattered by the default.
    /// </remarks>
    public int? ScreenshotFrame { get; set; }

    // --- launchers --------------------------------------------------------

    /// <summary>Gets or sets whether the example appears in the launchers. Defaults to <see langword="true"/>.</summary>
    public bool? Launcher { get; set; }

    // --- lifecycle --------------------------------------------------------

    /// <summary>
    /// Gets or sets whether the example is published at all. When <see langword="false"/> the example is
    /// excluded from the manifest entirely. Defaults to <see langword="true"/>.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>Gets or sets the authoring date, <c>yyyy-MM-dd</c>.</summary>
    public string? Created { get; set; }

    // --- populated by the generator ---------------------------------------

    /// <summary>Gets or sets the project directory name, for example <c>Example13_MeshOutline</c>.</summary>
    [YamlIgnore]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the entry file path relative to the examples root, using forward slashes so the
    /// manifest is identical on every platform.
    /// </summary>
    [YamlIgnore]
    public string? ProjectPath { get; set; }

    /// <summary>Gets or sets <see cref="Related"/> resolved to slugs. Populated after validation.</summary>
    [YamlIgnore]
    public List<string>? RelatedSlugs { get; set; }

    /// <summary>
    /// Gets or sets where the metadata block sits in the source file, so the documentation can include
    /// the code without it.
    /// </summary>
    [YamlIgnore]
    [JsonIgnore]
    public Core.MetadataBlockLocation BlockLocation { get; set; }

    /// <summary>Gets the effective language, applying the <c>csharp</c> default.</summary>
    /// <remarks>A convenience for the generator only. It is kept out of the manifest because it would
    /// duplicate <see cref="Language"/>, which the parser already fills in from the file extension.</remarks>
    [YamlIgnore]
    [JsonIgnore]
    public string EffectiveLanguage => Language ?? Core.MetadataVocabulary.DefaultLanguage;

    /// <summary>Gets the effective media filename, applying the <c>&lt;slug&gt;.webp</c> default.</summary>
    /// <remarks>A convenience for the docs generator only. It is kept out of the manifest because a
    /// defaulted name would assert a screenshot that, for most examples, does not exist yet.</remarks>
    [YamlIgnore]
    [JsonIgnore]
    public string? EffectiveMedia => Media ?? (Slug is null ? null : $"{Slug}.webp");

    /// <summary>The language every localised value falls back to.</summary>
    public const string FallbackLanguage = "en";

    /// <summary>Gets the title in the requested language, falling back to English.</summary>
    /// <param name="language">The language code, for example <c>cs</c>.</param>
    /// <returns>The best available title, or <see langword="null"/> if there is none at all.</returns>
    public string? TitleFor(string language) => Localised(Title, language);

    /// <summary>Gets the description in the requested language, falling back to English.</summary>
    /// <param name="language">The language code, for example <c>cs</c>.</param>
    /// <returns>The best available description, or <see langword="null"/> if there is none at all.</returns>
    public string? DescriptionFor(string language) => Localised(Description, language);

    /// <summary>
    /// Picks a localised value, falling back to English when the requested language is absent.
    /// </summary>
    /// <remarks>
    /// Translations are optional and partial by design: <c>cs</c> is consumed only by the launchers,
    /// never by the docs (D5), and most examples will carry English alone. A missing translation must
    /// therefore read as "not translated yet" and show the English, never as a blank label. Putting the
    /// rule here rather than in each consumer is what stops one of them forgetting it.
    /// </remarks>
    private static string? Localised(Dictionary<string, string>? values, string language)
    {
        if (values is null)
        {
            return null;
        }

        if (values.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return values.TryGetValue(FallbackLanguage, out var fallback) && !string.IsNullOrWhiteSpace(fallback)
            ? fallback
            : null;
    }
}