using Stride.Core;
using Stride.Core.Reflection;
using System.Reflection;

namespace Stride.CommunityToolkit.Box2D;

internal static class Module
{
    // Without this the assembly is never scanned, so the grabber script does not appear in Game
    // Studio's Add-component list at all - the same registration the core toolkit, DebugShapes and
    // Shapes each do.
    [ModuleInitializer]
    internal static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}