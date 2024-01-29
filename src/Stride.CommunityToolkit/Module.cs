using Stride.Core.Reflection;
using System.Reflection;

namespace Stride.CommunityToolkit;
internal class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).GetTypeInfo().Assembly, AssemblyCommonCategories.Assets);
    }
}
