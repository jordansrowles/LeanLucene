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
