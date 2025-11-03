using Stride.CommunityToolkit.Examples.MetadataGenerator;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ExamplesMetadataGenerator <examples-root-path> <output-json-path>");
    return 1;
}

var examplesRoot = args[0];
var outputPath = args[1];

var scanner = new MetadataScanner(examplesRoot, outputPath);

return await scanner.ScanAndGenerateAsync();