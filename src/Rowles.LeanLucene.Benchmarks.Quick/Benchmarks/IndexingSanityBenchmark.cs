using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Benchmarks.Quick.Benchmarks;

/// <summary>
/// Sanity benchmark: indexes 1,000 documents to a temporary MMapDirectory and commits.
/// Verifies that write-path throughput has not regressed.
/// </summary>
internal sealed class IndexingSanityBenchmark : IQuickBenchmark
{
    private const int DocumentCount = 1_000;

    public string Name => "Indexing.Index1000Docs";

    private string[] _documents = [];

    public void Setup()
    {
        _documents = SanityBenchmarkData.BuildDocuments(DocumentCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Run()
    {
        var path = Path.Combine(Path.GetTempPath(), $"leanlucene-quick-idx-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);

        try
        {
            var directory = new MMapDirectory(path);
            using var writer = new IndexWriter(
                directory,
                new IndexWriterConfig { MaxBufferedDocs = 10_000, RamBufferSizeMB = 64 });

            for (int i = 0; i < _documents.Length; i++)
            {
                var doc = new LeanDocument();
                doc.Add(new StringField("id", i.ToString(CultureInfo.InvariantCulture)));
                doc.Add(new TextField("body", _documents[i]));
                writer.AddDocument(doc);
            }

            writer.Commit();
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }

    public void Cleanup()
    {
        _documents = [];
    }
}
