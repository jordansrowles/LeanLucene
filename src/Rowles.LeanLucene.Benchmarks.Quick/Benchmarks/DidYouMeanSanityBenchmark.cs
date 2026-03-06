using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Search.Suggestions;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Benchmarks.Quick.Benchmarks;

/// <summary>
/// Sanity benchmark for <see cref="DidYouMeanSuggester"/>: the on-the-fly
/// FST-based spelling correction that scans the per-segment term dictionary.
/// Builds a 1,000-doc index, then measures suggestion time for 10 misspelled terms.
/// </summary>
internal sealed class DidYouMeanSanityBenchmark : IQuickBenchmark
{
    private const int DocumentCount = 1_000;

    public string Name => "Suggester.DidYouMean";

    private (string Original, string Misspelled)[] _misspelledTerms = [];
    private string _indexPath = string.Empty;
    private MMapDirectory? _directory;
    private IndexSearcher? _searcher;

    public void Setup()
    {
        var documents = SanityBenchmarkData.BuildDocuments(DocumentCount);
        _misspelledTerms = SanityBenchmarkData.BuildMisspelledTerms();
        BuildIndex(documents);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Run()
    {
        int total = 0;
        foreach (var (_, misspelled) in _misspelledTerms)
        {
            var suggestions = DidYouMeanSuggester.Suggest(
                _searcher!, "body", misspelled, maxEdits: 2, topN: 5);
            total += suggestions.Count;
        }

        // Prevent dead-code elimination.
        if (total < 0)
            throw new InvalidOperationException("Unexpected negative suggestion count.");
    }

    public void Cleanup()
    {
        _searcher?.Dispose();
        _searcher = null;
        _directory = null;

        if (!string.IsNullOrWhiteSpace(_indexPath) && Directory.Exists(_indexPath))
            Directory.Delete(_indexPath, recursive: true);

        _misspelledTerms = [];
    }

    private void BuildIndex(string[] documents)
    {
        _indexPath = Path.Combine(Path.GetTempPath(), $"leanlucene-quick-dym-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_indexPath);

        _directory = new MMapDirectory(_indexPath);
        using (var writer = new IndexWriter(
            _directory,
            new IndexWriterConfig { MaxBufferedDocs = 10_000, RamBufferSizeMB = 64 }))
        {
            for (int i = 0; i < documents.Length; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new StringField("id", i.ToString(CultureInfo.InvariantCulture)));
                doc.Add(new TextField("body", documents[i]));
                writer.AddDocument(doc);
            }

            writer.Commit();
        }

        _searcher = new IndexSearcher(_directory);
    }
}
