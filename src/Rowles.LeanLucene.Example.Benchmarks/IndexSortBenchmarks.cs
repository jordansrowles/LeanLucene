using BenchmarkDotNet.Attributes;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Search.Scoring;
using Rowles.LeanLucene.Store;
using LeanDocument = Rowles.LeanLucene.Document.LeanDocument;
using LeanIndexSearcher = Rowles.LeanLucene.Search.Searcher.IndexSearcher;
using LeanMMapDirectory = Rowles.LeanLucene.Store.MMapDirectory;
using LeanStringField = Rowles.LeanLucene.Document.Fields.StringField;
using LeanTextField = Rowles.LeanLucene.Document.Fields.TextField;

namespace Rowles.LeanLucene.Example.Benchmarks;

/// <summary>
/// Measures index-time sort overhead and sorted-search early termination benefit.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[KeepBenchmarkFiles]
[SimpleJob]
public class IndexSortBenchmarks
{
    private const int TopN = 25;

    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(BenchmarkData.DefaultDocCount);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private (string Body, double Price)[] _documentsWithPrices = [];

    // Pre-built indices for search benchmarks
    private string _unsortedPath = string.Empty;
    private string _sortedPath = string.Empty;
    private LeanMMapDirectory? _unsortedDir;
    private LeanMMapDirectory? _sortedDir;
    private LeanIndexSearcher? _unsortedSearcher;
    private LeanIndexSearcher? _sortedSearcher;

    [GlobalSetup]
    public void Setup()
    {
        _documentsWithPrices = BenchmarkData.BuildDocumentsWithPrices(DocumentCount);
        BuildSearchIndices();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _unsortedSearcher?.Dispose();
        _sortedSearcher?.Dispose();

        if (!string.IsNullOrWhiteSpace(_unsortedPath) && Directory.Exists(_unsortedPath))
            Directory.Delete(_unsortedPath, recursive: true);
        if (!string.IsNullOrWhiteSpace(_sortedPath) && Directory.Exists(_sortedPath))
            Directory.Delete(_sortedPath, recursive: true);
    }

    [Benchmark(Baseline = true)]
    public int LeanLucene_Index_Unsorted()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-sort-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using var writer = new IndexWriter(directory, new IndexWriterConfig
            {
                MaxBufferedDocs = 10_000,
                RamBufferSizeMB = 256
            });

            IndexDocuments(writer);
            writer.Commit();
            return _documentsWithPrices.Length;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    [Benchmark]
    public int LeanLucene_Index_Sorted()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-sort-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using var writer = new IndexWriter(directory, new IndexWriterConfig
            {
                MaxBufferedDocs = 10_000,
                RamBufferSizeMB = 256,
                IndexSort = new IndexSort(SortField.Numeric("price"))
            });

            IndexDocuments(writer);
            writer.Commit();
            return _documentsWithPrices.Length;
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    [Benchmark]
    public int LeanLucene_SortedSearch_EarlyTermination()
    {
        // Search on pre-sorted index with matching sort — can early-terminate
        var topDocs = _sortedSearcher!.Search(new TermQuery("body", "product"), TopN, SortField.Numeric("price"));
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int LeanLucene_SortedSearch_PostSort()
    {
        // Search on unsorted index — must post-sort results
        var topDocs = _unsortedSearcher!.Search(new TermQuery("body", "product"), TopN, SortField.Numeric("price"));
        return topDocs.TotalHits;
    }

    private void IndexDocuments(IndexWriter writer)
    {
        for (int i = 0; i < _documentsWithPrices.Length; i++)
        {
            var (body, price) = _documentsWithPrices[i];
            var doc = new LeanDocument();
            doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            doc.Add(new LeanTextField("body", body));
            doc.Add(new NumericField("price", price));
            writer.AddDocument(doc);
        }
    }

    private void BuildSearchIndices()
    {
        _unsortedPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-sort-ns-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_unsortedPath);
        _unsortedDir = new LeanMMapDirectory(_unsortedPath);

        using (var writer = new IndexWriter(_unsortedDir, new IndexWriterConfig
        {
            MaxBufferedDocs = 10_000,
            RamBufferSizeMB = 256
        }))
        {
            IndexDocuments(writer);
            writer.Commit();
        }
        _unsortedSearcher = new LeanIndexSearcher(_unsortedDir);

        _sortedPath = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-sort-s-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_sortedPath);
        _sortedDir = new LeanMMapDirectory(_sortedPath);

        using (var writer = new IndexWriter(_sortedDir, new IndexWriterConfig
        {
            MaxBufferedDocs = 10_000,
            RamBufferSizeMB = 256,
            IndexSort = new IndexSort(SortField.Numeric("price"))
        }))
        {
            IndexDocuments(writer);
            writer.Commit();
        }
        _sortedSearcher = new LeanIndexSearcher(_sortedDir);
    }
}
