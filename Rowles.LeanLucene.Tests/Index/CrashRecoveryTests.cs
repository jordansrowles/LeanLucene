using System.Text.Json;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
public class CrashRecoveryTests : IDisposable
{
    private readonly string _dir;

    public CrashRecoveryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"ll_recovery_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void EmptyDirectory_StartsCleanIndex()
    {
        // Arrange — empty directory, no commit files
        var config = new IndexWriterConfig();
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello world"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Assert — index works
        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        var results = searcher.Search(new TermQuery("body", "hello"), 10);
        Assert.Equal(1, results.TotalHits);
    }

    [Fact]
    public void CorruptLatestCommit_FallsBackToPreviousGeneration()
    {
        // Arrange — create 2 valid commits, keeping both generations
        var config = new IndexWriterConfig
        {
            DeletionPolicy = new KeepLastNCommitsPolicy(2)
        };
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc1 = new LeanDocument();
            doc1.Add(new TextField("body", "first commit"));
            writer.AddDocument(doc1);
            writer.Commit(); // segments_1

            var doc2 = new LeanDocument();
            doc2.Add(new TextField("body", "second commit"));
            writer.AddDocument(doc2);
            writer.Commit(); // segments_2
        }

        // Corrupt segments_2
        var segments2 = Path.Combine(_dir, "segments_2");
        Assert.True(File.Exists(segments2));
        File.WriteAllText(segments2, "NOT_VALID_JSON{{{");

        // Act — re-open should fall back to segments_1
        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        var results = searcher.Search(new TermQuery("body", "first"), 10);
        Assert.Equal(1, results.TotalHits);
    }

    [Fact]
    public void OrphanedSegmentFiles_CleanedUpOnStartup()
    {
        // Arrange — create an index with 1 commit
        var config = new IndexWriterConfig();
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "real segment"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Plant orphaned segment files (segment not referenced by any commit)
        var orphanId = "orphan_99";
        File.WriteAllText(Path.Combine(_dir, orphanId + ".seg"), "fake");
        File.WriteAllText(Path.Combine(_dir, orphanId + ".dic"), "fake");
        File.WriteAllText(Path.Combine(_dir, orphanId + ".pos"), "fake");

        // Act — re-open triggers recovery which cleans orphans
        using var writer2 = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());

        // Assert — orphaned files removed
        Assert.False(File.Exists(Path.Combine(_dir, orphanId + ".seg")));
        Assert.False(File.Exists(Path.Combine(_dir, orphanId + ".dic")));
        Assert.False(File.Exists(Path.Combine(_dir, orphanId + ".pos")));
    }

    [Fact]
    public void TempFiles_CleanedUpOnStartup()
    {
        // Arrange — create an index, then leave temp files behind
        var config = new IndexWriterConfig();
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "data"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Simulate interrupted commit temp files
        File.WriteAllText(Path.Combine(_dir, "segments_99.tmp"), "partial");
        File.WriteAllText(Path.Combine(_dir, "data.tmp"), "partial");

        // Act
        using var writer2 = new IndexWriter(new MMapDirectory(_dir), new IndexWriterConfig());

        // Assert — temp files removed
        Assert.False(File.Exists(Path.Combine(_dir, "segments_99.tmp")));
        Assert.False(File.Exists(Path.Combine(_dir, "data.tmp")));
    }

    [Fact]
    public void LatestCommitMissingSegment_FallsBackToPreviousGeneration()
    {
        // Arrange — create 2 commits, keeping both generations
        var config = new IndexWriterConfig
        {
            DeletionPolicy = new KeepLastNCommitsPolicy(2)
        };
        using (var writer = new IndexWriter(new MMapDirectory(_dir), config))
        {
            var doc1 = new LeanDocument();
            doc1.Add(new TextField("body", "first commit"));
            writer.AddDocument(doc1);
            writer.Commit(); // segments_1

            var doc2 = new LeanDocument();
            doc2.Add(new TextField("body", "second commit extra"));
            writer.AddDocument(doc2);
            writer.Commit(); // segments_2
        }

        // Read segments_2 to find its segment IDs, then delete one segment file
        var segments2 = Path.Combine(_dir, "segments_2");
        var json = File.ReadAllText(segments2);
        var commit = JsonSerializer.Deserialize<JsonElement>(json);
        var segments = commit.GetProperty("Segments");
        var lastSeg = segments[segments.GetArrayLength() - 1].GetString()!;
        File.Delete(Path.Combine(_dir, lastSeg + ".seg"));

        // Act — should fall back to segments_1
        using var searcher = new IndexSearcher(new MMapDirectory(_dir));
        var results = searcher.Search(new TermQuery("body", "first"), 10);
        Assert.Equal(1, results.TotalHits);
    }

    [Fact]
    public void RecoveryResult_Null_ForEmptyDirectory()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), $"ll_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try
        {
            var result = IndexRecovery.RecoverLatestCommit(emptyDir);
            Assert.Null(result);
        }
        finally
        {
            try { Directory.Delete(emptyDir, true); } catch { }
        }
    }
}
