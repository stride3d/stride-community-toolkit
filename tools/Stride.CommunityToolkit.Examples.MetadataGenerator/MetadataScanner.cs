using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator;

/// <summary>
/// Scans Program.cs files in examples/code-only and extracts YAML metadata into JSON manifest.
/// </summary>
public partial class MetadataScanner(string examplesRoot, string outputPath)
{
    private readonly string _examplesRoot = examplesRoot;
    private readonly string _outputPath = outputPath;

    [GeneratedRegex(@"/\*\s*---example-metadata\s*(.*?)\s*---\s*\*/", RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex YamlBlockRegex();

    public async Task<int> ScanAndGenerateAsync()
    {
        if (!Directory.Exists(_examplesRoot))
        {
            Console.Error.WriteLine($"Examples directory not found: {_examplesRoot}");

            return 1;
        }

        var examples = await ScanExamplesAsync();

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

    public async Task<List<ExampleMetadata>> ScanExamplesAsync()
    {
        var examples = new List<ExampleMetadata>();
        var programFiles = Directory.GetFiles(_examplesRoot, "Program.cs", SearchOption.AllDirectories);

        Console.WriteLine($"Scanning {programFiles.Length} Program.cs files...");

        foreach (var programFile in programFiles)
        {
            Console.WriteLine($"Processing {Path.GetFileName(Path.GetDirectoryName(programFile))}... ");

            try
            {
                var metadata = await ExtractMetadata(programFile);

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

        return examples;
    }

    private async Task<ExampleMetadata?> ExtractMetadata(string programFile)
    {
        var content = await File.ReadAllTextAsync(programFile);
        var match = YamlBlockRegex().Match(content);

        if (!match.Success) return null;

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
        catch (YamlException ex)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"Failed to parse YAML metadata in {Path.GetFileName(programFile)}");
            errorMessage.AppendLine($"Error at Line {ex.End.Line}, Column {ex.End.Column}");
            errorMessage.AppendLine();
            errorMessage.AppendLine("YAML Content:");
            errorMessage.AppendLine("---");
            errorMessage.AppendLine(yamlContent);
            errorMessage.AppendLine("---");
            errorMessage.AppendLine();
            errorMessage.AppendLine($"Error: {ex.Message}");

            throw new InvalidOperationException(errorMessage.ToString(), ex);
        }
        catch (Exception ex)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"Unexpected error while processing {Path.GetFileName(programFile)}");
            errorMessage.AppendLine();
            errorMessage.AppendLine("YAML Content:");
            errorMessage.AppendLine("---");
            errorMessage.AppendLine(yamlContent);
            errorMessage.AppendLine("---");
            errorMessage.AppendLine();
            errorMessage.AppendLine($"Error: {ex.Message}");

            throw new InvalidOperationException(errorMessage.ToString(), ex);
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