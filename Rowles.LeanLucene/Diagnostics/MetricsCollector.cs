using System.Diagnostics;

namespace Rowles.LeanLucene.Diagnostics;

/// <summary>
/// Interface for collecting operational metrics from IndexSearcher, IndexWriter, and QueryCache.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>Records a search latency sample.</summary>
    void RecordSearchLatency(TimeSpan elapsed);

    /// <summary>Records a query cache hit.</summary>
    void RecordCacheHit();

    /// <summary>Records a query cache miss.</summary>
    void RecordCacheMiss();

    /// <summary>Records a segment flush event.</summary>
    void RecordFlush(TimeSpan elapsed);

    /// <summary>Records a segment merge event.</summary>
    void RecordMerge(TimeSpan elapsed, int segmentsMerged);

    /// <summary>Records a commit event.</summary>
    void RecordCommit(TimeSpan elapsed);

    /// <summary>Takes a point-in-time snapshot of all metrics.</summary>
    MetricsSnapshot GetSnapshot();
}

/// <summary>
/// No-op metrics collector — the default when no metrics are configured.
/// </summary>
public sealed class NullMetricsCollector : IMetricsCollector
{
    public static readonly NullMetricsCollector Instance = new();

    public void RecordSearchLatency(TimeSpan elapsed) { }
    public void RecordCacheHit() { }
    public void RecordCacheMiss() { }
    public void RecordFlush(TimeSpan elapsed) { }
    public void RecordMerge(TimeSpan elapsed, int segmentsMerged) { }
    public void RecordCommit(TimeSpan elapsed) { }
    public MetricsSnapshot GetSnapshot() => MetricsSnapshot.Empty;
}

/// <summary>
/// Lock-free metrics collector using Interlocked operations.
/// </summary>
public sealed class DefaultMetricsCollector : IMetricsCollector
{
    private long _searchCount;
    private long _searchTotalMs;
    private long _searchMaxMs;
    private long _cacheHits;
    private long _cacheMisses;
    private long _flushCount;
    private long _flushTotalMs;
    private long _mergeCount;
    private long _mergeSegments;
    private long _mergeTotalMs;
    private long _commitCount;
    private long _commitTotalMs;

    // Latency histogram buckets: <1ms, <5ms, <10ms, <50ms, <100ms, <500ms, <1000ms, ≥1000ms
    private readonly long[] _latencyBuckets = new long[8];
    private static readonly int[] BucketThresholdsMs = [1, 5, 10, 50, 100, 500, 1000];

    public void RecordSearchLatency(TimeSpan elapsed)
    {
        long ms = (long)elapsed.TotalMilliseconds;
        Interlocked.Increment(ref _searchCount);
        Interlocked.Add(ref _searchTotalMs, ms);
        InterlockedMax(ref _searchMaxMs, ms);

        int bucket = 0;
        for (int i = 0; i < BucketThresholdsMs.Length; i++)
        {
            if (ms < BucketThresholdsMs[i]) { bucket = i; break; }
            bucket = i + 1;
        }
        Interlocked.Increment(ref _latencyBuckets[bucket]);
    }

    public void RecordCacheHit() => Interlocked.Increment(ref _cacheHits);
    public void RecordCacheMiss() => Interlocked.Increment(ref _cacheMisses);

    public void RecordFlush(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _flushCount);
        Interlocked.Add(ref _flushTotalMs, (long)elapsed.TotalMilliseconds);
    }

    public void RecordMerge(TimeSpan elapsed, int segmentsMerged)
    {
        Interlocked.Increment(ref _mergeCount);
        Interlocked.Add(ref _mergeSegments, segmentsMerged);
        Interlocked.Add(ref _mergeTotalMs, (long)elapsed.TotalMilliseconds);
    }

    public void RecordCommit(TimeSpan elapsed)
    {
        Interlocked.Increment(ref _commitCount);
        Interlocked.Add(ref _commitTotalMs, (long)elapsed.TotalMilliseconds);
    }

    public MetricsSnapshot GetSnapshot()
    {
        long searchCount = Interlocked.Read(ref _searchCount);
        long hits = Interlocked.Read(ref _cacheHits);
        long misses = Interlocked.Read(ref _cacheMisses);
        long totalCacheLookups = hits + misses;

        var buckets = new long[_latencyBuckets.Length];
        for (int i = 0; i < buckets.Length; i++)
            buckets[i] = Interlocked.Read(ref _latencyBuckets[i]);

        return new MetricsSnapshot
        {
            SearchCount = searchCount,
            SearchTotalMs = Interlocked.Read(ref _searchTotalMs),
            SearchMaxMs = Interlocked.Read(ref _searchMaxMs),
            SearchAvgMs = searchCount > 0 ? (double)Interlocked.Read(ref _searchTotalMs) / searchCount : 0,
            CacheHits = hits,
            CacheMisses = misses,
            CacheHitRate = totalCacheLookups > 0 ? (double)hits / totalCacheLookups : 0,
            FlushCount = Interlocked.Read(ref _flushCount),
            FlushTotalMs = Interlocked.Read(ref _flushTotalMs),
            MergeCount = Interlocked.Read(ref _mergeCount),
            MergeSegments = Interlocked.Read(ref _mergeSegments),
            MergeTotalMs = Interlocked.Read(ref _mergeTotalMs),
            CommitCount = Interlocked.Read(ref _commitCount),
            CommitTotalMs = Interlocked.Read(ref _commitTotalMs),
            LatencyHistogram = buckets
        };
    }

    private static void InterlockedMax(ref long location, long value)
    {
        long current = Interlocked.Read(ref location);
        while (value > current)
        {
            long prev = Interlocked.CompareExchange(ref location, value, current);
            if (prev == current) break;
            current = prev;
        }
    }
}
