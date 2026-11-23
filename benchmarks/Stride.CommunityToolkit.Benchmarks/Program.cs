using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System.Reflection;

// BenchmarkDotNet refuses by default to measure against a non-optimized assembly, and a Stride
// package is not guaranteed to be an optimized build (a locally built engine is not). Everything
// built from this repository is still compiled in Release, so the validator is turned off rather
// than the measurement.
var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);

var switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly());

if (args.Length == 0)
{
    switcher.RunAll(config);
}
else
{
    switcher.Run(args, config);
}

return 0;
