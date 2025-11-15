using BenchmarkDotNet.Running;
using System.Reflection;

var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());

if (args == null || args.Length == 0)
{
    switcher.RunAll();
}
else
{
    switcher.Run(args);
}

return 0;