using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Text.RegularExpressions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Checks parsed example metadata against schema v1 and reports every problem in one pass.
/// </summary>
/// <remarks>
/// Aggregation is the point: an author fixing frontmatter wants the whole list, not the first failure.
/// Nothing here throws - the caller decides whether errors are fatal (see <c>--strict</c>).
/// </remarks>
public partial class MetadataValidator(ILogger<MetadataValidator> logger)
{
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    private static readonly IReadOnlySet<string> EmptyProjects = new HashSet<string>(StringComparer.Ordinal);


    /// <summary>
    /// Validates a set of parsed examples.
    /// </summary>
    /// <param name="examples">The parsed examples, already filtered to those that will reach the manifest.</param>
    /// <param name="mediaDirectory">
    /// The docs media folder used to confirm <c>media:</c> files exist, or <see langword="null"/> to skip
    /// that check.
    /// </param>
    /// <param name="knownProjects">
    /// Every example project folder name, used to resolve <c>related:</c>. Pass <see langword="null"/>
    /// to resolve against the parsed examples alone.
    /// </param>
    /// <param name="disabledProjects">
    /// Project folder names excluded by <c>enabled: false</c>. A <c>related:</c> link pointing at one of
    /// these is dropped deliberately rather than through an authoring mistake, so it is reported as
    /// information instead of a warning.
    /// </param>
    /// <returns>Every finding, in no particular order.</returns>
    public IReadOnlyList<ValidationMessage> Validate(
        IReadOnlyList<ParsedExample> examples,
        DirectoryInfo? mediaDirectory,
        IReadOnlySet<string>? knownProjects = null,
        IReadOnlySet<string>? disabledProjects = null)
    {
        ArgumentNullException.ThrowIfNull(examples);

        var messages = new List<ValidationMessage>();

        knownProjects ??= examples
            .Select(example => example.Metadata.ProjectName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var example in examples)
        {
            ValidateKeys(example, messages);
            ValidateRequiredFields(example, messages);
            ValidateVocabulary(example, messages);
            ValidateMedia(example, mediaDirectory, messages);
            messages.AddRange(YamlSourceInspector.Inspect(ProjectNameOf(example), example.RawYaml));
            ValidateTags(example, messages);
        }

        ValidateSlugUniqueness(examples, messages);
        ValidateMediaUniqueness(examples, messages);
        ValidateOrderUniqueness(examples, messages);
        ResolveRelated(examples, knownProjects, disabledProjects ?? EmptyProjects, messages);

        var errors = messages.Count(message => message.Severity == ValidationSeverity.Error);
        var warnings = messages.Count(message => message.Severity == ValidationSeverity.Warning);

        // Deliberately phrased without a colon in front of the word "error". Visual Studio runs its own
        // error-format parser over task output, and "<origin>: 0 error(s)..." was matching it: every
        // build grew a red row in the Error List reading "0 error(s), 2 warning(s)", on a build that had
        // neither. MSBuild itself never classified the line - the CLI reported 0/0 throughout - so this
        // was invisible to anyone not building in the IDE.
        logger.LogInformation("Validation finished - {Count} example(s) checked, {Errors} failed, {Warnings} flagged",
            examples.Count, errors, warnings);

        return messages;
    }

    /// <summary>
    /// Rejects keys the schema does not define. This is the check that would have caught <c>Order:</c>
    /// being silently dropped by the camelCase naming convention.
    /// </summary>
    private static void ValidateKeys(ParsedExample example, List<ValidationMessage> messages)
    {
        var projectName = ProjectNameOf(example);

        foreach (var key in example.DeclaredKeys)
        {
            if (MetadataVocabulary.KnownKeys.Contains(key))
            {
                continue;
            }

            var suggestion = MetadataVocabulary.SuggestKey(key);
            var hint = suggestion is null
                ? $"'{key}' is not part of the schema."
                : $"'{key}' is not a schema key - did you mean '{suggestion}'? Keys are case-sensitive, and an unrecognised one is discarded silently.";

            messages.Add(ValidationMessage.Error(projectName, key, hint));
        }
    }

    private static void ValidateRequiredFields(ParsedExample example, List<ValidationMessage> messages)
    {
        var metadata = example.Metadata;
        var projectName = ProjectNameOf(example);

        if (string.IsNullOrWhiteSpace(metadata.Slug))
        {
            messages.Add(ValidationMessage.Error(projectName, "slug",
                "Required. Short, shareable, kebab-case; it becomes the doc filename and the public URL."));
        }
        else if (!SlugPattern().IsMatch(metadata.Slug))
        {
            messages.Add(ValidationMessage.Error(projectName, "slug",
                $"'{metadata.Slug}' is not kebab-case (lowercase letters, digits and single hyphens)."));
        }

        if (metadata.Title is null || !metadata.Title.TryGetValue("en", out var englishTitle) || string.IsNullOrWhiteSpace(englishTitle))
        {
            messages.Add(ValidationMessage.Error(projectName, "title", "Required, and must include an 'en' entry."));
        }

        if (string.IsNullOrWhiteSpace(metadata.Level))
        {
            messages.Add(ValidationMessage.Error(projectName, "level", "Required."));
        }

        if (string.IsNullOrWhiteSpace(metadata.Category))
        {
            messages.Add(ValidationMessage.Error(projectName, "category", "Required."));
        }
    }

    private static void ValidateVocabulary(ParsedExample example, List<ValidationMessage> messages)
    {
        var metadata = example.Metadata;
        var projectName = ProjectNameOf(example);

        if (metadata.Level is { Length: > 0 } level && !MetadataVocabulary.Levels.Contains(level, StringComparer.Ordinal))
        {
            messages.Add(ValidationMessage.Error(projectName, "level",
                $"'{level}' is not a known level. Expected one of: {string.Join(", ", MetadataVocabulary.Levels)}. Matching is exact."));
        }

        if (metadata.Category is { Length: > 0 } category && !MetadataVocabulary.Categories.Contains(category, StringComparer.Ordinal))
        {
            messages.Add(ValidationMessage.Error(projectName, "category",
                $"'{category}' is not a known category. Expected one of: {string.Join(", ", MetadataVocabulary.Categories)}."));
        }

        if (metadata.Language is { Length: > 0 } language)
        {
            if (!MetadataVocabulary.Languages.Contains(language, StringComparer.Ordinal))
            {
                messages.Add(ValidationMessage.Error(projectName, "language",
                    $"'{language}' is not a known language. Expected one of: {string.Join(", ", MetadataVocabulary.Languages)}."));
            }
            else if (YamlMetadataExtractor.GetLanguage(example.SourcePath) is { } fileLanguage
                && !string.Equals(language, fileLanguage, StringComparison.Ordinal))
            {
                messages.Add(ValidationMessage.Error(projectName, "language",
                    $"declared as '{language}' but the block lives in a {Path.GetExtension(example.SourcePath)} file ('{fileLanguage}'). The toc groups on this field, so a mismatch files the example under the wrong language."));
            }
        }

        if (metadata.Complexity is { } complexity
            && (complexity < MetadataVocabulary.MinComplexity || complexity > MetadataVocabulary.MaxComplexity))
        {
            messages.Add(ValidationMessage.Error(projectName, "complexity",
                $"{complexity} is outside the documented range {MetadataVocabulary.MinComplexity}-{MetadataVocabulary.MaxComplexity}."));
        }

        if (metadata.Created is { Length: > 0 } created && !DateOnly.TryParse(created, out _))
        {
            messages.Add(ValidationMessage.Warning(projectName, "created", $"'{created}' is not a yyyy-MM-dd date."));
        }

        // The documentation includes the source with a line range that stops before the block, which
        // only works if the block is the last thing in the file. Otherwise the whole file is embedded
        // and the reader sees the metadata restated as YAML under the prose it was rendered from.
        if (!metadata.BlockLocation.IsLastInFile)
        {
            messages.Add(ValidationMessage.Warning(projectName, "(metadata block)",
                "The block is not the last thing in the file, so the documentation cannot trim it out of the code listing and will embed it. Move it to the end."));
        }
    }

    private static void ValidateMedia(ParsedExample example, DirectoryInfo? mediaDirectory, List<ValidationMessage> messages)
    {
        if (mediaDirectory is null || !mediaDirectory.Exists)
        {
            return;
        }

        var metadata = example.Metadata;

        // Only an explicit media: entry is an error when missing. A defaulted <slug>.webp that does not
        // exist yet is expected - most examples have no screenshot at all (see plan §5).
        if (metadata.Media is not { Length: > 0 } media)
        {
            return;
        }

        if (!File.Exists(Path.Combine(mediaDirectory.FullName, media)))
        {
            messages.Add(ValidationMessage.Error(ProjectNameOf(example), "media",
                $"'{media}' does not exist in {mediaDirectory.FullName}."));
        }
    }


    /// <summary>
    /// Warns when a level name is repeated as a tag - the same fact in a place nothing validates.
    /// </summary>
    private static void ValidateTags(ParsedExample example, List<ValidationMessage> messages)
    {
        if (example.Metadata.Tags is not { Count: > 0 } tags)
        {
            return;
        }

        foreach (var tag in tags)
        {
            var isLevelName = MetadataVocabulary.Levels.Contains(tag, StringComparer.OrdinalIgnoreCase)
                || string.Equals(tag, "Beginners", StringComparison.OrdinalIgnoreCase);

            if (isLevelName)
            {
                messages.Add(ValidationMessage.Warning(ProjectNameOf(example), "tags",
                    $"'{tag}' duplicates the 'level' field. Remove it; the generator can derive a level index itself."));
            }
        }
    }

    private static void ValidateSlugUniqueness(IReadOnlyList<ParsedExample> examples, List<ValidationMessage> messages)
    {
        var duplicates = examples
            .Where(example => !string.IsNullOrWhiteSpace(example.Metadata.Slug))
            .GroupBy(example => example.Metadata.Slug!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicates)
        {
            var owners = string.Join(", ", group.Select(ProjectNameOf));

            messages.Add(ValidationMessage.Error(owners, "slug",
                $"'{group.Key}' is used by more than one example. A slug is a doc filename, so it must be unique."));
        }
    }

    /// <summary>
    /// Rejects two examples that declare the same <c>media:</c> filename.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An error, not a warning, because it silently destroys work. The screenshot run captures each
    /// example in manifest order and writes to the declared filename, so a shared name means the second
    /// capture overwrites the first and one example's page ends up showing another example's scene. It
    /// was found by counting: 49 captures produced 48 files, because
    /// <c>Example05_PartialTorus</c> and <c>Example05_PartialTorus_FSharp</c> both claimed
    /// <c>stride-game-engine-example-05-partial-torus-mesh.webp</c>.
    /// </para>
    /// <para>
    /// Only explicit values are compared. A defaulted <c>&lt;slug&gt;.webp</c> cannot collide, because
    /// <see cref="ValidateSlugUniqueness"/> has already established that slugs are unique. The comparison
    /// ignores case: these are filenames on a case-insensitive filesystem, so two spellings that differ
    /// only in case are the same file and would collide just as destructively.
    /// </para>
    /// </remarks>
    private static void ValidateMediaUniqueness(IReadOnlyList<ParsedExample> examples, List<ValidationMessage> messages)
    {
        var duplicates = examples
            .Where(example => !string.IsNullOrWhiteSpace(example.Metadata.Media))
            .GroupBy(example => example.Metadata.Media!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicates)
        {
            var owners = string.Join(", ", group.Select(ProjectNameOf));

            messages.Add(ValidationMessage.Error(owners, "media",
                $"'{group.Key}' is declared by more than one example. The screenshot run writes to that " +
                "filename, so one capture would overwrite the other. Give each example its own image, or " +
                "omit 'media' to default to '<slug>.webp'."));
        }
    }

    /// <summary>
    /// Notes examples that share an <c>order</c> within the same group.
    /// </summary>
    /// <remarks>
    /// A warning rather than an error: a tie is broken by <c>slug</c>, which is required and unique, so
    /// the resulting sequence is still stable and reproducible - the author simply has not said which of
    /// the two comes first. That is a reasonable thing to leave unsaid, and not worth failing a build
    /// over.
    /// </remarks>
    private static void ValidateOrderUniqueness(IReadOnlyList<ParsedExample> examples, List<ValidationMessage> messages)
    {
        var collisions = examples
            .Where(example => example.Metadata.Order is not null)
            .GroupBy(example => (example.Metadata.EffectiveLanguage, example.Metadata.Level, example.Metadata.Order))
            .Where(group => group.Count() > 1);

        foreach (var group in collisions)
        {
            var owners = string.Join(", ", group.Select(ProjectNameOf));

            messages.Add(ValidationMessage.Warning(owners, "order",
                $"order {group.Key.Order} is shared within ({group.Key.EffectiveLanguage}, {group.Key.Level}); the tie is broken by slug, so the sequence is stable but not author-chosen."));
        }
    }

    /// <summary>
    /// Resolves <c>related:</c> project names to slugs, erroring on a name that matches no example.
    /// </summary>
    private static void ResolveRelated(
        IReadOnlyList<ParsedExample> examples,
        IReadOnlySet<string> knownProjects,
        IReadOnlySet<string> disabledProjects,
        List<ValidationMessage> messages)
    {
        var slugByProject = examples
            .Where(example => !string.IsNullOrWhiteSpace(example.Metadata.ProjectName)
                && !string.IsNullOrWhiteSpace(example.Metadata.Slug))
            .ToDictionary(example => example.Metadata.ProjectName!, example => example.Metadata.Slug!, StringComparer.Ordinal);

        foreach (var example in examples)
        {
            if (example.Metadata.Related is not { Count: > 0 } related)
            {
                continue;
            }

            var resolved = new List<string>(related.Count);

            foreach (var name in related)
            {
                if (!knownProjects.Contains(name))
                {
                    messages.Add(ValidationMessage.Error(ProjectNameOf(example), "related",
                        $"'{name}' does not match any example project folder - check the spelling."));

                    continue;
                }

                if (slugByProject.TryGetValue(name, out var slug))
                {
                    resolved.Add(slug);

                    continue;
                }

                // Two different situations reach here, and conflating them produced a warning nobody
                // could act on. A link to a disabled example is working as designed: the target is
                // deliberately absent from the manifest, and the link is meant to come back with it.
                if (disabledProjects.Contains(name))
                {
                    messages.Add(ValidationMessage.Info(ProjectNameOf(example), "related",
                        $"'{name}' is enabled: false, so the link is dropped until it is published again."));

                    continue;
                }

                messages.Add(ValidationMessage.Warning(ProjectNameOf(example), "related",
                    $"'{name}' has no metadata block yet, so the link is dropped from the manifest."));
            }

            example.Metadata.RelatedSlugs = resolved;
        }
    }

    private static string ProjectNameOf(ParsedExample example)
        => example.Metadata.ProjectName ?? Path.GetFileName(example.SourcePath);
}
