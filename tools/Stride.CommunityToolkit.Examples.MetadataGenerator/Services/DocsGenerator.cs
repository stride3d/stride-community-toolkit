using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Text;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Writes the example documentation: one page per example, a landing page per group, the folder's
/// table of contents, and redirect stubs for the URLs that levels replaced.
/// </summary>
public class DocsGenerator(ILogger<DocsGenerator> logger)
{
    private int _written;
    private int _skipped;
    private int _unchanged;

    /// <summary>
    /// Generates every documentation file for a set of examples.
    /// </summary>
    /// <param name="examples">The published examples, in manifest order.</param>
    /// <param name="docsDirectory">The <c>docs/manual/code-only/examples</c> folder.</param>
    /// <param name="mediaDirectory">The screenshot folder, used to decide whether to link an image.</param>
    /// <param name="dryRun">When <see langword="true"/>, report what would change and write nothing.</param>
    /// <returns>The number of files written, or that would be written.</returns>
    public int Generate(
        IReadOnlyList<ExampleMetadata> examples,
        DirectoryInfo docsDirectory,
        DirectoryInfo? mediaDirectory,
        bool dryRun)
    {
        ArgumentNullException.ThrowIfNull(examples);
        ArgumentNullException.ThrowIfNull(docsDirectory);

        _written = 0;
        _skipped = 0;
        _unchanged = 0;

        if (!docsDirectory.Exists)
        {
            throw new InvalidOperationException($"Docs directory does not exist: {docsDirectory.FullName}");
        }

        var builder = new DocPageBuilder(mediaDirectory);
        var documented = examples.Where(example => example.Docs != false).ToList();

        foreach (var example in documented)
        {
            WriteExamplePage(builder, example, docsDirectory, dryRun);
        }

        WriteGallery(builder, documented, docsDirectory, dryRun);
        WriteLandingPages(documented, docsDirectory, dryRun);
        WriteRedirectStubs(docsDirectory, dryRun);
        WriteTableOfContents(documented, docsDirectory, dryRun);

        logger.LogInformation(
            "Docs {Verb}: {Written} written, {Unchanged} already current, {Skipped} hand-owned and left alone",
            dryRun ? "preview" : "generation", _written, _unchanged, _skipped);

        return _written;
    }

    /// <summary>
    /// Writes one example's page, honouring whoever owns the file.
    /// </summary>
    private void WriteExamplePage(DocPageBuilder builder, ExampleMetadata example, DirectoryInfo docsDirectory, bool dryRun)
    {
        if (example.Slug is not { Length: > 0 } slug)
        {
            return;
        }

        var path = Path.Combine(docsDirectory.FullName, $"{slug}.md");

        if (!File.Exists(path))
        {
            Write(path, builder.BuildExamplePage(example), dryRun);

            return;
        }

        var existing = File.ReadAllText(path);

        switch (DocFrontmatter.ReadOwnership(existing))
        {
            case DocOwnership.Generated:
                Write(path, builder.BuildExamplePage(example), dryRun);
                break;

            case DocOwnership.Partial:
                if (DocFrontmatter.TryReplaceRegion(existing, builder.BuildExampleBody(example), out var merged))
                {
                    Write(path, merged, dryRun);
                }
                else
                {
                    // Never guess where the region was meant to start.
                    logger.LogWarning(
                        "  ⚠ {Slug}.md is marked 'generated: partial' but has no {Start} / {End} markers. Skipped",
                        slug, DocFrontmatter.RegionStart, DocFrontmatter.RegionEnd);

                    _skipped++;
                }

                break;

            default:
                logger.LogDebug("  · {Slug}.md is hand-owned; left alone", slug);
                _skipped++;
                break;
        }
    }

    /// <summary>
    /// Writes one landing page per language and level group that actually has examples.
    /// </summary>
    /// <summary>
    /// Writes the visual gallery, which is the landing page for the whole examples section.
    /// </summary>
    /// <remarks>
    /// Reached through <c>topicHref</c> on the Examples node in the hand-maintained
    /// <c>manual/toc.yml</c>, so it does not appear as a child in the generated toc.
    /// </remarks>
    private void WriteGallery(DocPageBuilder builder, IReadOnlyList<ExampleMetadata> examples, DirectoryInfo docsDirectory, bool dryRun)
    {
        var groups = GroupByLanguageAndLevel(examples).ToList();
        var path = Path.Combine(docsDirectory.FullName, "index.md");

        WriteIfOwned(path, () => builder.BuildGallery(groups), dryRun);
    }

    private void WriteLandingPages(IReadOnlyList<ExampleMetadata> examples, DirectoryInfo docsDirectory, bool dryRun)
    {
        foreach (var group in GroupByLanguageAndLevel(examples))
        {
            var path = Path.Combine(docsDirectory.FullName, DocPaths.LandingPage(group.Language, group.Level));

            WriteIfOwned(path, () => DocPageBuilder.BuildLandingPage(group.Language, group.Level, group.Examples), dryRun);
        }
    }

    /// <summary>
    /// Keeps the pre-level URLs alive.
    /// </summary>
    /// <remarks>
    /// The stub file has to keep existing: <c>redirect_url</c> produces a redirecting HTML page at
    /// build time, so deleting the markdown would break the URL rather than forward it. GitHub Pages
    /// cannot serve a real 301, which makes this the strongest mechanism available rather than a
    /// compromise against a better one.
    /// </remarks>
    private void WriteRedirectStubs(DirectoryInfo docsDirectory, bool dryRun)
    {
        foreach (var (oldPage, newPage) in DocPaths.LegacyLandingPages)
        {
            var path = Path.Combine(docsDirectory.FullName, oldPage);

            WriteIfOwned(path, () => DocPageBuilder.BuildRedirectStub(newPage), dryRun);
        }
    }

    /// <summary>
    /// Writes the examples folder's own table of contents.
    /// </summary>
    /// <remarks>
    /// A dedicated toc referenced as a nested node from <c>manual/toc.yml</c>, so the generator never
    /// has to edit a hand-maintained file to add an example.
    /// </remarks>
    private void WriteTableOfContents(IReadOnlyList<ExampleMetadata> examples, DirectoryInfo docsDirectory, bool dryRun)
    {
        var toc = new StringBuilder();

        toc.AppendLine("# Generated by the examples metadata generator - do not edit by hand.");
        toc.AppendLine("# Run: dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- docs");
        toc.AppendLine();

        // No entry for index.md. The gallery is the landing page of the "Examples" node itself, wired
        // up with topicHref in the hand-maintained manual/toc.yml - listing it here as well would show
        // the same page twice in the sidebar.
        foreach (var group in GroupByLanguageAndLevel(examples))
        {
            // The count is appended rather than left to the reader to work out by expanding the node.
            // It is written for every group including the single-example ones: an inconsistent "(11)"
            // here and nothing there reads as a badge on the big groups rather than as a count.
            toc.AppendLine($"- name: {DocPaths.LanguageName(group.Language)} {group.Level} ({group.Examples.Count})");
            toc.AppendLine($"  href: {DocPaths.LandingPage(group.Language, group.Level)}");
            toc.AppendLine("  items:");

            foreach (var example in group.Examples)
            {
                var name = example.TocName ?? example.Title?.GetValueOrDefault("en") ?? example.Slug;

                toc.AppendLine($"  - name: {YamlScalar(name)}");
                toc.AppendLine($"    href: {example.Slug}.md");
            }
        }

        Write(Path.Combine(docsDirectory.FullName, "toc.yml"), toc.ToString(), dryRun);
    }

    /// <summary>
    /// Groups examples the way the toc presents them, preserving the manifest's ordering.
    /// </summary>
    private static IEnumerable<(string Language, string Level, IReadOnlyList<ExampleMetadata> Examples)> GroupByLanguageAndLevel(
        IReadOnlyList<ExampleMetadata> examples)
        => examples
            .GroupBy(example => (Language: example.EffectiveLanguage, Level: example.Level ?? MetadataVocabulary.Other))
            .Select(group => (group.Key.Language, group.Key.Level, (IReadOnlyList<ExampleMetadata>)[.. group]));

    /// <summary>
    /// Writes a file the generator owns, unless a person has taken it over.
    /// </summary>
    private void WriteIfOwned(string path, Func<string> build, bool dryRun)
    {
        if (File.Exists(path) && DocFrontmatter.ReadOwnership(File.ReadAllText(path)) == DocOwnership.HandOwned)
        {
            logger.LogWarning("  ⚠ {File} is hand-owned; left alone. Add 'generated: true' to let the tool manage it",
                Path.GetFileName(path));

            _skipped++;

            return;
        }

        Write(path, build(), dryRun);
    }

    /// <summary>
    /// Writes a file, skipping the write when the content has not changed.
    /// </summary>
    /// <remarks>
    /// Comparing first keeps the git diff limited to pages that actually changed. Without it every run
    /// would rewrite sixty files and the review would be worthless.
    /// </remarks>
    private void Write(string path, string content, bool dryRun)
    {
        var normalised = content.ReplaceLineEndings();

        if (File.Exists(path) && File.ReadAllText(path).ReplaceLineEndings() == normalised)
        {
            _unchanged++;

            return;
        }

        logger.LogInformation("  {Marker} {File}", dryRun ? "would write" : "✅", Path.GetFileName(path));

        if (!dryRun)
        {
            File.WriteAllText(path, normalised, new UTF8Encoding(false));
        }

        _written++;
    }

    /// <summary>
    /// Quotes a toc entry name when YAML would otherwise misread it.
    /// </summary>
    private static string YamlScalar(string? value)
    {
        if (value is null)
        {
            return "\"\"";
        }

        var needsQuoting = value.Contains(':') || value.Contains('#') || value.StartsWith('[') || value.StartsWith('{');

        return needsQuoting ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
    }
}