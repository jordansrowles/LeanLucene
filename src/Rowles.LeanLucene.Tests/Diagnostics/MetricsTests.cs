using Rowles.LeanLucene.Diagnostics;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Diagnostics;

public class MetricsTests : IDisposable
{
    private readonly string _dir;

    public MetricsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "metrics_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }
        catch { }
    }

    [Fact]
    public void DefaultMetricsCollector_RecordsSearchLatency()
    {
        var metrics = new DefaultMetricsCollector();
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(15));
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(25));

        var snap = metrics.GetSnapshot();
        Assert.Equal(2, snap.SearchCount);
        Assert.Equal(40, snap.SearchTotalMs);
        Assert.Equal(25, snap.SearchMaxMs);
        Assert.Equal(20.0, snap.SearchAvgMs, 1);
    }

    [Fact]
    public void DefaultMetricsCollector_RecordsCacheHitMiss()
    {
        var metrics = new DefaultMetricsCollector();
        metrics.RecordCacheHit();
        metrics.RecordCacheHit();
        metrics.RecordCacheMiss();

        var snap = metrics.GetSnapshot();
        Assert.Equal(2, snap.CacheHits);
        Assert.Equal(1, snap.CacheMisses);
        Assert.True(snap.CacheHitRate > 0.6);
    }

    [Fact]
    public void DefaultMetricsCollector_RecordsWriterOps()
    {
        var metrics = new DefaultMetricsCollector();
        metrics.RecordFlush(TimeSpan.FromMilliseconds(50));
        metrics.RecordMerge(TimeSpan.FromMilliseconds(100), 3);
        metrics.RecordCommit(TimeSpan.FromMilliseconds(30));

        var snap = metrics.GetSnapshot();
        Assert.Equal(1, snap.FlushCount);
        Assert.Equal(50, snap.FlushTotalMs);
        Assert.Equal(1, snap.MergeCount);
        Assert.Equal(3, snap.MergeSegments);
        Assert.Equal(1, snap.CommitCount);
    }

    [Fact]
    public void LatencyHistogram_DistributesSamples()
    {
        var metrics = new DefaultMetricsCollector();
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(0.5)); // bucket 0 (<1ms)
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(3));   // bucket 1 (<5ms)
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(7));   // bucket 2 (<10ms)
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(30));  // bucket 3 (<50ms)
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(2000));// bucket 7 (≥1000ms)

        var snap = metrics.GetSnapshot();
        Assert.NotNull(snap.LatencyHistogram);
        Assert.Equal(8, snap.LatencyHistogram.Length);
        Assert.True(snap.LatencyHistogram[7] >= 1); // ≥1000ms bucket
    }

    [Fact]
    public void NullMetricsCollector_IsNoOp()
    {
        var metrics = NullMetricsCollector.Instance;
        metrics.RecordSearchLatency(TimeSpan.FromMilliseconds(10));
        metrics.RecordCacheHit();

        var snap = metrics.GetSnapshot();
        Assert.Equal(0, snap.SearchCount);
    }

    [Fact]
    public void IndexSearcher_RecordsMetrics_WhenConfigured()
    {
        var metrics = new DefaultMetricsCollector();

        using var writer = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello world test metrics"));
        writer.AddDocument(doc);
        writer.Commit();
        writer.Dispose();

        var config = new IndexSearcherConfig { Metrics = metrics };
        using var searcher = new IndexSearcher(new MMapDirectory(_dir), config);

        searcher.Search(new TermQuery("body", "hello"), 10);
        searcher.Search(new TermQuery("body", "world"), 10);

        var snap = metrics.GetSnapshot();
        Assert.Equal(2, snap.SearchCount);
        Assert.True(snap.SearchTotalMs >= 0);
    }

    [Fact]
    public void IndexWriter_RecordsCommitMetrics()
    {
        var metrics = new DefaultMetricsCollector();
        var config = new IndexWriterConfig { Metrics = metrics };

        using var writer = new IndexWriter(new MMapDirectory(_dir), config);
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "commit metrics test"));
        writer.AddDocument(doc);
        writer.Commit();

        var snap = metrics.GetSnapshot();
        Assert.Equal(1, snap.CommitCount);
        Assert.True(snap.CommitTotalMs >= 0);
    }
}
