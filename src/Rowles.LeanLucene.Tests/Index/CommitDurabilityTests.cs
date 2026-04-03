using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Index;

/// <summary>
/// Regression tests for C5: durable atomic commit.
/// Verifies that <see cref="IndexWriterConfig.DurableCommits"/> ensures committed
/// data round-trips intact through a writer restart, and that disabling the flag
/// does not regress correctness for the happy path.
/// </summary>
[Trait("Category", "Index")]
public class CommitDurabilityTests : IDisposable
{
    private readonly string _dir;

    public CommitDurabilityTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ll_durable_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void DurableCommits_DefaultsToTrue()
    {
        Assert.True(new IndexWriterConfig().DurableCommits);
    }

    [Fact]
    public void DurableCommit_RoundTrip_PreservesAllDocuments()
    {
        // Arrange — write three commits with durability ON
        var config = new IndexWriterConfig { DurableCommits = true };
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            for (int i = 0; i < 3; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("body", $"payload number {i}"));
                writer.AddDocument(doc);
                writer.Commit();
            }
        }

        // Act — re-open and search
        using var searcher = new IndexSearcher(new MMapDirectory(_dir));

        // Assert — every committed document survives the writer restart
        for (int i = 0; i < 3; i++)
        {
            var results = searcher.Search(new TermQuery("body", $"{i}"), 10);
            Assert.Equal(1, results.TotalHits);
        }
    }

    [Fact]
    public void DurableCommit_AllSegmentFilesPresentAfterDispose()
    {
        // Arrange — durable commit must leave a fully readable segments_N + every referenced segment file on disk
        var config = new IndexWriterConfig { DurableCommits = true };
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            for (int i = 0; i < 5; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("body", $"durable {i}"));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        // Assert — at least one segments_N file exists and every .seg referenced by it is present and non-empty
        var segmentsFiles = Directory.GetFiles(_dir, "segments_*");
        Assert.NotEmpty(segmentsFiles);

        var segFiles = Directory.GetFiles(_dir, "*.seg");
        Assert.NotEmpty(segFiles);
        foreach (var seg in segFiles)
            Assert.True(new FileInfo(seg).Length > 0, $"Segment file {seg} is empty");
    }

    [Fact]
    public void DurableCommitsDisabled_StillWorks()
    {
        // Arrange — ensure the opt-out path remains functional
        var config = new IndexWriterConfig { DurableCommits = false };
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "non-durable but valid"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        var results = searcher.Search(new TermQuery("body", "valid"), 10);
        Assert.Equal(1, results.TotalHits);
    }
}
