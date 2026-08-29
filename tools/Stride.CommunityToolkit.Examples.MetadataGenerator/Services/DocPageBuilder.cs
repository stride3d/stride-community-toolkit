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
    /// Bootstrap grid classes for one gallery card: three across on a wide screen, two on a tablet.
    /// </summary>
    /// <remarks>
    /// Three, not four. The docs content column is narrow enough that a fourth card breaks titles
    /// mid-word - which is why the gallery also turns the affix off.
    /// </remarks>
    private const string GalleryColumnClasses = "col-xxl-4 col-md-6";

    /// <summary>
    /// How much of a description a card shows before it is cut.
    /// </summary>
    private const int GallerySummaryLength = 150;

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

        // No per-example package note. The old shared one named a fixed three packages and was wrong for
        // 19 of 62 projects, and generating an accurate one per page would restate what the `using`
        // directives at the top of the listing below already say. The landing pages explain it once.

        // Only link a screenshot that exists. Most examples have none yet (see plan §5), and a broken
        // image is worse than no image.
        if (ExistingMedia(example) is { Length: > 0 } media)
        {
            body.AppendLine($"![{title}]({DocPaths.MediaFolder}/{media})");
            body.AppendLine();
        }

        body.AppendLine($"View on [GitHub]({DocPaths.GitHubExamplesUrl}/{example.ProjectName}).");
        body.AppendLine();
        body.AppendLine(CodeInclude(example));

        return body.ToString();
    }

    /// <summary>
    /// Renders the visual gallery - every example as a Bootstrap card, grouped as the toc groups them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bootstrap 5 ships with the DocFX template, so the markup needs no extra CSS. Only stock classes
    /// are used; anything bespoke here would have to be maintained against future template upgrades.
    /// </para>
    /// <para>
    /// Every part of a card is emitted by its own <c>AppendCard*</c> method, so dropping the badge, the
    /// summary or the link is a matter of commenting out one line in <see cref="AppendCard"/>.
    /// </para>
    /// <para>
    /// An example without an image is left out entirely rather than shown with a placeholder: the point
    /// of this page is to be looked at, and a card with nothing to look at is worse than one less card.
    /// </para>
    /// </remarks>
    /// <param name="groups">The example groups, in toc order.</param>
    /// <returns>The full file content.</returns>
    public string BuildGallery(IReadOnlyList<(string Language, string Level, IReadOnlyList<ExampleMetadata> Examples)> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var page = new StringBuilder();

        page.AppendLine("---");
        page.AppendLine("generated: true");

        // The right-hand "In this article" strip would list all 56 card titles, which is noise rather
        // than an outline - and switching it off widens the content column enough for three cards.
        page.AppendLine("_disableAffix: true");
        page.AppendLine("---");
        page.AppendLine();
        page.AppendLine("# Code-Only Examples");
        page.AppendLine();
        page.AppendLine("Every code-only example, with a screenshot of what it actually renders. Each one is a complete, self-contained program you can copy and run.");
        page.AppendLine();
        page.AppendLine("Prefer a list? Each level has its own page, linked from the table of contents.");

        foreach (var group in groups)
        {
            var cards = group.Examples.Where(example => ExistingMedia(example) is not null).ToList();

            if (cards.Count == 0)
            {
                continue;
            }

            page.AppendLine();
            page.AppendLine($"## {DocPaths.LanguageName(group.Language)} {group.Level}");
            page.AppendLine();
            page.AppendLine("<div class=\"row g-4 mb-4\">");

            foreach (var example in cards)
            {
                AppendCard(page, example);
            }

            page.AppendLine("</div>");
        }

        return page.ToString();
    }

    /// <summary>
    /// Emits one card. Comment out a line to drop that part from every card.
    /// </summary>
    private void AppendCard(StringBuilder page, ExampleMetadata example)
    {
        page.AppendLine($"    <div class=\"{GalleryColumnClasses}\">");
        page.AppendLine("        <div class=\"card h-100\">");

        AppendCardImage(page, example);

        page.AppendLine("            <div class=\"card-body\">");

        //AppendCardTitle(page, example);
        AppendCardLinkTitle(page, example);
        AppendCardBadge(page, example);
        AppendCardText(page, example);

        page.AppendLine("            </div>");

        //AppendCardLink(page, example);

        page.AppendLine("        </div>");
        page.AppendLine("    </div>");
    }

    /// <summary>
    /// The screenshot. Sized and lazy-loaded because this page carries every example's image at once.
    /// </summary>
    /// <remarks>
    /// The explicit width and height are the capture resolution. They are not there to size the image -
    /// Bootstrap does that - but to reserve its aspect ratio so the page does not reflow as fifty-odd
    /// lazy images arrive.
    /// </remarks>
    private void AppendCardImage(StringBuilder page, ExampleMetadata example)
        => page.AppendLine(
            $"            <img src=\"{DocPaths.MediaFolder}/{ExistingMedia(example)}\" class=\"card-img-top\" " +
            $"alt=\"Screenshot of the {Escape(TitleOf(example))} example\" width=\"1280\" height=\"720\" loading=\"lazy\">");

    /// <summary>
    /// The title, as a real heading so the page keeps a usable document outline for search engines.
    /// </summary>
    private static void AppendCardTitle(StringBuilder page, ExampleMetadata example)
        => page.AppendLine($"                <h3 class=\"card-title h6\">{Escape(TitleOf(example))}</h3>");

    private static void AppendCardLinkTitle(StringBuilder page, ExampleMetadata example)
        => page.AppendLine($"                <h3 class=\"card-title h6\"><a class=\"stretched-link text-decoration-none text-body\" href=\"{example.Slug}.md\">{Escape(TitleOf(example))}</a></h3>");

    /// <summary>
    /// The category badge. Not the level - that is already the section heading above the card.
    /// </summary>
    private static void AppendCardBadge(StringBuilder page, ExampleMetadata example)
    {
        if (example.Category is not { Length: > 0 } category)
        {
            return;
        }

        page.AppendLine($"                <p><span class=\"badge text-bg-secondary\">{Escape(category)}</span></p>");
    }

    /// <summary>
    /// The one-line summary.
    /// </summary>
    private static void AppendCardText(StringBuilder page, ExampleMetadata example)
    {
        if (Summarise(example.Description?.GetValueOrDefault("en")) is not { Length: > 0 } summary)
        {
            return;
        }

        page.AppendLine($"                <p class=\"card-text\">{Escape(summary)}</p>");
    }

    /// <summary>
    /// The link. <c>stretched-link</c> makes the whole card clickable, so the text stays short.
    /// </summary>
    private static void AppendCardLink(StringBuilder page, ExampleMetadata example)
        => page.AppendLine(
            $"            <p class=\"px-3 mb-3\"><a class=\"stretched-link\" href=\"{example.Slug}.md\">Open example</a></p>");

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
        page.AppendLine("> [!NOTE]");
        page.AppendLine("> Each example references a handful of toolkit packages. The `using` directives at the top of");
        page.AppendLine("> every listing name them, and the linked project file on GitHub is authoritative. A few examples");
        page.AppendLine("> also need a third-party package - Box2D.NET, Jitter2, Myra or ImGui - which their page calls out.");
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
    /// <summary>
    /// The English title, falling back the same way the landing pages and the toc do.
    /// </summary>
    private static string TitleOf(ExampleMetadata example)
        => example.Title?.GetValueOrDefault("en") ?? example.ProjectName ?? example.Slug ?? string.Empty;

    /// <summary>
    /// <see cref="FirstSentence"/>, capped to what fits on a card.
    /// </summary>
    /// <remarks>
    /// A few descriptions open with a sentence long enough to unbalance the row, so a hard cap is
    /// needed on top of the sentence split. The cut lands on a word boundary.
    /// </remarks>
    private static string? Summarise(string? description)
    {
        if (FirstSentence(description) is not { Length: > 0 } text || text.Length <= GallerySummaryLength)
        {
            return FirstSentence(description);
        }

        var cut = text.LastIndexOf(' ', GallerySummaryLength);

        return string.Concat(text.AsSpan(0, cut > 0 ? cut : GallerySummaryLength), "...");
    }

    /// <summary>
    /// Escapes the characters that would otherwise break out of the card markup.
    /// </summary>
    private static string Escape(string value)
        => value.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);

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
    /// Builds the DocFX code include for an example's source.
    /// </summary>
    /// <remarks>
    /// The include stops before the <c>---example-metadata</c> block. Rendering it would show the
    /// reader a wall of YAML restating the description and concepts that are already on the page as
    /// prose, directly above the listing. DocFX accepts a line range on a code include, so the block is
    /// simply left outside it - no post-processing pass, and it behaves the same in <c>docfx serve</c>.
    ///
    /// When the block is not the last thing in the file the range cannot express "everything but the
    /// block", so the whole file is included instead. <see cref="MetadataValidator"/> warns in that case.
    /// </remarks>
    private static string CodeInclude(ExampleMetadata example)
    {
        var tag = DocPaths.CodeTag(example.EffectiveLanguage);
        var path = $"{DocPaths.ExamplesFolder}/{example.ProjectPath}";

        return example.BlockLocation.CanTrimBlock
            ? $"[!{tag}[]({path}?start=1&end={example.BlockLocation.CodeLineCount})]"
            : $"[!{tag}[]({path})]";
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