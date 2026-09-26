using Stride.Core;
using Stride.Core.Reflection;
using System.Reflection;

namespace Stride.CommunityToolkit.Effects;

internal static class Module
{
    // Without this the assembly is never scanned, so the transforms do not appear in Game Studio's
    // compositor editor - the same registration every toolkit package with data contracts does.
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}