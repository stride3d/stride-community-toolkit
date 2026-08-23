using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Text;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Renders the markdown the docs command writes.
/// </summary>
/// <remarks>
/// The output deliberately matches the house style of the pages that were written by hand - H1, intro
/// prose, a "shows how to" bullet list, the additional-packages note, a screenshot, a GitHub link and a
/// code include - so a generated page and a hand-written one are not distinguishable to a reader.
/// </remarks>
public class DocPageBuilder(DirectoryInfo? mediaDirectory)
{
    /// <summary>
    /// Renders a complete example page, frontmatter included.
    /// </summary>
    /// <param name="example">The example to document.</param>
    /// <returns>The full file content.</returns>
    public string BuildExamplePage(ExampleMetadata example)
    {
        ArgumentNullException.ThrowIfNull(example);

        var page = new StringBuilder();

        page.AppendLine("---");
        page.AppendLine("generated: true");
        page.AppendLine($"slug: {example.Slug}");
        page.AppendLine("---");
        page.AppendLine();
        page.Append(BuildExampleBody(example));

        return page.ToString();
    }

    /// <summary>
    /// Renders just the tool-owned part of an example page - everything below the frontmatter.
    /// </summary>
    /// <param name="example">The example to document.</param>
    /// <returns>The body, which is also what a <c>generated: partial</c> region is replaced with.</returns>
    public string BuildExampleBody(ExampleMetadata example)
    {
        ArgumentNullException.ThrowIfNull(example);

        var title = example.Title?.GetValueOrDefault("en") ?? example.ProjectName ?? example.Slug ?? "Example";
        var body = new StringBuilder();

        body.AppendLine($"# {title}");
        body.AppendLine();

        if (example.Description?.GetValueOrDefault("en") is { Length: > 0 } description)
        {
            body.AppendLine(description.Trim());
            body.AppendLine();
        }

        if (example.Concepts is { Count: > 0 } concepts)
        {
            // The entry file is not always Program.cs: a file-based app is named after what it does.
            body.AppendLine($"The `{EntryFileName(example)}` file shows how to:");
            body.AppendLine();

            foreach (var concept in concepts)
            {
                body.AppendLine($"- {concept}");
            }

            body.AppendLine();
        }

        body.AppendLine($"[!INCLUDE [note-additional-packages]({DocPaths.IncludesFolder}/note-additional-packages.md)]");
        body.AppendLine();

        // Only link a screenshot that exists. Most examples have none yet (see plan §5), and a broken
        // image is worse than no image.
        if (ExistingMedia(example) is { Length: > 0 } media)
        {
            body.AppendLine($"![{title}]({DocPaths.MediaFolder}/{media})");
            body.AppendLine();
        }

        body.AppendLine($"View on [GitHub]({DocPaths.GitHubExamplesUrl}/{example.ProjectName}).");
        body.AppendLine();
        body.AppendLine($"[!{DocPaths.CodeTag(example.EffectiveLanguage)}[]({DocPaths.ExamplesFolder}/{example.ProjectPath})]");

        return body.ToString();
    }

    /// <summary>
    /// Renders a landing page listing every example in one language and level.
    /// </summary>
    /// <param name="language">The group's language.</param>
    /// <param name="level">The group's level.</param>
    /// <param name="examples">The group's examples, in order.</param>
    /// <returns>The full file content.</returns>
    public static string BuildLandingPage(string language, string level, IReadOnlyList<ExampleMetadata> examples)
    {
        ArgumentNullException.ThrowIfNull(examples);

        var languageName = DocPaths.LanguageName(language);
        var page = new StringBuilder();

        page.AppendLine("---");
        page.AppendLine("generated: true");
        page.AppendLine("---");
        page.AppendLine();
        page.AppendLine($"# {languageName} {level} Examples");
        page.AppendLine();
        page.AppendLine(Introduction(level, languageName));
        page.AppendLine();
        page.AppendLine("## Examples Overview");
        page.AppendLine();

        foreach (var example in examples)
        {
            var title = example.Title?.GetValueOrDefault("en") ?? example.ProjectName ?? example.Slug;
            var summary = FirstSentence(example.Description?.GetValueOrDefault("en"));

            page.AppendLine(summary is null
                ? $"- [{title}]({example.Slug}.md)"
                : $"- [{title}]({example.Slug}.md): {summary}");
        }

        page.AppendLine();
        page.AppendLine($"[!INCLUDE [basic-examples-outro]({DocPaths.IncludesFolder}/basic-examples-outro.md)]");

        return page.ToString();
    }

    /// <summary>
    /// Renders a frontmatter-only stub that forwards an old URL to its replacement.
    /// </summary>
    /// <param name="target">The markdown page to redirect to.</param>
    /// <returns>The full file content.</returns>
    /// <remarks>
    /// The target is written with its rendered <c>.html</c> extension, not <c>.md</c>. DocFX turns
    /// <c>redirect_url</c> into a meta-refresh in the generated HTML and copies the value through
    /// verbatim, so a <c>.md</c> target produces a redirect to a page that does not exist on the
    /// deployed site.
    /// </remarks>
    public static string BuildRedirectStub(string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        var page = new StringBuilder();

        page.AppendLine("---");
        page.AppendLine($"redirect_url: {Path.ChangeExtension(target, ".html")}");
        page.AppendLine("generated: true");
        page.AppendLine("---");

        return page.ToString();
    }

    /// <summary>
    /// Gets the introduction for a level's landing page.
    /// </summary>
    /// <remarks>
    /// Each is a complete sentence on its own, with no count spliced in. Composing prose around a
    /// number means getting singular and plural right in every branch for no real gain - the list
    /// below it already shows how many there are.
    /// </remarks>
    private static string Introduction(string level, string languageName) => level switch
    {
        MetadataVocabulary.GettingStarted =>
            $"Your first code-only Stride application in {languageName}: boilerplate, one helper call, and something on screen. Start here.",
        MetadataVocabulary.Beginner =>
            "One new idea at a time, on top of the base scene. Toolkit helpers only, with no engine extension points to understand first.",
        MetadataVocabulary.Intermediate =>
            "A Stride subsystem used directly, or several concepts combined. These assume you are comfortable with the basics.",
        MetadataVocabulary.Advanced =>
            "Custom engine extension points, third-party integration and multi-project architecture. The deepest material here.",
        _ =>
            "Playgrounds and demonstrations that are not teaching one specific lesson, worth a look once the rest makes sense."
    };

    /// <summary>
    /// Takes the first sentence of a description, for a one-line list entry.
    /// </summary>
    private static string? FirstSentence(string? description)
    {
        if (description is not { Length: > 0 })
        {
            return null;
        }

        // Descriptions are wrapped prose; collapse the newlines before looking for the sentence end.
        var text = string.Join(' ', description.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var stop = text.IndexOf(". ", StringComparison.Ordinal);

        return stop > 0 ? text[..(stop + 1)] : text;
    }

    /// <summary>
    /// Gets the entry file's name, which is not always <c>Program.cs</c>.
    /// </summary>
    private static string EntryFileName(ExampleMetadata example)
        => example.ProjectPath is { Length: > 0 } path ? Path.GetFileName(path) : "Program.cs";

    /// <summary>
    /// Gets the example's screenshot filename if the file is actually present.
    /// </summary>
    private string? ExistingMedia(ExampleMetadata example)
    {
        if (mediaDirectory is null || !mediaDirectory.Exists || example.EffectiveMedia is not { Length: > 0 } media)
        {
            return null;
        }

        return File.Exists(Path.Combine(mediaDirectory.FullName, media)) ? media : null;
    }
}