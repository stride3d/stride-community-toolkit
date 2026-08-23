namespace Stride.CommunityToolkit.Examples.Core;

/// <summary>
/// The <c>examples-manifest.json</c> document, as the launchers read it.
/// </summary>
/// <remarks>
/// <para>
/// This is a deliberate second copy of the shape the metadata generator writes, rather than a project
/// reference to it. The generator is a console application carrying a generic host, Serilog and
/// YamlDotNet; a launcher needs none of that to read a JSON file. The two are coupled by the file
/// format and by <see cref="SchemaVersion"/>, which is checked on load, not by a shared assembly.
/// </para>
/// <para>
/// Only the fields a launcher actually uses are declared. Unknown properties are ignored by
/// <c>System.Text.Json</c>, so the generator can add fields without breaking either launcher.
/// </para>
/// </remarks>
public sealed class ExampleManifest
{
    /// <summary>The schema version this code understands.</summary>
    public const int SupportedSchemaVersion = 1;

    /// <summary>Gets or sets the schema version of the document.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>Gets or sets when the manifest was generated, in ISO 8601.</summary>
    public string? GeneratedAt { get; set; }

    /// <summary>Gets or sets the examples, already ordered by language, level and order.</summary>
    public List<ManifestExample> Examples { get; set; } = [];
}

/// <summary>
/// One example, as described by the manifest.
/// </summary>
public sealed class ManifestExample
{
    /// <summary>Gets or sets the short, unique, kebab-case identifier.</summary>
    public string? Slug { get; set; }

    /// <summary>Gets or sets the localised titles, keyed by language code.</summary>
    public Dictionary<string, string>? Title { get; set; }

    /// <summary>Gets or sets the localised descriptions, keyed by language code.</summary>
    public Dictionary<string, string>? Description { get; set; }

    /// <summary>Gets or sets the teaching level, for example <c>Beginner</c>.</summary>
    public string? Level { get; set; }

    /// <summary>Gets or sets the topic, for example <c>Physics</c>.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the relative difficulty, 1 to 5.</summary>
    public int? Complexity { get; set; }

    /// <summary>Gets or sets the sort order within the example's language and level group.</summary>
    public int? Order { get; set; }

    /// <summary>Gets or sets the source language, one of <c>csharp</c>, <c>fsharp</c> or <c>vb</c>.</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the free-form topic tags.</summary>
    public List<string>? Tags { get; set; }

    /// <summary>Gets or sets whether the example is shown in the launchers. Defaults to <see langword="true"/>.</summary>
    public bool? Launcher { get; set; }

    /// <summary>Gets or sets the project directory name.</summary>
    public string? ProjectName { get; set; }

    /// <summary>Gets or sets the entry file, relative to the examples root, with forward slashes.</summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// Gets the title for a language, falling back to English.
    /// </summary>
    /// <param name="language">A two-letter language code, for example <c>cs</c>.</param>
    /// <returns>The best available title, or <see langword="null"/> if there is none.</returns>
    public string? TitleFor(string language) => Localised(Title, language);

    /// <summary>
    /// Gets the description for a language, falling back to English.
    /// </summary>
    /// <param name="language">A two-letter language code, for example <c>cs</c>.</param>
    /// <returns>The best available description, or <see langword="null"/> if there is none.</returns>
    public string? DescriptionFor(string language) => Localised(Description, language);

    /// <summary>
    /// Picks a localised value, falling back to English when the requested language is absent.
    /// </summary>
    /// <remarks>
    /// Translations are optional and partial by design - most examples carry English only - so a
    /// missing one must read as "not translated yet" and show the English, never as a blank label.
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

        return values.TryGetValue("en", out var fallback) && !string.IsNullOrWhiteSpace(fallback)
            ? fallback
            : null;
    }
}
