using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

var summary = BenchmarkRunner.Run<SampleBenchmarks>();

public class SampleBenchmarks
{
    [Benchmark]
    public void BenchmarkMethod()
    {
        // Place benchmark code here
    }
}
