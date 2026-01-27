using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Index;

[Trait("Category", "Index")]
[Trait("Category", "LiveDocs")]
public sealed class LiveDocsTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public LiveDocsTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void LiveDocs_DeleteDocument_MarkedInBitset()
    {
        var liveDocs = new LiveDocs(3);
        liveDocs.Delete(1);

        Assert.True(liveDocs.IsLive(0));
        Assert.False(liveDocs.IsLive(1));
        Assert.True(liveDocs.IsLive(2));
    }

    [Fact]
    public void LiveDocs_DeletedDoc_NotReturnedBySearch()
    {
        var subDir = System.IO.Path.Combine(_fixture.Path, "livedocs_search");
        System.IO.Directory.CreateDirectory(subDir);

        var dir = new MMapDirectory(subDir);
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "alpha content here"));
        writer.AddDocument(doc1);

        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "beta content here"));
        writer.AddDocument(doc2);

        writer.DeleteDocuments(new TermQuery("body", "alpha"));
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var results = searcher.Search(new TermQuery("body", "alpha"), 10);
        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void DeleteAndReopen_DeletedDocumentRemainsDeleted_NonDeletedDocumentFound()
    {
        var subDir = System.IO.Path.Combine(_fixture.Path, "delete_reopen");
        System.IO.Directory.CreateDirectory(subDir);

        var dir = new MMapDirectory(subDir);

        // Index several documents with unique IDs
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            var doc1 = new LeanDocument();
            doc1.Add(new TextField("id", "doc1"));
            doc1.Add(new TextField("body", "first document content"));
            writer.AddDocument(doc1);

            var doc2 = new LeanDocument();
            doc2.Add(new TextField("id", "doc2"));
            doc2.Add(new TextField("body", "second document content"));
            writer.AddDocument(doc2);

            var doc3 = new LeanDocument();
            doc3.Add(new TextField("id", "doc3"));
            doc3.Add(new TextField("body", "third document content"));
            writer.AddDocument(doc3);

            writer.Commit();

            // Delete one document by term
            writer.DeleteDocuments(new TermQuery("id", "doc2"));
            writer.Commit();
        }

        // Open a NEW IndexWriter on the same directory
        using (var writer = new IndexWriter(dir, new IndexWriterConfig()))
        {
            using var searcher = new IndexSearcher(dir);

            // Search for deleted document — assert 0 results
            var deletedResults = searcher.Search(new TermQuery("id", "doc2"), 10);
            Assert.Equal(0, deletedResults.TotalHits);

            // Search for non-deleted documents — assert they're found
            var doc1Results = searcher.Search(new TermQuery("id", "doc1"), 10);
            Assert.Equal(1, doc1Results.TotalHits);

            var doc3Results = searcher.Search(new TermQuery("id", "doc3"), 10);
            Assert.Equal(1, doc3Results.TotalHits);
        }
    }
}
