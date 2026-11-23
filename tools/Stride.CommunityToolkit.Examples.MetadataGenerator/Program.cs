using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Stride.CommunityToolkit.Examples.MetadataGenerator;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Services;
using System.Globalization;

// Warnings and errors go to stderr, which the MSBuild pre-build hook raises at high importance while
// leaving stdout at low. That is what keeps a clean build silent: the per-example lines are hidden, and
// a failed validation still prints its reasons and not only an exit code.
var log = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture,
        standardErrorFromLevel: LogEventLevel.Warning)
    .CreateLogger();

// A process runs one command, so the service and what it uses are built once, by hand
using var loggers = new SerilogLoggerFactory(log, dispose: true);

var service = new ManifestService(
    loggers.CreateLogger<ManifestService>(),
    new ExampleScanner(loggers.CreateLogger<ExampleScanner>()),
    new MetadataParser(loggers.CreateLogger<MetadataParser>()),
    new MetadataValidator(loggers.CreateLogger<MetadataValidator>()),
    new ManifestWriter(loggers.CreateLogger<ManifestWriter>()),
    new DocsGenerator(loggers.CreateLogger<DocsGenerator>()));

return new CommandLineConfiguration(service).CreateRootCommand().Parse(args).Invoke();