using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Tests.Codecs;

public class FieldCompressionTests : IDisposable
{
    private readonly string _baseDir;

    public FieldCompressionTests()
    {
        _baseDir = Path.Combine(Path.GetTempPath(), "compress_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_baseDir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_baseDir)) Directory.Delete(_baseDir, true); }
        catch { }
    }

    private string SubDir(string name)
    {
        var d = Path.Combine(_baseDir, name);
        Directory.CreateDirectory(d);
        return d;
    }

    private void IndexDocs(string dir, IndexWriterConfig config, int count = 50)
    {
        using var writer = new IndexWriter(new MMapDirectory(dir), config);
        for (int i = 0; i < count; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body",
                "the quick brown fox jumps over the lazy dog " +
                "performance benchmarks allocation profiling memory " + i));
            doc.Add(new StringField("id", i.ToString()));
            writer.AddDocument(doc);
        }
        writer.Commit();
    }

    [Fact]
    public void Policy_None_ProducesLargestFiles()
    {
        var dirNone = SubDir("none");
        IndexDocs(dirNone, new IndexWriterConfig { CompressionPolicy = FieldCompressionPolicy.None });

        var dirFast = SubDir("fast");
        IndexDocs(dirFast, new IndexWriterConfig { CompressionPolicy = FieldCompressionPolicy.Fast });

        long sizeNone = GetFdtSize(dirNone);
        long sizeFast = GetFdtSize(dirFast);

        Assert.True(sizeNone >= sizeFast, $"None ({sizeNone}) should be >= Fast ({sizeFast})");
    }

    [Fact]
    public void Policy_High_ProducesSmallerThanFast()
    {
        var dirFast = SubDir("fast2");
        IndexDocs(dirFast, new IndexWriterConfig { CompressionPolicy = FieldCompressionPolicy.Fast }, count: 200);

        var dirHigh = SubDir("high");
        IndexDocs(dirHigh, new IndexWriterConfig { CompressionPolicy = FieldCompressionPolicy.High }, count: 200);

        long sizeFast = GetFdtSize(dirFast);
        long sizeHigh = GetFdtSize(dirHigh);

        // High should be <= Fast (may be equal for very small data)
        Assert.True(sizeHigh <= sizeFast, $"High ({sizeHigh}) should be <= Fast ({sizeFast})");
    }

    [Fact]
    public void AllPolicies_RoundTrip_Correctly()
    {
        foreach (var policy in Enum.GetValues<FieldCompressionPolicy>())
        {
            var dir = SubDir($"roundtrip_{policy}");
            IndexDocs(dir, new IndexWriterConfig { CompressionPolicy = policy }, count: 10);

            using var searcher = new IndexSearcher(new MMapDirectory(dir));
            var results = searcher.Search(new TermQuery("body", "fox"), 100);
            Assert.Equal(10, results.TotalHits);

            // Verify stored fields round-trip
            var stored = searcher.GetStoredFields(results.ScoreDocs[0].DocId);
            Assert.True(stored.ContainsKey("body"));
        }
    }

    [Fact]
    public void CompressionPolicy_SetsCompressionLevel()
    {
        var config = new IndexWriterConfig { CompressionPolicy = FieldCompressionPolicy.None };
        Assert.Equal(System.IO.Compression.CompressionLevel.NoCompression, config.StoredFieldCompressionLevel);

        config.CompressionPolicy = FieldCompressionPolicy.Fast;
        Assert.Equal(System.IO.Compression.CompressionLevel.Fastest, config.StoredFieldCompressionLevel);

        config.CompressionPolicy = FieldCompressionPolicy.High;
        Assert.Equal(System.IO.Compression.CompressionLevel.Optimal, config.StoredFieldCompressionLevel);
    }

    [Fact]
    public void DefaultConfig_HasNoCompressionPolicy()
    {
        var config = new IndexWriterConfig();
        Assert.Null(config.CompressionPolicy);
        Assert.Equal(System.IO.Compression.CompressionLevel.Fastest, config.StoredFieldCompressionLevel);
    }

    private static long GetFdtSize(string dir)
    {
        return Directory.GetFiles(dir, "*.fdt")
            .Sum(f => new FileInfo(f).Length);
    }
}
