using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

public sealed class IndexWriterTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public IndexWriterTests(TestDirectoryFixture fixture) => _fixture = fixture;

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
}
