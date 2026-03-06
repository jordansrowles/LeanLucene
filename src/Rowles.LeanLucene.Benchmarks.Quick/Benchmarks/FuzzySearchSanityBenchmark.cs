using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Queries;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Benchmarks.Quick.Benchmarks;

/// <summary>
/// Sanity benchmark: builds a 10,000-doc index once, then measures FuzzyQuery search time
/// for both single-edit (maxEdits=1) and double-edit (maxEdits=2) queries.
/// Verifies that fuzzy search performance has not regressed.
/// </summary>
internal sealed class FuzzySearchSanityBenchmark : IQuickBenchmark
{
    private const int DocumentCount = 10_000;
    private const int TopN = 10;

    public string Name => "Search.FuzzyQuery10kDocs";

    private string[] _documents = [];
    private string _indexPath = string.Empty;
    private MMapDirectory? _directory;
    private IndexSearcher? _searcher;

    public void Setup()
    {
        _documents = SanityBenchmarkData.BuildDocuments(DocumentCount);
        BuildIndex();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Run()
    {
        // Single-edit fuzzy query (the regression target).
        var topDocs1 = _searcher!.Search(new FuzzyQuery("body", "serch", maxEdits: 1), TopN);

        // Double-edit fuzzy query (should remain unchanged).
        var topDocs2 = _searcher!.Search(new FuzzyQuery("body", "banchmark", maxEdits: 2), TopN);

        // Prevent dead-code elimination.
        if (topDocs1.TotalHits < 0 || topDocs2.TotalHits < 0)
            throw new InvalidOperationException("Unexpected negative hit count.");
    }

    public void Cleanup()
    {
        _searcher?.Dispose();
        _searcher = null;
        _directory = null;

        if (!string.IsNullOrWhiteSpace(_indexPath) && Directory.Exists(_indexPath))
            Directory.Delete(_indexPath, recursive: true);

        _documents = [];
    }

    private void BuildIndex()
    {
        _indexPath = Path.Combine(Path.GetTempPath(), $"leanlucene-quick-fuzzy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_indexPath);

        _directory = new MMapDirectory(_indexPath);
        using (var writer = new IndexWriter(
            _directory,
            new IndexWriterConfig { MaxBufferedDocs = 20_000, RamBufferSizeMB = 64 }))
        {
            for (int i = 0; i < _documents.Length; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new StringField("id", i.ToString(CultureInfo.InvariantCulture)));
                doc.Add(new TextField("body", _documents[i]));
                writer.AddDocument(doc);
            }

            writer.Commit();
        }

        _searcher = new IndexSearcher(_directory);
    }
}
