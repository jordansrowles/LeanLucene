using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Infrastructure;

namespace Rowles.LeanLucene.Tests.Index;

public sealed class ConcurrentIndexingTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ll-conc-{Guid.NewGuid():N}");

    public ConcurrentIndexingTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void AddDocumentsConcurrent_AllDocsSearchable()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 500 });

        var docs = new List<LeanDocument>();
        for (int i = 0; i < 100; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new StringField("id", i.ToString()));
            doc.Add(new TextField("body", $"document number {i} with searchable content"));
            docs.Add(doc);
        }

        writer.AddDocumentsConcurrent(docs);
        writer.Commit();

        using var searcher = new IndexSearcher(directory);
        var results = searcher.Search(new TermQuery("body", "document"), 200);

        Assert.Equal(100, results.TotalHits);
    }

    [Fact]
    public void AddDocumentsConcurrent_PreservesStoredFields()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 500 });

        var docs = new List<LeanDocument>();
        for (int i = 0; i < 50; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new StringField("id", i.ToString()));
            doc.Add(new TextField("body", "hello world"));
            docs.Add(doc);
        }

        writer.AddDocumentsConcurrent(docs);
        writer.Commit();

        using var searcher = new IndexSearcher(directory);
        var results = searcher.Search(new TermQuery("body", "hello"), 100);

        Assert.Equal(50, results.TotalHits);

        // Verify stored fields for first hit
        var stored = searcher.GetStoredFields(results.ScoreDocs[0].DocId);
        Assert.True(stored.ContainsKey("id"));
        Assert.True(stored.ContainsKey("body"));
        Assert.True(stored["id"].Count > 0);
        Assert.True(stored["body"].Count > 0);
    }

    [Fact]
    public void AddDocumentsConcurrent_EmptyBatch_NoOp()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig());

        writer.AddDocumentsConcurrent([]);
        writer.Commit();

        using var searcher = new IndexSearcher(directory);
        var results = searcher.Search(new TermQuery("body", "anything"), 10);
        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void AddDocumentsConcurrent_WithNumericFields()
    {
        var directory = new MMapDirectory(_dir);
        using var writer = new IndexWriter(directory, new IndexWriterConfig { MaxBufferedDocs = 500 });

        var docs = new List<LeanDocument>();
        for (int i = 0; i < 30; i++)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "product listing"));
            doc.Add(new NumericField("price", i * 10.0));
            docs.Add(doc);
        }

        writer.AddDocumentsConcurrent(docs);
        writer.Commit();

        using var searcher = new IndexSearcher(directory);
        var results = searcher.Search(new TermQuery("body", "product"), 100);

        Assert.Equal(30, results.TotalHits);
    }
}
