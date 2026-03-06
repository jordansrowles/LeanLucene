namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Aggregated result for a single benchmark method, including per-iteration
/// raw data and computed statistics across all metric dimensions.
/// </summary>
internal sealed class QuickBenchmarkResult
{
    public required string BenchmarkName { get; init; }
    public required int WarmupIterations { get; init; }
    public required int MeasuredIterations { get; init; }
    public required List<IterationResult> Iterations { get; init; }
    public required MetricStatistics TimeTicks { get; init; }
    public required MetricStatistics TimeMs { get; init; }
    public required MetricStatistics BytesAllocated { get; init; }
    public required MetricStatistics Gen0Collections { get; init; }
    public required MetricStatistics Gen1Collections { get; init; }
    public required MetricStatistics Gen2Collections { get; init; }

    /// <summary>
    /// Computes summary statistics from a sequence of double values.
    /// </summary>
    public static MetricStatistics ComputeStatistics(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return new MetricStatistics();

        var sorted = values.OrderBy(v => v).ToList();
        double sum = 0;
        foreach (var v in sorted)
            sum += v;

        double mean = sum / sorted.Count;
        double median = sorted.Count % 2 == 0
            ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0
            : sorted[sorted.Count / 2];

        double varianceSum = 0;
        foreach (var v in sorted)
            varianceSum += (v - mean) * (v - mean);

        double stddev = sorted.Count > 1
            ? Math.Sqrt(varianceSum / (sorted.Count - 1))
            : 0;

        return new MetricStatistics
        {
            Mean = mean,
            Median = median,
            Min = sorted[0],
            Max = sorted[^1],
            StandardDeviation = stddev,
            SampleCount = sorted.Count
        };
    }
}

/// <summary>
/// Basic descriptive statistics for a single metric dimension.
/// </summary>
internal sealed class MetricStatistics
{
    public double Mean { get; init; }
    public double Median { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double StandardDeviation { get; init; }
    public int SampleCount { get; init; }
}
