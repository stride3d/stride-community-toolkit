using Microsoft.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Core;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

/// <summary>
/// Writes the example manifest to disk.
/// </summary>
public class ManifestWriter(ILogger<ManifestWriter> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Without this, every non-ASCII character in the Czech titles is written as a \uXXXX escape,
        // which makes the manifest unreadable in review for no gain.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Serializes the examples into the versioned envelope and writes it to
    /// <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="examples">The examples to include, already validated and sorted.</param>
    /// <param name="outputPath">The full path of the manifest file to write.</param>
    /// <param name="generatedAtUtc">The timestamp to stamp into the envelope.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    public async Task WriteManifestAsync(
        IReadOnlyList<ExampleMetadata> examples,
        string outputPath,
        DateTimeOffset generatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(examples);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        logger.LogInformation("Writing manifest to: {OutputPath}", outputPath);

        EnsureOutputDirectoryExists(outputPath);

        var manifest = new ExampleManifest
        {
            GeneratedAt = generatedAtUtc.UtcDateTime.ToString("O"),
            ToolVersion = GetToolVersion(),
            Examples = examples
        };

        var json = JsonSerializer.Serialize(manifest, JsonOptions);

        await File.WriteAllTextAsync(outputPath, json, new UTF8Encoding(false), cancellationToken);

        logger.LogInformation("Wrote manifest schema v{SchemaVersion} with {Count} example(s)",
            manifest.SchemaVersion, manifest.Count);
    }

    private static string GetToolVersion()
        => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

    private void EnsureOutputDirectoryExists(string outputPath)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (string.IsNullOrEmpty(outputDirectory) || Directory.Exists(outputDirectory))
        {
            return;
        }

        logger.LogInformation("Creating output directory: {Directory}", outputDirectory);

        Directory.CreateDirectory(outputDirectory);
    }
}