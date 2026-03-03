using BenchmarkDotNet.Attributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using LeanTermQuery = Rowles.LeanLucene.Search.Queries.TermQuery;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanIndexSearcher = Rowles.LeanLucene.Search.Searcher.IndexSearcher;
using LeanIndexWriter = Rowles.LeanLucene.Index.Indexer.IndexWriter;
using LeanIndexWriterConfig = Rowles.LeanLucene.Index.Indexer.IndexWriterConfig;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.Fields.StringField;
using LeanTextField = Rowles.LeanLucene.Document.Fields.TextField;
using LuceneIndexSearcher = Lucene.Net.Search.IndexSearcher;
using LuceneStringField = Lucene.Net.Documents.StringField;
using LuceneTextField = Lucene.Net.Documents.TextField;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures search performance on compound vs non-compound indices: LeanLucene and Lucene.NET.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[RPlotExporter]
[KeepBenchmarkFiles]
[SimpleJob]
public class CompoundFileSearchBenchmarks
{
    private const int TopN = 25;

    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(BenchmarkData.DefaultDocCount);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private string _leanNoCompoundPath = string.Empty;
    private string _leanCompoundPath = string.Empty;
    private LeanMMapDirectory? _leanNoCompoundDir;
    private LeanMMapDirectory? _leanCompoundDir;
    private LeanIndexSearcher? _leanNoCompoundSearcher;
    private LeanIndexSearcher? _leanCompoundSearcher;

    private RAMDirectory? _luceneCompoundDirectory;
    private DirectoryReader? _luceneCompoundReader;
    private LuceneIndexSearcher? _luceneCompoundSearcher;

    [GlobalSetup]
    public void Setup()
    {
        var documents = BenchmarkData.BuildDocuments(DocumentCount);
        BuildLeanSearchIndices(documents);
        BuildLuceneNetSearchIndex(documents);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _leanNoCompoundSearcher?.Dispose();
        _leanCompoundSearcher?.Dispose();

        if (!string.IsNullOrWhiteSpace(_leanNoCompoundPath) && System.IO.Directory.Exists(_leanNoCompoundPath))
            System.IO.Directory.Delete(_leanNoCompoundPath, recursive: true);
        if (!string.IsNullOrWhiteSpace(_leanCompoundPath) && System.IO.Directory.Exists(_leanCompoundPath))
            System.IO.Directory.Delete(_leanCompoundPath, recursive: true);

        _luceneCompoundReader?.Dispose();
        _luceneCompoundDirectory?.Dispose();
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LeanLucene_Search_NoCompound()
    {
        var topDocs = _leanNoCompoundSearcher!.Search(new LeanTermQuery("body", "search"), TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LeanLucene_Search_Compound()
    {
        var topDocs = _leanCompoundSearcher!.Search(new LeanTermQuery("body", "search"), TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LuceneNet_Search_Compound()
    {
        var query = new Lucene.Net.Search.TermQuery(new Term("body", "search"));
        var topDocs = _luceneCompoundSearcher!.Search(query, TopN);
        return topDocs.TotalHits;
    }

    private void BuildLeanSearchIndices(string[] documents)
    {
        _leanNoCompoundPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-cfs-nc-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(_leanNoCompoundPath);
        _leanNoCompoundDir = new LeanMMapDirectory(_leanNoCompoundPath);

        using (var writer = new LeanIndexWriter(_leanNoCompoundDir, new LeanIndexWriterConfig
        {
            MaxBufferedDocs = 10_000,
            RamBufferSizeMB = 256,
            UseCompoundFile = false
        }))
        {
            IndexDocuments(writer, documents);
            writer.Commit();
        }
        _leanNoCompoundSearcher = new LeanIndexSearcher(_leanNoCompoundDir);

        _leanCompoundPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-cfs-c-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(_leanCompoundPath);
        _leanCompoundDir = new LeanMMapDirectory(_leanCompoundPath);

        using (var writer = new LeanIndexWriter(_leanCompoundDir, new LeanIndexWriterConfig
        {
            MaxBufferedDocs = 10_000,
            RamBufferSizeMB = 256,
            UseCompoundFile = true
        }))
        {
            IndexDocuments(writer, documents);
            writer.Commit();
        }
        _leanCompoundSearcher = new LeanIndexSearcher(_leanCompoundDir);
    }

    private void BuildLuceneNetSearchIndex(string[] documents)
    {
        _luceneCompoundDirectory = new RAMDirectory();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        using (var writer = new Lucene.Net.Index.IndexWriter(_luceneCompoundDirectory,
            new Lucene.Net.Index.IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                UseCompoundFile = true
            }))
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

        _luceneCompoundReader = DirectoryReader.Open(_luceneCompoundDirectory);
        _luceneCompoundSearcher = new LuceneIndexSearcher(_luceneCompoundReader);
    }

    private static void IndexDocuments(LeanIndexWriter writer, string[] documents)
    {
        for (int i = 0; i < documents.Length; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            doc.Add(new LeanTextField("body", documents[i]));
            writer.AddDocument(doc);
        }
    }
}
