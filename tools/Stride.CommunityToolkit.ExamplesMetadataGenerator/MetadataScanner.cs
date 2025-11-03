using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stride.CommunityToolkit.ExamplesMetadataGenerator;

/// <summary>
/// Scans Program.cs files in examples/code-only and extracts YAML metadata into JSON manifest.
/// </summary>
public partial class MetadataScanner
{
    private readonly string _examplesRoot;
    private readonly string _outputPath;

    [GeneratedRegex(@"/\*\s*---example-metadata\s*(.*?)\s*---\s*\*/", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex YamlBlockRegex();

    public MetadataScanner(string examplesRoot, string outputPath)
    {
        _examplesRoot = examplesRoot;
        _outputPath = outputPath;
    }

    public async Task<int> ScanAndGenerateAsync()
    {
        if (!Directory.Exists(_examplesRoot))
        {
            Console.Error.WriteLine($"Examples directory not found: {_examplesRoot}");
            return 1;
        }

        var examples = new List<ExampleMetadata>();
        var programFiles = Directory.GetFiles(_examplesRoot, "Program.cs", SearchOption.AllDirectories);

        Console.WriteLine($"Scanning {programFiles.Length} Program.cs files...");

        foreach (var programFile in programFiles)
        {
            Console.WriteLine($"Processing {Path.GetFileName(Path.GetDirectoryName(programFile))}... ");

            try
            {
                var metadata = ExtractMetadata(programFile);
                if (metadata != null)
                {
                    examples.Add(metadata);
                    Console.WriteLine($"  ✓ {metadata.ProjectName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {Path.GetFileName(Path.GetDirectoryName(programFile))}: {ex.Message}");
            }
        }

        Console.WriteLine($"\nFound {examples.Count} examples with metadata.");

        if (examples.Count > 0)
        {
            await WriteManifestAsync(examples);
            Console.WriteLine($"Manifest written to: {_outputPath}");
        }
        else
        {
            Console.WriteLine("No manifest written - no examples with metadata found.");
        }

        return 0;
    }

    private ExampleMetadata? ExtractMetadata(string programFile)
    {
        var content = File.ReadAllText(programFile);
        var match = YamlBlockRegex().Match(content);

        if (!match.Success)
            return null;

        var yamlContent = match.Groups[1].Value.Trim();

        try
        {
            var deserializer = new DeserializerBuilder()
          .WithNamingConvention(CamelCaseNamingConvention.Instance)
           .IgnoreUnmatchedProperties()
            .Build();

            var metadata = deserializer.Deserialize<ExampleMetadata>(yamlContent);

            if (metadata != null)
            {
                var projectDir = Path.GetDirectoryName(programFile);
                metadata.ProjectName = Path.GetFileName(projectDir);
                metadata.ProjectPath = Path.GetRelativePath(_examplesRoot, programFile);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse YAML metadata: {ex.Message}", ex);
        }
    }

    private async Task WriteManifestAsync(List<ExampleMetadata> examples)
    {
        var outputDir = Path.GetDirectoryName(_outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(examples, options);
        await File.WriteAllTextAsync(_outputPath, json, Encoding.UTF8);
    }
}
