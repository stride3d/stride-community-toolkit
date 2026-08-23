using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Stride.CommunityToolkit.Examples.MetadataGenerator;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

// The content root must be the tool's own folder, not the caller's working directory. Configuration
// is resolved against the content root, so with the default the MSBuild pre-build hook - which runs
// the generator from the Launcher directory - silently found no appsettings.json, configured no
// Serilog sinks, and produced no output whatsoever: a failed validation reported nothing but an exit
// code. appsettings.json is copied next to the assembly, so BaseDirectory always finds it.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddScoped<ExampleScanner>();
builder.Services.AddScoped<MetadataParser>();
builder.Services.AddScoped<MetadataValidator>();
builder.Services.AddScoped<ManifestWriter>();
builder.Services.AddScoped<DocsGenerator>();
builder.Services.AddScoped<ManifestService>();

using var host = builder.Build();

var cliConfiguration = new CommandLineConfiguration(host.Services);
var rootCommand = cliConfiguration.CreateRootCommand();
var parseResult = rootCommand.Parse(args);

return parseResult.Invoke();