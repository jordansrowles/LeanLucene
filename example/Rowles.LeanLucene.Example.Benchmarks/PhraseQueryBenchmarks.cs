using BenchmarkDotNet.Attributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using IODirectory = System.IO.Directory;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanIndexSearcher = Rowles.LeanLucene.Search.IndexSearcher;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.StringField;
using LeanTextField = Rowles.LeanLucene.Document.TextField;
using LuceneIndexSearcher = Lucene.Net.Search.IndexSearcher;
using LuceneStringField = Lucene.Net.Documents.StringField;
using LuceneTextField = Lucene.Net.Documents.TextField;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures PhraseQuery performance: exact and slop phrase matching.
/// Compares LeanLucene vs Lucene.NET (Lifti lacks first-class phrase support).
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[SimpleJob]
public class PhraseQueryBenchmarks
{
    private const int TopN = 25;

    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(2_000);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private string _leanIndexPath = string.Empty;
    private LeanMMapDirectory? _leanDirectory;
    private LeanIndexSearcher? _leanSearcher;

    private RAMDirectory? _luceneDirectory;
    private StandardAnalyzer? _luceneAnalyzer;
    private DirectoryReader? _luceneReader;
    private LuceneIndexSearcher? _luceneSearcher;

    [GlobalSetup]
    public void Setup()
    {
        var documents = BenchmarkData.BuildDocuments(DocumentCount);
        BuildLeanIndex(documents);
        BuildLuceneIndex(documents);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _leanSearcher?.Dispose();
        if (!string.IsNullOrWhiteSpace(_leanIndexPath) && IODirectory.Exists(_leanIndexPath))
            IODirectory.Delete(_leanIndexPath, recursive: true);

        _luceneReader?.Dispose();
        _luceneAnalyzer?.Dispose();
        _luceneDirectory?.Dispose();
    }

    // --- Exact phrase (2 words) ---

    [Benchmark(Baseline = true)]
    public int LeanLucene_ExactPhrase_TwoWords()
    {
        var query = new Rowles.LeanLucene.Search.PhraseQuery("body", "search", "benchmark");
        return _leanSearcher!.Search(query, TopN).TotalHits;
    }

    [Benchmark]
    public int LuceneNet_ExactPhrase_TwoWords()
    {
        var pq = new Lucene.Net.Search.PhraseQuery();
        pq.Add(new Term("body", "search"));
        pq.Add(new Term("body", "benchmark"));
        return _luceneSearcher!.Search(pq, TopN).TotalHits;
    }

    // --- Exact phrase (3 words) ---

    [Benchmark]
    public int LeanLucene_ExactPhrase_ThreeWords()
    {
        var query = new Rowles.LeanLucene.Search.PhraseQuery("body", "segment", "index", "bm25");
        return _leanSearcher!.Search(query, TopN).TotalHits;
    }

    [Benchmark]
    public int LuceneNet_ExactPhrase_ThreeWords()
    {
        var pq = new Lucene.Net.Search.PhraseQuery();
        pq.Add(new Term("body", "segment"));
        pq.Add(new Term("body", "index"));
        pq.Add(new Term("body", "bm25"));
        return _luceneSearcher!.Search(pq, TopN).TotalHits;
    }

    // --- Slop phrase ---

    [Benchmark]
    public int LeanLucene_SlopPhrase_TwoWords()
    {
        var query = new Rowles.LeanLucene.Search.PhraseQuery("body", slop: 2, "search", "latency");
        return _leanSearcher!.Search(query, TopN).TotalHits;
    }

    [Benchmark]
    public int LuceneNet_SlopPhrase_TwoWords()
    {
        var pq = new Lucene.Net.Search.PhraseQuery { Slop = 2 };
        pq.Add(new Term("body", "search"));
        pq.Add(new Term("body", "latency"));
        return _luceneSearcher!.Search(pq, TopN).TotalHits;
    }

    // --- Index builders ---

    private void BuildLeanIndex(string[] documents)
    {
        _leanIndexPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-phrase-{Guid.NewGuid():N}");
        IODirectory.CreateDirectory(_leanIndexPath);

        _leanDirectory = new LeanMMapDirectory(_leanIndexPath);
        using (var writer = new Rowles.LeanLucene.Index.IndexWriter(
            _leanDirectory,
            new Rowles.LeanLucene.Index.IndexWriterConfig { MaxBufferedDocs = 512, RamBufferSizeMB = 64 }))
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

        _leanSearcher = new LeanIndexSearcher(_leanDirectory);
    }

    private void BuildLuceneIndex(string[] documents)
    {
        _luceneDirectory = new RAMDirectory();
        _luceneAnalyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        using (var writer = new Lucene.Net.Index.IndexWriter(
            _luceneDirectory,
            new Lucene.Net.Index.IndexWriterConfig(LuceneVersion.LUCENE_48, _luceneAnalyzer)))
        {
            for (int i = 0; i < documents.Length; i++)
            {
                var doc = new Lucene.Net.Documents.Document
                {
                    new LuceneStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture), Field.Store.NO),
                    new LuceneTextField("body", documents[i], Field.Store.NO)
                };
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        _luceneReader = DirectoryReader.Open(_luceneDirectory);
        _luceneSearcher = new LuceneIndexSearcher(_luceneReader);
    }
}
