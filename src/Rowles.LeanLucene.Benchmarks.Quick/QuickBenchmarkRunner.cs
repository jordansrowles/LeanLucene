namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Executes quick benchmarks using the prescribed GC-warming measurement protocol.
/// Collects timing, allocation and GC metrics per iteration.
/// </summary>
internal static class QuickBenchmarkRunner
{
    /// <summary>
    /// Runs a single benchmark through warmup and measured iterations,
    /// returning the aggregated result.
    /// </summary>
    public static QuickBenchmarkResult Run(
        IQuickBenchmark benchmark,
        int warmupIterations = 3,
        int measuredIterations = 10)
    {
        benchmark.Setup();

        try
        {
            // Warmup: JIT-compile hot paths, results discarded.
            for (int i = 0; i < warmupIterations; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                benchmark.Run();
            }

            var iterations = new List<IterationResult>(measuredIterations);

            // Measured loop with GC warming per the prescribed pattern.
            for (int i = 0; i < measuredIterations; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                long allocBefore = GC.GetTotalAllocatedBytes(precise: true);
                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();
                benchmark.Run();
                sw.Stop();

                long allocAfter = GC.GetTotalAllocatedBytes(precise: true);
                int gen0After = GC.CollectionCount(0);
                int gen1After = GC.CollectionCount(1);
                int gen2After = GC.CollectionCount(2);

                iterations.Add(new IterationResult(
                    ElapsedTicks: sw.ElapsedTicks,
                    ElapsedMs: sw.Elapsed.TotalMilliseconds,
                    BytesAllocated: allocAfter - allocBefore,
                    Gen0Collections: gen0After - gen0Before,
                    Gen1Collections: gen1After - gen1Before,
                    Gen2Collections: gen2After - gen2Before));
            }

            return BuildResult(benchmark.Name, warmupIterations, measuredIterations, iterations);
        }
        finally
        {
            benchmark.Cleanup();
        }
    }

    private static QuickBenchmarkResult BuildResult(
        string name,
        int warmupIterations,
        int measuredIterations,
        List<IterationResult> iterations)
    {
        var tickValues = iterations.Select(i => (double)i.ElapsedTicks).ToList();
        var msValues = iterations.Select(i => i.ElapsedMs).ToList();
        var allocValues = iterations.Select(i => (double)i.BytesAllocated).ToList();
        var gen0Values = iterations.Select(i => (double)i.Gen0Collections).ToList();
        var gen1Values = iterations.Select(i => (double)i.Gen1Collections).ToList();
        var gen2Values = iterations.Select(i => (double)i.Gen2Collections).ToList();

        return new QuickBenchmarkResult
        {
            BenchmarkName = name,
            WarmupIterations = warmupIterations,
            MeasuredIterations = measuredIterations,
            Iterations = iterations,
            TimeTicks = QuickBenchmarkResult.ComputeStatistics(tickValues),
            TimeMs = QuickBenchmarkResult.ComputeStatistics(msValues),
            BytesAllocated = QuickBenchmarkResult.ComputeStatistics(allocValues),
            Gen0Collections = QuickBenchmarkResult.ComputeStatistics(gen0Values),
            Gen1Collections = QuickBenchmarkResult.ComputeStatistics(gen1Values),
            Gen2Collections = QuickBenchmarkResult.ComputeStatistics(gen2Values)
        };
    }
}
