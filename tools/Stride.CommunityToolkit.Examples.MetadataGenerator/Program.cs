using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stride.CommunityToolkit.Examples.MetadataGenerator;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Services;
using System.CommandLine;

// ToDo
// Add serilog
// Fix builder generation, move to the top so it is used just once
// Use DI
// Use new command line

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddScoped<ManifestService>();

using IHost host = builder.Build();

//var hostBuilder = Host.CreateDefaultBuilder(args);
//        .ConfigureAppConfiguration((_, config) => AppConfigurationBuilder.SetupConfiguration(args, config))
//        .ConfigureServices(ServiceConfiguration.SetupServices)
//        .Build();

//await RunApplication(hostBuilder, args);

//var commandLineBuilder = new CommandLineBuilder(rootCommand)

Option<string> fileOption = new("--file")
{
    Description = "The file to read and display on the console."
};

RootCommand rootCommand = new("Stride examples metadata parser");

Argument<DirectoryInfo> pathArgument = new("examples-root-path")
{
    Description = "The root path of the examples to scan.",
    DefaultValueFactory = parseResult => new DirectoryInfo("..\\..\\examples\\code-only")
};

var scanCommand = new Command("scan", "Scans the examples and lists metadata.");
scanCommand.SetAction(async (parseResult) =>
{
    DirectoryInfo? parsedFile = parseResult.GetValue(pathArgument);

    Console.WriteLine("Scan command executed.");
    if (parsedFile != null)
    {
        Console.WriteLine($"Parsed examples-root-path: {parsedFile.FullName}, exists: {parsedFile.Exists}");

        if (parsedFile.Exists)
        {
            var scanner = new MetadataScanner(parsedFile.FullName, "examples-metadata.json");

            await scanner.ScanExamplesAsync();
        }
    }
});
scanCommand.Arguments.Add(pathArgument);

var scanCommand2 = new Command("scan2", "Scans the examples and lists metadata.");
scanCommand2.SetAction(async (parseResult) =>
{
    Console.WriteLine("Scan 2 command executed.");

    var serviceProvider = host.Services;
    var manifestService = serviceProvider.GetRequiredService<ManifestService>();
    manifestService.GenerateManifest();
});
scanCommand.Arguments.Add(pathArgument);

rootCommand.Subcommands.Add(scanCommand);
rootCommand.Subcommands.Add(scanCommand2);

//rootCommand.Options.Add(fileOption);

//rootCommand.SetAction(parseResult =>
//{
//    string? parsedFile = parseResult.GetValue(fileOption);
//    ReadFile(parsedFile);
//    return 0;
//});

ParseResult parseResult = rootCommand.Parse(args);

return parseResult.Invoke();

//foreach (ParseError parseError in parseResult.Errors)
//{
//    Console.Error.WriteLine(parseError.Message);
//}

//return 1;

static void ReadFile(string file)
{
    Console.WriteLine(file);
}

//static async Task<int> RunApplication(IHost hostBuilder, string[] args)
//{
//    // scan examples-root-path --verbosity just titles)
//    // scan examples-root-path --verbosity detailed
//    // generate examples-root-path output-json-path
//    var rootCommand = new RootCommand("Examples metadata parser.");

//    var scanCommand = new Command("scan", "Scans the examples and lists metadata.");
//    scanCommand.SetAction((parseResult) =>
//    {
//        Console.WriteLine("Scan command executed.");
//    });
//    rootCommand.Subcommands.Add(scanCommand);

//    var generateCommand = new Command("generate", "Generates the metadata JSON file.");
//    generateCommand.SetAction((parseResult) =>
//    {
//        Console.WriteLine("Generate command executed.");
//    });
//    rootCommand.Subcommands.Add(generateCommand);

//    var pathArgument = new Argument<DirectoryInfo>("examples-root-path")
//    {
//        Description = "The root path of the examples to scan."
//    };

//    scanCommand.Arguments.Add(pathArgument);
//    generateCommand.Arguments.Add(pathArgument);

//    //rootCommand.Parse("-h").Invoke();

//    //if (args.Length < 2)
//    //{
//    //    Console.Error.WriteLine("Usage: ExamplesMetadataGenerator <examples-root-path> <output-json-path>");
//    //    return 1;
//    //}

//    //var examplesRoot = args[0];
//    //var outputPath = args[1];

//    //var scanner = new MetadataScanner(examplesRoot, outputPath);

//    //ParseResult parseResult = rootCommand.Parse(args);

//    //if (parseResult.Errors.Count == 0 && parseResult.GetValue(pathArgument) is DirectoryInfo parsedFile)
//    //{
//    //    Console.WriteLine($"Parsed examples-root-path: {parsedFile.FullName}");
//    //    return 0;
//    //}
//    //foreach (ParseError parseError in parseResult.Errors)
//    //{
//    //    Console.Error.WriteLine(parseError.Message);
//    //}

//    //return 1;

//    //return await Task.FromResult(0);

//    //return await scanner.ScanAndGenerateAsync();

//    return rootCommand.In(args);


//}