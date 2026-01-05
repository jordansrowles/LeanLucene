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
/// Measures PhraseQuery performance: exact phrase matching on 2K docs.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[SimpleJob]
public class PhraseQueryBenchmarks
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
        _leanIndexPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-phrase-{Guid.NewGuid():N}");
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
    public int ExactPhrase_TwoWords()
    {
        var query = new PhraseQuery("body", "search", "benchmark");
        var topDocs = _leanSearcher!.Search(query, TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int ExactPhrase_ThreeWords()
    {
        var query = new PhraseQuery("body", "segment", "index", "bm25");
        var topDocs = _leanSearcher!.Search(query, TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int SlopPhrase_TwoWords()
    {
        var query = new PhraseQuery("body", slop: 2, "search", "latency");
        var topDocs = _leanSearcher!.Search(query, TopN);
        return topDocs.TotalHits;
    }
}
