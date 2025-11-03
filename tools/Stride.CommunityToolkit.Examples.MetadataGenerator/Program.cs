using Stride.CommunityToolkit.Examples.MetadataGenerator;

// ToDo
// Add serilog
// Fix builder generation, move to the top so it is used just once
// Use DI
// Use new command line

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ExamplesMetadataGenerator <examples-root-path> <output-json-path>");
    return 1;
}

var examplesRoot = args[0];
var outputPath = args[1];

var scanner = new MetadataScanner(examplesRoot, outputPath);

return await scanner.ScanAndGenerateAsync();