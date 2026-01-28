namespace Rowles.LeanLucene.Diagnostics;

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
