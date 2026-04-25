using System.Collections.Concurrent;
using System.Diagnostics;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Diagnostics;

/// <summary>
/// Verifies that <see cref="LeanLuceneActivitySource"/> emits activities with the expected names
/// and tags for Search, Commit, Flush, and Merge operations.
/// </summary>
public sealed class ActivitySourceTests : IDisposable
{
    private const string SourceName = "Rowles.LeanLucene";

    private readonly string _dir;
    private readonly ConcurrentBag<Activity> _captured = [];
    private readonly ActivityListener _listener;

    public ActivitySourceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "activity_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);

        _listener = new ActivityListener
        {
            ShouldListenTo = src => src.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => _captured.Add(a)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        foreach (var a in _captured) a.Dispose();
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Search_EmitsActivity_WithQueryTypeTag()
    {
        using var writer = CreateAndPopulateIndex();

        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        searcher.Search(new TermQuery("body", "hello"), 5);

        var activity = _captured.FirstOrDefault(a => a.OperationName == "leanlucene.search");
        Assert.NotNull(activity);
        Assert.Equal("TermQuery", activity!.GetTagItem("query.type"));
    }

    [Fact]
    public void Search_EmitsActivity_WithTotalHitsTag()
    {
        using var writer = CreateAndPopulateIndex();

        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        searcher.Search(new TermQuery("body", "hello"), 10);

        var activity = _captured.FirstOrDefault(a => a.OperationName == "leanlucene.search");
        Assert.NotNull(activity);
        Assert.NotNull(activity!.GetTagItem("search.total_hits"));
    }

    [Fact]
    public void Commit_EmitsActivity_WithSegmentCountTag()
    {
        using var writer = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "commit activity test"));
        writer.AddDocument(doc);
        writer.Commit();
        writer.Dispose();

        var activity = _captured.FirstOrDefault(a => a.OperationName == "leanlucene.index.commit");
        Assert.NotNull(activity);
        Assert.NotNull(activity!.GetTagItem("index.segment_count"));
    }

    [Fact]
    public void Flush_EmitsActivity_WithDocCountTag()
    {
        using var writer = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "flush activity test"));
        writer.AddDocument(doc);
        writer.Commit();
        writer.Dispose();

        var activity = _captured.FirstOrDefault(a => a.OperationName == "leanlucene.index.flush");
        Assert.NotNull(activity);
        Assert.NotNull(activity.GetTagItem("index.doc_count"));
    }

    [Fact]
    public void Activities_HaveCorrectSourceName()
    {
        using var writer = CreateAndPopulateIndex();
        writer.Dispose();

        var snapshot = _captured.ToList();
        Assert.NotEmpty(snapshot);
        Assert.All(snapshot, a => Assert.Equal(SourceName, a.Source.Name));
    }

    [Fact]
    public void NoListener_ProducesNoActivities()
    {
        _listener.Dispose();

        var noListenPath = Path.Combine(Path.GetTempPath(), "act_nolisten_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(noListenPath);

        var writer = new IndexWriter(new MMapDirectory(noListenPath), new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "no listener test"));
        writer.AddDocument(doc);
        writer.Commit();
        writer.Dispose();

        try { Directory.Delete(noListenPath, true); } catch { }

        // Activities produced before Dispose of the listener were already captured;
        // the write above happened after Dispose so should add nothing new.
        Assert.DoesNotContain(_captured, a => a.OperationName == "leanlucene.index.flush");
    }

    private IndexWriter CreateAndPopulateIndex()
    {
        var writer = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello world"));
        writer.AddDocument(doc);
        writer.Commit();
        return writer;
    }
}
