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
    /// <summary>
    /// The examples root, relative to the generator project directory. Matches how the MSBuild
    /// pre-build hook in the Launcher invokes the tool.
    /// </summary>
    private const string DefaultExamplesRoot = @"..\..\examples\code-only";

    private const string DefaultOutputPath = "examples-manifest.json";

    private const string DefaultDocsPath = "../../docs/manual/code-only/examples";

    private const string DefaultMediaPath = "../../docs/manual/code-only/examples/media";

    /// <summary>
    /// Creates and configures the root command with all subcommands.
    /// </summary>
    /// <returns>Configured root command ready for parsing.</returns>
    public RootCommand CreateRootCommand()
    {
        var pathArgument = CreatePathArgument();

        return new RootCommand("Stride examples metadata parser")
        {
            CreateScanCommand(pathArgument),
            CreateGenerateCommand(pathArgument),
            CreateDocsCommand(pathArgument)
        };
    }

    private static Argument<DirectoryInfo> CreatePathArgument()
        => new("examples-root-path")
        {
            Description = $"The root path of the examples to scan. Defaults to {DefaultExamplesRoot}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("..", "..", "examples", "code-only"))
        };

    private static Option<DirectoryInfo?> CreateMediaOption()
        => new("--media-path")
        {
            Description = "The docs media folder used to confirm that every explicit media: file exists. Skipped when omitted."
        };

    private static Option<bool> CreateStrictOption()
        => new("--strict")
        {
            Description = "Treat validation errors as fatal: report them, write no manifest, and exit non-zero."
        };

    private Command CreateScanCommand(Argument<DirectoryInfo> pathArgument)
    {
        var mediaOption = CreateMediaOption();
        var scanCommand = new Command("scan", "Scans the examples, validates their metadata, and reports what it finds.");

        scanCommand.Arguments.Add(pathArgument);
        scanCommand.Options.Add(mediaOption);

        scanCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<ManifestService>();
            var validator = scope.ServiceProvider.GetRequiredService<MetadataValidator>();
            var scanner = scope.ServiceProvider.GetRequiredService<ExampleScanner>();
            var path = parseResult.GetValue(pathArgument);

            var scan = await service.ScanExamplesAsync(path, cancellationToken);

            if (scan.Examples.Count == 0)
            {
                return ManifestService.ExitNoExamplesFound;
            }

            var published = scan.Examples.Where(example => example.Metadata.Enabled != false).ToList();
            var messages = validator.Validate(published, parseResult.GetValue(mediaOption), scanner.FindProjectNames(path!));
            var errorCount = service.ReportValidation(messages) + scan.Failures;

            return errorCount > 0 ? ManifestService.ExitValidationFailed : ManifestService.ExitSuccess;
        });

        return scanCommand;
    }

    private Command CreateDocsCommand(Argument<DirectoryInfo> pathArgument)
    {
        var docsOption = new Option<DirectoryInfo>("--docs-path")
        {
            Description = $"The examples documentation folder. Defaults to {DefaultDocsPath}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("..", "..", "docs", "manual", "code-only", "examples"))
        };

        // Unlike scan and generate, this one defaults. There the folder is only used to confirm that an
        // explicit media: file exists, and omitting it skips a check. Here it decides whether a page gets
        // a screenshot at all, so omitting it silently published 41 pages with no image - including 25
        // whose image had been sitting in the media folder the whole time. A default that matches
        // --docs-path is the difference between forgetting a flag and losing every screenshot.
        var mediaOption = new Option<DirectoryInfo?>("--media-path")
        {
            Description = $"The docs media folder. An image is linked only when the file exists. Defaults to {DefaultMediaPath}, relative to the current directory.",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("..", "..", "docs", "manual", "code-only", "examples", "media"))
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

        docsCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<ManifestService>();

            return await service.GenerateDocsAsync(
                parseResult.GetValue(pathArgument),
                parseResult.GetValue(docsOption),
                parseResult.GetValue(mediaOption),
                parseResult.GetValue(dryRunOption),
                cancellationToken);
        });

        return docsCommand;
    }

    private Command CreateGenerateCommand(Argument<DirectoryInfo> pathArgument)
    {
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "The output path for the generated manifest JSON file.",
            DefaultValueFactory = _ => DefaultOutputPath
        };

        var mediaOption = CreateMediaOption();
        var strictOption = CreateStrictOption();

        var generateCommand = new Command("generate", "Generates the metadata JSON manifest.");

        generateCommand.Arguments.Add(pathArgument);
        generateCommand.Options.Add(outputOption);
        generateCommand.Options.Add(mediaOption);
        generateCommand.Options.Add(strictOption);

        generateCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            using var scope = serviceProvider.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<ManifestService>();

            return await service.ScanAndGenerateManifestAsync(
                parseResult.GetValue(pathArgument),
                parseResult.GetValue(outputOption) ?? DefaultOutputPath,
                parseResult.GetValue(mediaOption),
                parseResult.GetValue(strictOption),
                cancellationToken);
        });

        return generateCommand;
    }
}