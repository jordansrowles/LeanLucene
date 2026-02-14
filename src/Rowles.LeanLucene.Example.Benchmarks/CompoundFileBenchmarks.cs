using BenchmarkDotNet.Attributes;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LeanTermQuery = Rowles.LeanLucene.Search.Queries.TermQuery;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Rowles.LeanLucene.Store;
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
/// Compares compound file read/write performance: LeanLucene compound vs non-compound,
/// plus Lucene.NET compound baseline.
/// </summary>
[MemoryDiagnoser]
[HtmlExporter]
[JsonExporterAttribute.Full]
[MarkdownExporterAttribute.GitHub]
[RPlotExporter]
[KeepBenchmarkFiles]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.Net11_0)]
public class CompoundFileBenchmarks
{
    private const int TopN = 25;

    public static IEnumerable<int> DocCounts => BenchmarkData.GetDocCounts(BenchmarkData.DefaultDocCount);

    [ParamsSource(nameof(DocCounts))]
    public int DocumentCount { get; set; }

    private string[] _documents = [];

    // Pre-built indices for search benchmarks
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
        _documents = BenchmarkData.BuildDocuments(DocumentCount);
        BuildLeanSearchIndices();
        BuildLuceneNetSearchIndex();
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
    public int LeanLucene_Index_NoCompound()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-cfs-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using var writer = new LeanIndexWriter(directory, new LeanIndexWriterConfig
            {
                MaxBufferedDocs = 10_000,
                RamBufferSizeMB = 256,
                UseCompoundFile = false
            });

            IndexDocuments(writer);
            writer.Commit();
            return _documents.Length;
        }
        finally
        {
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, recursive: true);
        }
    }

    [Benchmark]
    public int LeanLucene_Index_Compound()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-bench-cfs-{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(path);

        try
        {
            var directory = new LeanMMapDirectory(path);
            using var writer = new LeanIndexWriter(directory, new LeanIndexWriterConfig
            {
                MaxBufferedDocs = 10_000,
                RamBufferSizeMB = 256,
                UseCompoundFile = true
            });

            IndexDocuments(writer);
            writer.Commit();
            return _documents.Length;
        }
        finally
        {
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, recursive: true);
        }
    }

    [Benchmark]
    public int LeanLucene_Search_NoCompound()
    {
        var topDocs = _leanNoCompoundSearcher!.Search(new LeanTermQuery("body", "search"), TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int LeanLucene_Search_Compound()
    {
        var topDocs = _leanCompoundSearcher!.Search(new LeanTermQuery("body", "search"), TopN);
        return topDocs.TotalHits;
    }

    [Benchmark]
    public int LuceneNet_Index_Compound()
    {
        using var directory = new RAMDirectory();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        using var writer = new Lucene.Net.Index.IndexWriter(directory,
            new Lucene.Net.Index.IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                UseCompoundFile = true
            });

        for (int i = 0; i < _documents.Length; i++)
        {
            var doc = new Lucene.Net.Documents.Document
            {
                new LuceneStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture), Field.Store.NO),
                new LuceneTextField("body", _documents[i], Field.Store.NO)
            };
            writer.AddDocument(doc);
        }

        writer.Commit();
        return _documents.Length;
    }

    [Benchmark]
    public int LuceneNet_Search_Compound()
    {
        var query = new Lucene.Net.Search.TermQuery(new Term("body", "search"));
        var topDocs = _luceneCompoundSearcher!.Search(query, TopN);
        return topDocs.TotalHits;
    }

    private void IndexDocuments(LeanIndexWriter writer)
    {
        for (int i = 0; i < _documents.Length; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new LeanStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            doc.Add(new LeanTextField("body", _documents[i]));
            writer.AddDocument(doc);
        }
    }

    private void BuildLeanSearchIndices()
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
            IndexDocuments(writer);
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
            IndexDocuments(writer);
            writer.Commit();
        }
        _leanCompoundSearcher = new LeanIndexSearcher(_leanCompoundDir);
    }

    private void BuildLuceneNetSearchIndex()
    {
        _luceneCompoundDirectory = new RAMDirectory();
        using var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

        using (var writer = new Lucene.Net.Index.IndexWriter(_luceneCompoundDirectory,
            new Lucene.Net.Index.IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
            {
                UseCompoundFile = true
            }))
        {
            for (int i = 0; i < _documents.Length; i++)
            {
                var doc = new Lucene.Net.Documents.Document
                {
                    new LuceneStringField("id", i.ToString(System.Globalization.CultureInfo.InvariantCulture), Field.Store.NO),
                    new LuceneTextField("body", _documents[i], Field.Store.NO)
                };
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        _luceneCompoundReader = DirectoryReader.Open(_luceneCompoundDirectory);
        _luceneCompoundSearcher = new LuceneIndexSearcher(_luceneCompoundReader);
    }
}
