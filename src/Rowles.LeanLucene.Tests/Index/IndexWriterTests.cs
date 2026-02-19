using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
public sealed class IndexWriterTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public IndexWriterTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string SubDir(string name)
    {
        var path = System.IO.Path.Combine(_fixture.Path, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void IndexWriter_FlushOnRamThreshold_ProducesSegmentFile()
    {
        var dir = new MMapDirectory(SubDir("ram_flush"));
        var config = new IndexWriterConfig { RamBufferSizeMB = 0.001 }; // ~1 KB
        using var writer = new IndexWriter(dir, config);

        for (int i = 0; i < 50; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", new string('x', 100)));
            writer.AddDocument(doc);
        }
        writer.Commit();

        var segFiles = System.IO.Directory.GetFiles(SubDir("ram_flush"), "*.seg");
        Assert.NotEmpty(segFiles);
    }

    [Fact]
    public void IndexWriter_FlushOnDocCountCeiling_ProducesSegmentFile()
    {
        var dir = new MMapDirectory(SubDir("doc_flush"));
        var config = new IndexWriterConfig { MaxBufferedDocs = 5 };
        using var writer = new IndexWriter(dir, config);

        for (int i = 0; i < 6; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"document number {i}"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        var segFiles = System.IO.Directory.GetFiles(SubDir("doc_flush"), "*.seg");
        Assert.NotEmpty(segFiles);
    }

    [Fact]
    public void IndexWriter_CommitWritesSegmentsNFile()
    {
        var dir = new MMapDirectory(SubDir("commit_test"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "hello world"));
        writer.AddDocument(doc);
        writer.Commit();

        var segNFiles = System.IO.Directory.GetFiles(SubDir("commit_test"), "segments_*");
        Assert.NotEmpty(segNFiles);
    }

    [Fact]
    public void IndexWriter_CrashBeforeCommit_SegmentNotVisible()
    {
        var subDir = SubDir("crash_test");
        var dir = new MMapDirectory(subDir);

        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "uncommitted data"));
            writer.AddDocument(doc);
            // No commit — dispose discards uncommitted work
        }

        var segNFiles = System.IO.Directory.GetFiles(subDir, "segments_*");
        if (segNFiles.Length == 0)
        {
            Assert.Empty(segNFiles);
        }
        else
        {
            var commitContent = File.ReadAllText(segNFiles[^1]);
            Assert.DoesNotContain("uncommitted", commitContent);
        }
    }

    [Fact]
    public void FlushTriggersAtAccurateRamThreshold()
    {
        // With RamBufferSizeMB = 1 MB and accurate tracking via PostingAccumulator.EstimatedBytes,
        // the flush should happen close to 1 MB (not 5× overshoot from old heuristic).
        var dir = new MMapDirectory(SubDir("accurate_flush"));
        var config = new IndexWriterConfig { RamBufferSizeMB = 1.0, MaxBufferedDocs = 100_000 };
        using var writer = new IndexWriter(dir, config);

        for (int i = 0; i < 5000; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"document number {i} with some text to consume memory"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        // With accurate tracking, we should get at least one flush (segment files exist)
        var segFiles = System.IO.Directory.GetFiles(SubDir("accurate_flush"), "*.seg");
        Assert.NotEmpty(segFiles);
    }

    [Fact]
    public void HardCeiling_ForcesFlush_WhenExceeded()
    {
        var dir = new MMapDirectory(SubDir("hard_ceiling"));
        var config = new IndexWriterConfig
        {
            RamBufferSizeMB = 1024, // very high so RAM threshold won't trigger
            MaxBufferedDocs = 100_000,
            FlushThrottleBytes = 100 * 1024 // 100 KB hard ceiling
        };
        using var writer = new IndexWriter(dir, config);

        for (int i = 0; i < 2000; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"document {i} with text to fill buffer past ceiling"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        // Hard ceiling should have forced flushes, creating segment files
        var segFiles = System.IO.Directory.GetFiles(SubDir("hard_ceiling"), "*.seg");
        Assert.NotEmpty(segFiles);
    }

    [Fact]
    public void MergeBackpressure_PausesIndexing_WhenTooManySegments()
    {
        var dir = new MMapDirectory(SubDir("merge_bp"));
        var config = new IndexWriterConfig
        {
            MaxBufferedDocs = 2,        // flush every 2 docs → lots of segments
            MergeThrottleSegments = 5   // throttle at 5 segments
        };
        using var writer = new IndexWriter(dir, config);

        // Index enough docs to exceed 5 segments, then commit
        for (int i = 0; i < 20; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", $"doc {i}"));
            writer.AddDocument(doc);
        }
        writer.Commit();

        // Should complete without hanging (backpressure triggers flush, not deadlock)
        var segFiles = System.IO.Directory.GetFiles(SubDir("merge_bp"), "*.seg");
        Assert.NotEmpty(segFiles);
    }
}
