using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Stride.CommunityToolkit.Examples.MetadataGenerator;
using Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

var builder = Host.CreateApplicationBuilder(args);

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
builder.Services.AddScoped<ManifestService>();

using var host = builder.Build();

var cliConfiguration = new CommandLineConfiguration(host.Services);
var rootCommand = cliConfiguration.CreateRootCommand();
var parseResult = rootCommand.Parse(args);

return parseResult.Invoke();