using Microsoft.Extensions.Logging;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Services;

public class ManifestService(ILogger<ManifestService> logger)
{
    private readonly ILogger<ManifestService> _logger = logger;

    public void GenerateManifest()
    {
        _logger.LogInformation("Generating manifest...");
    }
}
