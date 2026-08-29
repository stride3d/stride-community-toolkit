using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Orchestrates scanning, parsing, validation and manifest generation.
/// </summary>
public class ManifestService(
    ILogger<ManifestService> logger,
    ExampleScanner exampleScanner,
    MetadataParser metadataParser,
    MetadataValidator metadataValidator,
    ManifestWriter manifestWriter,
    DocsGenerator docsGenerator)
{
    /// <summary>Everything worked.</summary>
    public const int ExitSuccess = 0;

    /// <summary>The run could not proceed - a missing directory, an unreadable file, a failed write.</summary>
    public const int ExitFailure = 1;

    /// <summary>The scan completed but found no examples at all, which is almost always a wrong path.</summary>
    public const int ExitNoExamplesFound = 2;

    /// <summary>Validation reported errors and <c>--strict</c> was in force.</summary>
    public const int ExitValidationFailed = 3;

    /// <summary>
    /// Scans the examples directory and returns everything that carries a metadata block.
    /// </summary>
    /// <param name="examplesRootPath">The root directory containing example projects.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    /// <returns>The parsed examples, including any marked <c>enabled: false</c>, and the failure count.</returns>
    public async Task<ScanResult> ScanExamplesAsync(
        DirectoryInfo? examplesRootPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);

        logger.LogInformation("Starting example scan in: {Path}", examplesRootPath.FullName);

        if (!examplesRootPath.Exists)
        {
            logger.LogError("Examples directory does not exist: {Path}", examplesRootPath.FullName);

            return new ScanResult([], 0);
        }

        var examples = new List<ParsedExample>();
        var failures = 0;

        foreach (var exampleFile in exampleScanner.FindExampleFiles(examplesRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectName = exampleScanner.GetProjectName(exampleFile);

            try
            {
                var parsed = await metadataParser.ParseMetadataAsync(exampleFile, examplesRootPath.FullName, cancellationToken);

                if (parsed is null)
                {
                    continue;
                }

                examples.Add(parsed);

                logger.LogInformation("  ✅ {ProjectName} - {Title}", parsed.Metadata.ProjectName, EnglishTitleOf(parsed));
            }
            catch (Exception ex)
            {
                // The message already carries the diagnosis and the file; the stack trace is noise.
                logger.LogError("  ✖ {ProjectName} - {Message}", projectName, ex.Message);

                failures++;
            }
        }

        if (failures > 0)
        {
            logger.LogError("{Count} metadata block(s) could not be parsed. Those examples are missing from the manifest", failures);
        }

        logger.LogInformation("Scan completed. Found {Count} example(s) with metadata", examples.Count);

        return new ScanResult(examples, failures);
    }

    /// <summary>
    /// Scans, validates, and writes the JSON manifest.
    /// </summary>
    /// <param name="examplesRootPath">The root directory containing example projects.</param>
    /// <param name="outputPath">Where the manifest should be written.</param>
    /// <param name="mediaDirectory">The docs media folder, or <see langword="null"/> to skip media checks.</param>
    /// <param name="strict">When <see langword="true"/>, validation errors fail the run.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>One of the <c>Exit*</c> codes on this class.</returns>
    public async Task<int> ScanAndGenerateManifestAsync(
        DirectoryInfo? examplesRootPath,
        string outputPath,
        DirectoryInfo? mediaDirectory,
        bool strict,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        logger.LogInformation("Starting manifest generation for: {Path}", examplesRootPath.FullName);

        if (!examplesRootPath.Exists)
        {
            logger.LogError("Examples directory does not exist: {Path}", examplesRootPath.FullName);

            return ExitFailure;
        }

        var scan = await ScanExamplesAsync(examplesRootPath, cancellationToken);

        if (scan.Examples.Count == 0)
        {
            logger.LogError("No examples with metadata found under {Path}. No manifest written", examplesRootPath.FullName);

            return ExitNoExamplesFound;
        }

        // enabled: false means excluded from the manifest entirely, so a disabled example is invisible
        // to every consumer and takes no part in the uniqueness checks.
        var published = scan.Examples.Where(example => example.Metadata.Enabled != false).ToList();
        var disabled = scan.Examples
            .Where(example => example.Metadata.Enabled == false)
            .Select(example => example.Metadata.ProjectName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        if (disabled.Count > 0)
        {
            logger.LogInformation("Excluded {Count} example(s) marked enabled: false", disabled.Count);
        }

        var messages = metadataValidator.Validate(published, mediaDirectory, exampleScanner.FindProjectNames(examplesRootPath), disabled);
        var errorCount = ReportValidation(messages) + scan.Failures;

        if (errorCount > 0 && strict)
        {
            logger.LogError("Validation failed with {Count} error(s) and --strict is in force. No manifest written", errorCount);

            return ExitValidationFailed;
        }

        var ordered = Sort(published);

        await manifestWriter.WriteManifestAsync(ordered, outputPath, DateTimeOffset.UtcNow, cancellationToken);

        logger.LogInformation("Manifest generation completed");

        return ExitSuccess;
    }

    /// <summary>
    /// Scans, validates, and writes the example documentation.
    /// </summary>
    /// <param name="examplesRootPath">The root directory containing example projects.</param>
    /// <param name="docsDirectory">The <c>docs/manual/code-only/examples</c> folder.</param>
    /// <param name="mediaDirectory">The screenshot folder, or <see langword="null"/> to skip image links.</param>
    /// <param name="dryRun">When <see langword="true"/>, report what would change and write nothing.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>One of the <c>Exit*</c> codes on this class.</returns>
    /// <remarks>
    /// Documentation is always generated from validated metadata: a page built from a block with a
    /// missing slug or an unknown level would be wrong in ways that are tedious to spot by reading it,
    /// so validation errors stop the run whether or not strict mode was asked for.
    /// </remarks>
    public async Task<int> GenerateDocsAsync(
        DirectoryInfo? examplesRootPath,
        DirectoryInfo? docsDirectory,
        DirectoryInfo? mediaDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(examplesRootPath);
        ArgumentNullException.ThrowIfNull(docsDirectory);

        if (!docsDirectory.Exists)
        {
            logger.LogError("Docs directory does not exist: {Path}", docsDirectory.FullName);

            return ExitFailure;
        }

        var scan = await ScanExamplesAsync(examplesRootPath, cancellationToken);

        if (scan.Examples.Count == 0)
        {
            logger.LogError("No examples with metadata found under {Path}. Nothing to document", examplesRootPath.FullName);

            return ExitNoExamplesFound;
        }

        var published = scan.Examples.Where(example => example.Metadata.Enabled != false).ToList();
        var disabled = scan.Examples
            .Where(example => example.Metadata.Enabled == false)
            .Select(example => example.Metadata.ProjectName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        var messages = metadataValidator.Validate(published, mediaDirectory, exampleScanner.FindProjectNames(examplesRootPath), disabled);
        var errorCount = ReportValidation(messages) + scan.Failures;

        if (errorCount > 0)
        {
            logger.LogError("Validation failed with {Count} error(s). No documentation written", errorCount);

            return ExitValidationFailed;
        }

        docsGenerator.Generate(Sort(published), docsDirectory, mediaDirectory, dryRun);

        return ExitSuccess;
    }

    /// <summary>
    /// Logs every finding and returns how many were errors.
    /// </summary>
    /// <param name="messages">The findings to report.</param>
    /// <returns>The number of error-severity findings.</returns>
    public int ReportValidation(IReadOnlyList<ValidationMessage> messages)
    {
        var errorCount = 0;

        foreach (var message in messages.OrderBy(m => m.ProjectName, StringComparer.Ordinal).ThenBy(m => m.Field, StringComparer.Ordinal))
        {
            if (message.Severity == ValidationSeverity.Error)
            {
                errorCount++;

                logger.LogError("  ✖ {ProjectName} [{Field}] {Message}", message.ProjectName, message.Field, message.Message);
            }
            else if (message.Severity == ValidationSeverity.Warning)
            {
                logger.LogWarning("  ⚠ {ProjectName} [{Field}] {Message}", message.ProjectName, message.Field, message.Message);
            }
            else
            {
                logger.LogInformation("  ℹ {ProjectName} [{Field}] {Message}", message.ProjectName, message.Field, message.Message);
            }
        }

        return errorCount;
    }

    /// <summary>
    /// Sorts examples the way the toc presents them: by language, then level, then <c>order</c>.
    /// </summary>
    private static List<ExampleMetadata> Sort(IEnumerable<ParsedExample> examples)
        => [.. examples
            .Select(example => example.Metadata)
            .OrderBy(metadata => IndexIn(MetadataVocabulary.Languages, metadata.EffectiveLanguage))
            .ThenBy(metadata => IndexIn(MetadataVocabulary.Levels, metadata.Level))
            .ThenBy(metadata => metadata.Order ?? int.MaxValue)
            .ThenBy(metadata => metadata.Slug ?? metadata.ProjectName, StringComparer.Ordinal)];

    /// <summary>
    /// Gets the position of a value in a vocabulary, sorting anything unrecognised last.
    /// </summary>
    private static int IndexIn(string[] vocabulary, string? value)
    {
        if (value is null)
        {
            return vocabulary.Length;
        }

        var index = Array.IndexOf(vocabulary, value);

        return index < 0 ? vocabulary.Length : index;
    }

    private static string EnglishTitleOf(ParsedExample example)
        => example.Metadata.Title?.GetValueOrDefault("en") ?? "(no title)";
}