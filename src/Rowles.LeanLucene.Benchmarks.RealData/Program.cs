using BenchmarkDotNet.Running;

namespace Rowles.LeanLucene.Benchmarks.RealData;

internal static class Program
{
    private static int Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes([
            typeof(GutenbergAnalysisBenchmarks),
            typeof(GutenbergIndexingBenchmarks),
            typeof(GutenbergSearchBenchmarks)
        ]).Run(args);

        return 0;
    }
}
