using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.StringField;
using LeanTextField = Rowles.LeanLucene.Document.TextField;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures BooleanQuery performance: +term1 +term2 intersection on 2K docs.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class BooleanQueryBenchmarks
{
    private const int TopN = 25;

    [Params(2_000)]
    public int DocumentCount { get; set; }

    private string _leanIndexPath = string.Empty;
    private LeanMMapDirectory? _leanDirectory;
    private IndexSearcher? _leanSearcher;

    [GlobalSetup]
    public void Setup()
    {
        var documents = BenchmarkData.BuildDocuments(DocumentCount);
        _leanIndexPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-bool-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_leanIndexPath);

        _leanDirectory = new LeanMMapDirectory(_leanIndexPath);
        using (var writer = new IndexWriter(
            _leanDirectory,
            new IndexWriterConfig { MaxBufferedDocs = 512, RamBufferSizeMB = 64 }))
        {
            for (int i = 0; i < documents.Length; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                doc.Add(new LeanTextField("body", documents[i]));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        _leanSearcher = new IndexSearcher(_leanDirectory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _leanSearcher?.Dispose();
        if (!string.IsNullOrWhiteSpace(_leanIndexPath) && Directory.Exists(_leanIndexPath))
            Directory.Delete(_leanIndexPath, recursive: true);
    }

    [Benchmark]
    public int Must_TwoTerms()
    {
        var bq = new BooleanQuery();
        bq.Add(new TermQuery("body", "search"), Occur.Must);
        bq.Add(new TermQuery("body", "benchmark"), Occur.Must);
        var topDocs = _leanSearcher!.Search(bq, TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int Should_TwoTerms()
    {
        var bq = new BooleanQuery();
        bq.Add(new TermQuery("body", "search"), Occur.Should);
        bq.Add(new TermQuery("body", "vector"), Occur.Should);
        var topDocs = _leanSearcher!.Search(bq, TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int Must_MustNot()
    {
        var bq = new BooleanQuery();
        bq.Add(new TermQuery("body", "benchmark"), Occur.Must);
        bq.Add(new TermQuery("body", "vector"), Occur.MustNot);
        var topDocs = _leanSearcher!.Search(bq, TopN);
        return topDocs.TotalHits;
    }
}
