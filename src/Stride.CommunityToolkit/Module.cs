using Stride.Core.Reflection;

namespace Stride.CommunityToolkit;

internal static class Module
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
    }
}