using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
using Rowles.LeanLucene.Search.Scoring;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Index;

/// <summary>
/// Tests for <see cref="IndexStats"/> persistence: write at commit time, load at searcher construction.
/// </summary>
public sealed class IndexStatsPersistenceTests : IDisposable
{
    private readonly string _dir;

    public IndexStatsPersistenceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ll_stats_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Commit_WritesStatsFile()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.AddDocument(CreateDoc("hello world"));
            writer.Commit();
        }

        var statsPath = IndexStats.GetStatsPath(_dir, 1);
        Assert.True(File.Exists(statsPath), "stats_1.json should exist after commit");
    }

    [Fact]
    public void PersistedStats_MatchRecomputedStats()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.AddDocument(CreateDoc("the quick brown fox"));
            writer.AddDocument(CreateDoc("jumped over the lazy dog"));
            writer.AddDocument(CreateDoc("hello"));
            writer.Commit();
        }

        var persisted = IndexStats.TryLoadFrom(IndexStats.GetStatsPath(_dir, 1));
        Assert.NotNull(persisted);

        using var searcher = new IndexSearcher(dir);
        var computed = searcher.Stats;

        Assert.Equal(computed.TotalDocCount, persisted.TotalDocCount);
        Assert.Equal(computed.LiveDocCount, persisted.LiveDocCount);
    }

    [Fact]
    public void PersistedStats_AfterDeletion_UpdatesLiveDocCount()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.AddDocument(CreateDoc("alpha"));
            writer.AddDocument(CreateDoc("beta"));
            writer.AddDocument(CreateDoc("gamma"));
            writer.Commit();

            writer.DeleteDocuments(new TermQuery("body", "alpha"));
            writer.Commit();
        }

        var stats = IndexStats.TryLoadFrom(IndexStats.GetStatsPath(_dir, 2));
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalDocCount);
        Assert.Equal(2, stats.LiveDocCount);
    }

    [Fact]
    public void CommitStats_UsesPerSegmentStatsWithoutOpeningOldSegmentData()
    {
        var dir = new MMapDirectory(_dir);
        using var writer = new IndexWriter(dir, new IndexWriterConfig
        {
            MaxBufferedDocs = 100,
            MergeThreshold = 100,
        });

        writer.AddDocument(CreateDoc("alpha"));
        writer.Commit();

        var firstSegmentStatsPath = SegmentStats.GetStatsPath(_dir, "seg_0");
        Assert.True(File.Exists(firstSegmentStatsPath));

        File.Delete(Path.Combine(_dir, "seg_0.dic"));
        File.Delete(Path.Combine(_dir, "seg_0.pos"));

        writer.AddDocument(CreateDoc("beta"));
        writer.Commit();

        var stats = IndexStats.TryLoadFrom(IndexStats.GetStatsPath(_dir, 2));
        Assert.NotNull(stats);
        Assert.Equal(2, stats.TotalDocCount);
        Assert.Equal(2, stats.LiveDocCount);
    }

    [Fact]
    public void MissingStatsFile_FallsBackToRecomputation()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.AddDocument(CreateDoc("test document"));
            writer.Commit();
        }

        File.Delete(IndexStats.GetStatsPath(_dir, 1));

        using var searcher = new IndexSearcher(dir);
        Assert.Equal(1, searcher.Stats.TotalDocCount);
        Assert.Equal(1, searcher.Stats.LiveDocCount);
    }

    [Fact]
    public void CorruptStatsFile_FallsBackToRecomputation()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.AddDocument(CreateDoc("test"));
            writer.Commit();
        }

        File.WriteAllText(IndexStats.GetStatsPath(_dir, 1), "NOT VALID JSON{{{");

        using var searcher = new IndexSearcher(dir);
        Assert.Equal(1, searcher.Stats.TotalDocCount);
    }

    [Fact]
    public void StatsFile_RoundTrip_PreservesFieldData()
    {
        var avgLengths = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["title"] = 5.2f,
            ["body"] = 142.7f,
        };
        var docCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["title"] = 9800,
            ["body"] = 9500,
        };
        var original = new IndexStats(10000, 9800, avgLengths, docCounts);

        var path = Path.Combine(_dir, "test_stats.json");
        original.WriteTo(path);

        var loaded = IndexStats.TryLoadFrom(path);
        Assert.NotNull(loaded);
        Assert.Equal(10000, loaded.TotalDocCount);
        Assert.Equal(9800, loaded.LiveDocCount);
        Assert.Equal(5.2f, loaded.GetAvgFieldLength("title"));
        Assert.Equal(142.7f, loaded.GetAvgFieldLength("body"));
        Assert.Equal(9800, loaded.GetFieldDocCount("title"));
        Assert.Equal(9500, loaded.GetFieldDocCount("body"));
    }

    [Fact]
    public void OldStatsFiles_PrunedByDeletionPolicy()
    {
        var dir = new MMapDirectory(_dir);
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        writer.AddDocument(CreateDoc("first"));
        writer.Commit();

        writer.AddDocument(CreateDoc("second"));
        writer.Commit();

        Assert.False(File.Exists(IndexStats.GetStatsPath(_dir, 1)),
            "stats_1.json should be deleted by KeepLatestCommitPolicy");
        Assert.True(File.Exists(IndexStats.GetStatsPath(_dir, 2)),
            "stats_2.json should remain");
    }

    [Fact]
    public void EmptyIndex_ReturnsEmptyStats()
    {
        var dir = new MMapDirectory(_dir);
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            writer.Commit();
        }

        var stats = IndexStats.TryLoadFrom(IndexStats.GetStatsPath(_dir, 1));
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalDocCount);
        Assert.Equal(0, stats.LiveDocCount);
    }

    private static LeanDocument CreateDoc(string bodyText)
    {
        var doc = new LeanDocument();
        doc.Add(new TextField("body", bodyText));
        return doc;
    }
}
