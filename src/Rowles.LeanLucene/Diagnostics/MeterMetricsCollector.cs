using System.Diagnostics.Metrics;

namespace Rowles.LeanLucene.Diagnostics;

/// <summary>
/// <see cref="IMetricsCollector"/> backed by <see cref="System.Diagnostics.Metrics.Meter"/>.
/// Instruments are published under the meter name <c>Rowles.LeanLucene</c> and can be consumed
/// by any <see cref="MeterListener"/> — including OpenTelemetry OTLP exporters.
/// </summary>
/// <remarks>
/// Construct once and assign to both <see cref="Search.Searcher.IndexSearcherConfig.Metrics"/>
/// and <see cref="Index.Indexer.IndexWriterConfig.Metrics"/> so all operations share the same meter.
/// Pass an <see cref="IMeterFactory"/> when using the Microsoft.Extensions.DependencyInjection hosting
/// model; otherwise a standalone <see cref="Meter"/> is created automatically.
/// </remarks>
public sealed class MeterMetricsCollector : IMetricsCollector, IDisposable
{
    private readonly Meter _meter;
    private readonly bool _ownsMeter;

    private readonly Histogram<double> _searchDuration;
    private readonly Counter<long>     _searchCount;
    private readonly Counter<long>     _cacheHits;
    private readonly Counter<long>     _cacheMisses;
    private readonly Histogram<double> _flushDuration;
    private readonly Histogram<double> _mergeDuration;
    private readonly Counter<long>     _mergeSegments;
    private readonly Histogram<double> _commitDuration;

    // Interlocked shadow counters so GetSnapshot() remains fully functional.
    private long _snSearchCount;
    private long _snSearchTotalMs;
    private long _snSearchMaxMs;
    private long _snCacheHits;
    private long _snCacheMisses;
    private long _snFlushCount;
    private long _snFlushTotalMs;
    private long _snMergeCount;
    private long _snMergeSegments;
    private long _snMergeTotalMs;
    private long _snCommitCount;
    private long _snCommitTotalMs;
    private readonly long[] _snLatencyBuckets = new long[8];
    private static readonly int[] BucketThresholdsMs = [1, 5, 10, 50, 100, 500, 1000];

    /// <summary>
    /// Initialises a <see cref="MeterMetricsCollector"/> using the provided <see cref="IMeterFactory"/>
    /// (for hosted / DI scenarios). If <paramref name="meterFactory"/> is <see langword="null"/> a
    /// standalone <see cref="Meter"/> is created and owned by this instance.
    /// </summary>
    public MeterMetricsCollector(IMeterFactory? meterFactory = null)
    {
        if (meterFactory is not null)
        {
            _meter = meterFactory.Create("Rowles.LeanLucene");
            _ownsMeter = false;
        }
        else
        {
            _meter = new Meter("Rowles.LeanLucene");
            _ownsMeter = true;
        }

        _searchDuration = _meter.CreateHistogram<double>(
            "leanlucene.search.duration", unit: "ms",
            description: "Elapsed time for each search operation.");

        _searchCount = _meter.CreateCounter<long>(
            "leanlucene.search.count", unit: "{query}",
            description: "Total number of search operations executed.");

        _cacheHits = _meter.CreateCounter<long>(
            "leanlucene.cache.hits", unit: "{hit}",
            description: "Number of query cache hits.");

        _cacheMisses = _meter.CreateCounter<long>(
            "leanlucene.cache.misses", unit: "{miss}",
            description: "Number of query cache misses.");

        _flushDuration = _meter.CreateHistogram<double>(
            "leanlucene.index.flush.duration", unit: "ms",
            description: "Elapsed time for each segment flush.");

        _mergeDuration = _meter.CreateHistogram<double>(
            "leanlucene.index.merge.duration", unit: "ms",
            description: "Elapsed time for each segment merge.");

        _mergeSegments = _meter.CreateCounter<long>(
            "leanlucene.index.merge.segments", unit: "{segment}",
            description: "Total number of segments consumed by merge operations.");

        _commitDuration = _meter.CreateHistogram<double>(
            "leanlucene.index.commit.duration", unit: "ms",
            description: "Elapsed time for each index commit.");
    }

    /// <inheritdoc/>
    public void RecordSearchLatency(TimeSpan elapsed)
    {
        double ms = elapsed.TotalMilliseconds;
        _searchDuration.Record(ms);
        _searchCount.Add(1);

        Interlocked.Increment(ref _snSearchCount);
        long msLong = (long)ms;
        Interlocked.Add(ref _snSearchTotalMs, msLong);
        InterlockedMax(ref _snSearchMaxMs, msLong);

        int bucket = 0;
        for (int i = 0; i < BucketThresholdsMs.Length; i++)
        {
            if (msLong < BucketThresholdsMs[i]) { bucket = i; break; }
            bucket = i + 1;
        }
        Interlocked.Increment(ref _snLatencyBuckets[bucket]);
    }

    /// <inheritdoc/>
    public void RecordCacheHit()
    {
        _cacheHits.Add(1);
        Interlocked.Increment(ref _snCacheHits);
    }

    /// <inheritdoc/>
    public void RecordCacheMiss()
    {
        _cacheMisses.Add(1);
        Interlocked.Increment(ref _snCacheMisses);
    }

    /// <inheritdoc/>
    public void RecordFlush(TimeSpan elapsed)
    {
        double ms = elapsed.TotalMilliseconds;
        _flushDuration.Record(ms);

        Interlocked.Increment(ref _snFlushCount);
        Interlocked.Add(ref _snFlushTotalMs, (long)ms);
    }

    /// <inheritdoc/>
    public void RecordMerge(TimeSpan elapsed, int segmentsMerged)
    {
        double ms = elapsed.TotalMilliseconds;
        _mergeDuration.Record(ms);
        _mergeSegments.Add(segmentsMerged);

        Interlocked.Increment(ref _snMergeCount);
        Interlocked.Add(ref _snMergeSegments, segmentsMerged);
        Interlocked.Add(ref _snMergeTotalMs, (long)ms);
    }

    /// <inheritdoc/>
    public void RecordCommit(TimeSpan elapsed)
    {
        double ms = elapsed.TotalMilliseconds;
        _commitDuration.Record(ms);

        Interlocked.Increment(ref _snCommitCount);
        Interlocked.Add(ref _snCommitTotalMs, (long)ms);
    }

    /// <inheritdoc/>
    public MetricsSnapshot GetSnapshot()
    {
        long searchCount = Interlocked.Read(ref _snSearchCount);
        long hits        = Interlocked.Read(ref _snCacheHits);
        long misses      = Interlocked.Read(ref _snCacheMisses);
        long total       = hits + misses;

        var buckets = new long[_snLatencyBuckets.Length];
        for (int i = 0; i < buckets.Length; i++)
            buckets[i] = Interlocked.Read(ref _snLatencyBuckets[i]);

        return new MetricsSnapshot
        {
            SearchCount    = searchCount,
            SearchTotalMs  = Interlocked.Read(ref _snSearchTotalMs),
            SearchMaxMs    = Interlocked.Read(ref _snSearchMaxMs),
            SearchAvgMs    = searchCount > 0 ? (double)Interlocked.Read(ref _snSearchTotalMs) / searchCount : 0,
            CacheHits      = hits,
            CacheMisses    = misses,
            CacheHitRate   = total > 0 ? (double)hits / total : 0,
            FlushCount     = Interlocked.Read(ref _snFlushCount),
            FlushTotalMs   = Interlocked.Read(ref _snFlushTotalMs),
            MergeCount     = Interlocked.Read(ref _snMergeCount),
            MergeSegments  = Interlocked.Read(ref _snMergeSegments),
            MergeTotalMs   = Interlocked.Read(ref _snMergeTotalMs),
            CommitCount    = Interlocked.Read(ref _snCommitCount),
            CommitTotalMs  = Interlocked.Read(ref _snCommitTotalMs),
            LatencyHistogram = buckets
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsMeter) _meter.Dispose();
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
