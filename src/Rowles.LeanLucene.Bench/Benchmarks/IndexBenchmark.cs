using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Bench.Benchmarks;

[SimpleJob(RuntimeMoniker.Net11_0)]
[MemoryDiagnoser]
public class IndexBenchmark
{
    private string _indexDir = null!;

    [Params(1_000, 10_000)]
    public int DocCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _indexDir = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_indexDir);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_indexDir))
            Directory.Delete(_indexDir, recursive: true);
    }

    [Benchmark(Description = "Index N documents")]
    public void IndexDocuments()
    {
        var path = Path.Combine(_indexDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var dir = new MMapDirectory(path);

        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        for (var i = 0; i < DocCount; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("title", $"Document number {i}"));
            doc.Add(new TextField("body", $"This is the body content for document {i} with some searchable text"));
            doc.Add(new StringField("id", i.ToString()));
            writer.AddDocument(doc);
        }
        writer.Commit();
    }

    [Benchmark(Description = "Index + search round-trip")]
    public int IndexAndSearch()
    {
        var path = Path.Combine(_indexDir, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var dir = new MMapDirectory(path);

        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        for (var i = 0; i < DocCount; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("title", $"Document number {i}"));
            doc.Add(new TextField("body", $"Searchable body content for document {i}"));
            doc.Add(new StringField("id", i.ToString()));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var results = searcher.Search(new TermQuery("body", "searchable"), 10);
        return results.TotalHits;
    }
}
