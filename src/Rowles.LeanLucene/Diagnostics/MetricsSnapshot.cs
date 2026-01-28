namespace Rowles.LeanLucene.Diagnostics;

/// <summary>
/// Point-in-time snapshot of operational metrics.
/// </summary>
public sealed class MetricsSnapshot
{
    public static readonly MetricsSnapshot Empty = new();

    // Search
    public long SearchCount { get; init; }
    public long SearchTotalMs { get; init; }
    public long SearchMaxMs { get; init; }
    public double SearchAvgMs { get; init; }

    // Cache
    public long CacheHits { get; init; }
    public long CacheMisses { get; init; }
    public double CacheHitRate { get; init; }

    // Writer
    public long FlushCount { get; init; }
    public long FlushTotalMs { get; init; }
    public long MergeCount { get; init; }
    public long MergeSegments { get; init; }
    public long MergeTotalMs { get; init; }
    public long CommitCount { get; init; }
    public long CommitTotalMs { get; init; }

    /// <summary>
    /// Latency histogram: buckets are [&lt;1ms, &lt;5ms, &lt;10ms, &lt;50ms, &lt;100ms, &lt;500ms, &lt;1000ms, ≥1000ms].
    /// </summary>
    public long[]? LatencyHistogram { get; init; }
}
