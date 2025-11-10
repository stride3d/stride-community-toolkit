using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Stride.CommunityToolkit.Examples.MetadataGenerator;

public static class AppConfigurationBuilder
{
    public static void SetupConfiguration(string[] args, IConfigurationBuilder config)
    {
        var entryAssembly = Assembly.GetEntryAssembly();

        if (entryAssembly != null)
        {
            config.AddUserSecrets(entryAssembly);
        }
    }
}