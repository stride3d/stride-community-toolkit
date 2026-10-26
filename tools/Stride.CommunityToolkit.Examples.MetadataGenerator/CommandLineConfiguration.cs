using Microsoft.Extensions.DependencyInjection;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Services;
using System.CommandLine;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator;

/// <summary>
/// Builds and configures the command-line interface structure.
/// </summary>
/// <param name="serviceProvider">Service provider for resolving command handlers.</param>
public class CommandLineConfiguration(IServiceProvider serviceProvider)
{
    // Relative to the generator project directory, matching how the MSBuild pre-build hook in the Launcher invokes the tool
    private static readonly string DefaultExamplesRoot = Path.Combine("..", "..", "examples", "code-only");
    private static readonly string DefaultDocsPath = Path.Combine("..", "..", "docs", "manual", "code-only", "examples");
    private static readonly string DefaultMediaPath = Path.Combine(DefaultDocsPath, "media");

    private const string DefaultOutputPath = "examples-manifest.json";

    /// <summary>
    /// Creates and configures the root command with all subcommands.
    /// </summary>
    /// <returns>Configured root command ready for parsing.</returns>
    public RootCommand CreateRootCommand()
    {
        var pathArgument = new Argument<DirectoryInfo>("examples-root-path")
        {
            Description = $"The root path of the examples to scan. Defaults to {DefaultExamplesRoot}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultExamplesRoot)
        };

        return new RootCommand("Stride examples metadata parser")
        {
            CreateScanCommand(pathArgument),
            CreateGenerateCommand(pathArgument),
            CreateDocsCommand(pathArgument)
        };
    }

    private static Option<DirectoryInfo?> CreateMediaOption() => new("--media-path")
    {
        Description = "The docs media folder used to confirm that every explicit media: file exists. Skipped when omitted."
    };

    private Command CreateScanCommand(Argument<DirectoryInfo> pathArgument)
    {
        var mediaOption = CreateMediaOption();
        var scanCommand = new Command("scan", "Scans the examples, validates their metadata, and reports what it finds.");

        scanCommand.Arguments.Add(pathArgument);
        scanCommand.Options.Add(mediaOption);

        scanCommand.SetAction((parseResult, cancellationToken) => RunAsync(service => service.ScanAndValidateAsync(
            parseResult.GetValue(pathArgument),
            parseResult.GetValue(mediaOption),
            cancellationToken)));

        return scanCommand;
    }

    private Command CreateGenerateCommand(Argument<DirectoryInfo> pathArgument)
    {
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "The output path for the generated manifest JSON file.",
            DefaultValueFactory = _ => DefaultOutputPath
        };

        var mediaOption = CreateMediaOption();

        var strictOption = new Option<bool>("--strict")
        {
            Description = "Treat validation errors as fatal: report them, write no manifest, and exit non-zero."
        };

        var generateCommand = new Command("generate", "Generates the metadata JSON manifest.");

        generateCommand.Arguments.Add(pathArgument);
        generateCommand.Options.Add(outputOption);
        generateCommand.Options.Add(mediaOption);
        generateCommand.Options.Add(strictOption);

        generateCommand.SetAction((parseResult, cancellationToken) => RunAsync(service => service.ScanAndGenerateManifestAsync(
            parseResult.GetValue(pathArgument),
            parseResult.GetValue(outputOption) ?? DefaultOutputPath,
            parseResult.GetValue(mediaOption),
            parseResult.GetValue(strictOption),
            cancellationToken)));

        return generateCommand;
    }

    private Command CreateDocsCommand(Argument<DirectoryInfo> pathArgument)
    {
        var docsOption = new Option<DirectoryInfo>("--docs-path")
        {
            Description = $"The examples documentation folder. Defaults to {DefaultDocsPath}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultDocsPath)
        };

        // Unlike scan and generate, this one defaults. There the folder is only used to confirm that an
        // explicit media: file exists, and omitting it skips a check. Here it decides whether a page gets
        // a screenshot at all, so omitting it silently published 41 pages with no image - including 25
        // whose image had been sitting in the media folder the whole time. A default that matches
        // --docs-path is the difference between forgetting a flag and losing every screenshot.
        var mediaOption = new Option<DirectoryInfo?>("--media-path")
        {
            Description = $"The docs media folder. An image is linked only when the file exists. Defaults to {DefaultMediaPath}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultMediaPath)
        };

        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Report which files would be written, and write nothing."
        };

        var docsCommand = new Command("docs", "Generates the example documentation pages, landing pages and toc.");

        docsCommand.Arguments.Add(pathArgument);
        docsCommand.Options.Add(docsOption);
        docsCommand.Options.Add(mediaOption);
        docsCommand.Options.Add(dryRunOption);

        docsCommand.SetAction((parseResult, cancellationToken) => RunAsync(service => service.GenerateDocsAsync(
            parseResult.GetValue(pathArgument),
            parseResult.GetValue(docsOption),
            parseResult.GetValue(mediaOption),
            parseResult.GetValue(dryRunOption),
            cancellationToken)));

        return docsCommand;
    }

    // Every command runs in a service scope of its own
    private async Task<int> RunAsync(Func<ManifestService, Task<int>> command)
    {
        using var scope = serviceProvider.CreateScope();

        return await command(scope.ServiceProvider.GetRequiredService<ManifestService>());
    }
}