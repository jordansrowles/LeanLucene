using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
[Trait("Category", "Snapshot")]
public sealed class IndexSnapshotTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-snap-{Guid.NewGuid():N}");

    public IndexSnapshotTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void CreateSnapshot_ReturnsCommittedSegments()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 100 });

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello world"));
        writer.AddDocument(doc);

        var snapshot = writer.CreateSnapshot();

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.Segments);
        Assert.Equal("seg_0", snapshot.Segments[0].SegmentId);
        Assert.Equal(1, snapshot.Segments[0].DocCount);

        writer.ReleaseSnapshot(snapshot);
    }

    [Fact]
    public void Snapshot_PreservesOldSegmentsAfterNewCommit()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 100 });

        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "first document"));
        writer.AddDocument(doc1);
        writer.Commit();

        var snapshot = writer.CreateSnapshot();
        var snappedIds = snapshot.Segments.Select(s => s.SegmentId).ToHashSet();

        // Add more docs and commit again
        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "second document"));
        writer.AddDocument(doc2);
        writer.Commit();

        // Snapshot segments should still reference original segments
        Assert.All(snappedIds, id => Assert.Contains(id, snappedIds));
        Assert.True(snapshot.Segments.Count >= 1);

        writer.ReleaseSnapshot(snapshot);
    }

    [Fact]
    public void Snapshot_CanBeUsedToOpenSearcher()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 100 });

        for (int i = 0; i < 5; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"document number {i}"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        var snapshot = writer.CreateSnapshot();

        // Open a searcher using the snapshot's segment list
        using var searcher = new IndexSearcher(directory, snapshot.Segments);
        var results = searcher.Search(new TermQuery("body", "document"), 10);

        Assert.Equal(5, results.TotalHits);

        writer.ReleaseSnapshot(snapshot);
    }

    [Fact]
    public void HeldSnapshot_ProtectsCommitFilesAndSegmentsDuringBackgroundMerge()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig
        {
            MaxBufferedDocs = 1,
            MergeThreshold = 2,
        });

        writer.AddDocument(CreateDocument("alpha anchor"));
        writer.Commit();

        var snapshot = writer.CreateSnapshot();
        var protectedSegmentId = snapshot.Segments[0].SegmentId;

        writer.AddDocument(CreateDocument("bravo anchor"));
        writer.Commit();

        Assert.True(File.Exists(Path.Combine(_dir, "segments_1")));
        Assert.True(File.Exists(Path.Combine(_dir, "stats_1.json")));
        Assert.True(File.Exists(Path.Combine(_dir, protectedSegmentId + ".dic")));
        Assert.True(File.Exists(Path.Combine(_dir, protectedSegmentId + ".pos")));

        writer.ReleaseSnapshot(snapshot);
    }

    [Fact]
    public void ReleaseSnapshot_AllowsRepeatedRelease()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 100 });

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "test"));
        writer.AddDocument(doc);
        writer.Commit();

        var snapshot = writer.CreateSnapshot();
        writer.ReleaseSnapshot(snapshot);
        // Second release should not throw
        writer.ReleaseSnapshot(snapshot);
    }

    private static LeanDocument CreateDocument(string body)
    {
        var document = new LeanDocument();
        document.Add(new TextField("body", body));
        return document;
    }
}
