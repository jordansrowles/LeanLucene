using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Search;

public sealed class SearcherManagerTests : IDisposable
{
    private readonly string _dir;

    public SearcherManagerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ll_smgr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Acquire_ReturnsUsableSearcher()
    {
        var dir = new MMapDirectory(_dir);
        using (var w = new IndexWriter(dir, new IndexWriterConfig()))
        {
            w.AddDocument(Doc("hello world"));
            w.Commit();
        }

        using var mgr = new SearcherManager(dir);
        var searcher = mgr.Acquire();
        try
        {
            var results = searcher.Search(new TermQuery("body", "hello"), 10);
            Assert.Equal(1, results.TotalHits);
        }
        finally
        {
            mgr.Release(searcher);
        }
    }

    [Fact]
    public void UsingSearcher_ConveniencePattern()
    {
        var dir = new MMapDirectory(_dir);
        using (var w = new IndexWriter(dir, new IndexWriterConfig()))
        {
            w.AddDocument(Doc("test"));
            w.Commit();
        }

        using var mgr = new SearcherManager(dir);
        var hits = mgr.UsingSearcher(s => s.Search(new TermQuery("body", "test"), 10).TotalHits);
        Assert.Equal(1, hits);
    }

    [Fact]
    public void MaybeRefresh_DetectsNewCommit()
    {
        var dir = new MMapDirectory(_dir);
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        writer.AddDocument(Doc("first"));
        writer.Commit();

        using var mgr = new SearcherManager(dir);
        var before = mgr.UsingSearcher(s => s.Stats.TotalDocCount);

        writer.AddDocument(Doc("second"));
        writer.Commit();

        bool refreshed = mgr.MaybeRefresh();
        Assert.True(refreshed);

        var after = mgr.UsingSearcher(s => s.Stats.TotalDocCount);
        Assert.Equal(2, after);
    }

    [Fact]
    public void MaybeRefresh_ReturnsFalse_WhenNoNewCommit()
    {
        var dir = new MMapDirectory(_dir);
        using (var w = new IndexWriter(dir, new IndexWriterConfig()))
        {
            w.AddDocument(Doc("test"));
            w.Commit();
        }

        using var mgr = new SearcherManager(dir);
        bool refreshed = mgr.MaybeRefresh();
        Assert.False(refreshed);
    }

    [Fact]
    public async Task MaybeRefreshAsync_Works()
    {
        var dir = new MMapDirectory(_dir);
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        writer.AddDocument(Doc("first"));
        writer.Commit();

        using var mgr = new SearcherManager(dir);

        writer.AddDocument(Doc("second"));
        writer.Commit();

        bool refreshed = await mgr.MaybeRefreshAsync();
        Assert.True(refreshed);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var dir = new MMapDirectory(_dir);
        using (var w = new IndexWriter(dir, new IndexWriterConfig()))
        {
            w.AddDocument(Doc("test"));
            w.Commit();
        }

        var mgr = new SearcherManager(dir);
        mgr.Dispose();
        mgr.Dispose(); // should not throw
    }

    [Fact]
    public void Acquire_AfterDispose_Throws()
    {
        var dir = new MMapDirectory(_dir);
        using (var w = new IndexWriter(dir, new IndexWriterConfig()))
        {
            w.AddDocument(Doc("test"));
            w.Commit();
        }

        var mgr = new SearcherManager(dir);
        mgr.Dispose();

        Assert.Throws<ObjectDisposedException>(() => mgr.Acquire());
    }

    private static LeanDocument Doc(string body)
    {
        var doc = new LeanDocument();
        doc.Add(new TextField("body", body));
        return doc;
    }
}
