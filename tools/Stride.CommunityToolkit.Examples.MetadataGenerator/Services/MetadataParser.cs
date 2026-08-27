using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Parses the <c>---example-metadata</c> block out of an example source file.
/// </summary>
/// <remarks>
/// <c>IgnoreUnmatchedProperties</c> is deliberately still enabled. On its own it is what let
/// <c>Order:</c> disappear from two examples without a word, but the fix is not to throw on the first
/// stray key - it is to report every one of them, which
/// <see cref="MetadataValidator"/> does using the literal key list captured here. Failing inside the
/// deserializer would give one file, one message, and no aggregation.
/// </remarks>
public class MetadataParser(ILogger<MetadataParser> logger)
{
    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly IDeserializer _rawDeserializer = new DeserializerBuilder()
        .Build();

    /// <summary>
    /// Extracts and parses the metadata block from a source file.
    /// </summary>
    /// <param name="exampleFilePath">The full path to the source file.</param>
    /// <param name="examplesRootPath">The root examples directory, used for the relative path.</param>
    /// <param name="cancellationToken">Cancels the file read.</param>
    /// <returns>The parsed example, or <see langword="null"/> if the file has no metadata block.</returns>
    public async Task<ParsedExample?> ParseMetadataAsync(
        string exampleFilePath,
        string examplesRootPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exampleFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(examplesRootPath);

        var projectName = Path.GetFileName(Path.GetDirectoryName(exampleFilePath));

        var content = await File.ReadAllTextAsync(exampleFilePath, cancellationToken);

        if (!YamlMetadataExtractor.TryExtract(exampleFilePath, content, out var yamlContent, out var blockLocation))
        {
            return null;
        }

        logger.LogDebug("Parsing metadata from: {ProjectName}", projectName);

        try
        {
            var metadata = _yamlDeserializer.Deserialize<ExampleMetadata>(yamlContent) ?? new ExampleMetadata();

            metadata.ProjectName = projectName;
            metadata.ProjectPath = ToRelativePosixPath(exampleFilePath, examplesRootPath);
            metadata.Language ??= YamlMetadataExtractor.GetLanguage(exampleFilePath);
            metadata.BlockLocation = blockLocation;

            NormaliseTrailingNewlines(metadata.Title);
            NormaliseTrailingNewlines(metadata.Description);

            var declaredKeys = ReadDeclaredKeys(yamlContent);

            logger.LogDebug("Parsed metadata for: {ProjectName}", metadata.ProjectName);

            return new ParsedExample(metadata, exampleFilePath, yamlContent, declaredKeys);
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException(BuildYamlErrorMessage(exampleFilePath, yamlContent, ex), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(BuildGenericErrorMessage(exampleFilePath, yamlContent, ex), ex);
        }
    }

    /// <summary>
    /// Reads the top-level keys exactly as written, so mis-cased and unknown ones can be reported.
    /// </summary>
    private IReadOnlyList<string> ReadDeclaredKeys(string yamlContent)
    {
        try
        {
            var raw = _rawDeserializer.Deserialize<Dictionary<string, object?>>(yamlContent);

            return raw is null ? [] : [.. raw.Keys];
        }
        catch (YamlException)
        {
            // The typed deserialization above is the authority; if the document is not a plain mapping
            // there are no top-level keys worth reporting.
            return [];
        }
    }

    /// <summary>
    /// Strips the trailing newline that a <c>|</c> block scalar keeps, so consumers do not each have to.
    /// </summary>
    private static void NormaliseTrailingNewlines(Dictionary<string, string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (var key in values.Keys.ToList())
        {
            values[key] = values[key].TrimEnd('\r', '\n');
        }
    }

    /// <summary>
    /// Produces a path relative to the examples root using forward slashes, so the manifest is byte
    /// identical whichever platform generated it.
    /// </summary>
    private static string ToRelativePosixPath(string filePath, string examplesRootPath)
        => Path.GetRelativePath(examplesRootPath, filePath).Replace('\\', '/');

    private static string BuildYamlErrorMessage(string exampleFilePath, string yamlContent, YamlException ex)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine($"Failed to parse YAML metadata in {Path.GetFileName(exampleFilePath)}, at line {ex.End.Line}, column {ex.End.Column}.");

        AppendDiagnosis(errorMessage, exampleFilePath, yamlContent);

        errorMessage.Append($"YamlDotNet reported: {ex.Message}");

        return errorMessage.ToString();
    }

    private static string BuildGenericErrorMessage(string exampleFilePath, string yamlContent, Exception ex)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine($"Unexpected error while processing {Path.GetFileName(exampleFilePath)}.");

        AppendDiagnosis(errorMessage, exampleFilePath, yamlContent);

        errorMessage.Append($"Error: {ex.Message}");

        return errorMessage.ToString();
    }

    /// <summary>
    /// Adds the source-level diagnosis to a parse-failure message.
    /// </summary>
    /// <remarks>
    /// A YamlDotNet failure is usually a symptom rather than the cause - the classic one is
    /// "Uninitialized Strings cannot be created", which really means an unquoted <c>": "</c> turned a
    /// sequence item into a mapping. <see cref="YamlSourceInspector"/> can see that in the source text,
    /// so its findings go first and the deserializer's own message goes last.
    /// </remarks>
    private static void AppendDiagnosis(StringBuilder errorMessage, string exampleFilePath, string yamlContent)
    {
        var projectName = Path.GetFileName(Path.GetDirectoryName(exampleFilePath)) ?? exampleFilePath;
        var findings = YamlSourceInspector.Inspect(projectName, yamlContent);

        if (findings.Count > 0)
        {
            errorMessage.AppendLine();
            errorMessage.AppendLine("Likely cause:");

            foreach (var finding in findings)
            {
                errorMessage.AppendLine($"  - {finding.Message}");
            }
        }

        errorMessage.AppendLine();
    }
}
