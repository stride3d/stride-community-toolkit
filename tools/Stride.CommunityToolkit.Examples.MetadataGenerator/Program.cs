using Microsoft.Extensions.Hosting;
using Stride.CommunityToolkit.Examples.MetadataGenerator;
using System.CommandLine;

// ToDo
// Add serilog
// Fix builder generation, move to the top so it is used just once
// Use DI
// Use new command line

var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((_, config) => AppConfigurationBuilder.SetupConfiguration(args, config))
        .ConfigureServices(ServiceConfiguration.SetupServices)
        .Build();

await RunApplication(hostBuilder, args);

static async Task<int> RunApplication(IHost hostBuilder, string[] args)
{
    // scan examples-root-path --verbosity just titles)
    // scan examples-root-path --verbosity detailed
    // generate examples-root-path output-json-path
    var rootCommand = new RootCommand("Examples metadata parser.");

    var scanCommand = new Command("scan", "Scans the examples and lists metadata.");
    scanCommand.SetAction((parseResult) =>
    {
        Console.WriteLine("Scan command executed.");
    });
    rootCommand.Subcommands.Add(scanCommand);

    var generateCommand = new Command("generate", "Generates the metadata JSON file.");
    generateCommand.SetAction((parseResult) =>
    {
        Console.WriteLine("Generate command executed.");
    });
    rootCommand.Subcommands.Add(generateCommand);

    var pathArgument = new Argument<DirectoryInfo>("examples-root-path")
    {
        Description = "The root path of the examples to scan."
    };

    scanCommand.Arguments.Add(pathArgument);
    generateCommand.Arguments.Add(pathArgument);

    rootCommand.Parse("-h").Invoke();

    //if (args.Length < 2)
    //{
    //    Console.Error.WriteLine("Usage: ExamplesMetadataGenerator <examples-root-path> <output-json-path>");
    //    return 1;
    //}

    //var examplesRoot = args[0];
    //var outputPath = args[1];

    //var scanner = new MetadataScanner(examplesRoot, outputPath);

    //ParseResult parseResult = rootCommand.Parse(args);

    //if (parseResult.Errors.Count == 0 && parseResult.GetValue(pathArgument) is DirectoryInfo parsedFile)
    //{
    //    Console.WriteLine($"Parsed examples-root-path: {parsedFile.FullName}");
    //    return 0;
    //}
    //foreach (ParseError parseError in parseResult.Errors)
    //{
    //    Console.Error.WriteLine(parseError.Message);
    //}

    return 1;

    return await Task.FromResult(0);

    //return await scanner.ScanAndGenerateAsync();


}