namespace Rowles.LeanLucene.Benchmarks.Quick;

/// <summary>
/// Captures metrics from a single measured iteration.
/// </summary>
internal sealed record IterationResult(
    long ElapsedTicks,
    double ElapsedMs,
    long BytesAllocated,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);
