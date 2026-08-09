using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

// Stride's 4.4.0-dev packages ship non-optimized assemblies, which BenchmarkDotNet refuses to
// measure against by default. Everything built from this repository is still compiled in Release;
// only the engine dependency is not, so the validator is turned off rather than the measurement.
var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);

var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());

if (args == null || args.Length == 0)
{
    switcher.RunAll(config);
}
else
{
    switcher.Run(args, config);
}

return 0;
