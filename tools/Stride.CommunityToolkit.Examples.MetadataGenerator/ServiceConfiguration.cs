using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator;

public static class ServiceConfiguration
{
    public static void SetupServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(context.Configuration)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .CreateLogger();

            builder.AddSerilog(logger);
        });

        //services.AddScoped<MetadataScannerService>(context.Configuration);
    }
}
