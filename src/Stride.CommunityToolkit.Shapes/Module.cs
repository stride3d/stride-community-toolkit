using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.CommunityToolkit.Shapes;

internal static class Module
{
    // Without this the assembly is never scanned, so ShapeComponent does not appear in Game Studio's
    // Add-component list at all - the same registration the core toolkit and DebugShapes each do.
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
    }
}