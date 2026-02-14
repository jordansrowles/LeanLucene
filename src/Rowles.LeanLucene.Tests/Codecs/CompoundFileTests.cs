using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Codecs;

public sealed class CompoundFileTests : IClassFixture<TestDirectoryFixture>
{
    private readonly string _path;

    public CompoundFileTests(TestDirectoryFixture fixture) => _path = fixture.Path;

    [Fact]
    public void CompoundFile_IndexAndSearch_RoundTrips()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(CompoundFile_IndexAndSearch_RoundTrips));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig { UseCompoundFile = true };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello world"));
            doc.Add(new StringField("tag", "greeting"));
            writer.AddDocument(doc);

            var doc2 = new LeanDocument();
            doc2.Add(new TextField("body", "goodbye world"));
            doc2.Add(new StringField("tag", "farewell"));
            writer.AddDocument(doc2);

            writer.Commit();
        }

        // Act — search should work against compound file segments
        using var searcher = new IndexSearcher(mmap);
        var hits = searcher.Search(new TermQuery("body", "hello"), 10);

        // Assert
        Assert.Equal(1, hits.TotalHits);
    }

    [Fact]
    public void CompoundFile_CfsFileCreated()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(CompoundFile_CfsFileCreated));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig { UseCompoundFile = true };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "test"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Assert — .cfs should exist
        var cfsFiles = Directory.GetFiles(dir, "*.cfs");
        Assert.NotEmpty(cfsFiles);

        // .seg metadata should still exist
        var segFiles = Directory.GetFiles(dir, "*.seg");
        Assert.NotEmpty(segFiles);
    }

    [Fact]
    public void CompoundFile_SegmentInfoHasFlag()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(CompoundFile_SegmentInfoHasFlag));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig { UseCompoundFile = true };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "test"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act — read the segment info
        var segFile = Directory.GetFiles(dir, "*.seg").First();
        var info = Rowles.LeanLucene.Index.Segment.SegmentInfo.ReadFrom(segFile);

        // Assert
        Assert.True(info.IsCompoundFile);
    }

    [Fact]
    public void NonCompound_StillWorksNormally()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(NonCompound_StillWorksNormally));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig { UseCompoundFile = false };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello world"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act
        using var searcher = new IndexSearcher(mmap);
        var hits = searcher.Search(new TermQuery("body", "hello"), 10);

        // Assert
        Assert.Equal(1, hits.TotalHits);
        Assert.Empty(Directory.GetFiles(dir, "*.cfs")); // no compound files
    }
}
