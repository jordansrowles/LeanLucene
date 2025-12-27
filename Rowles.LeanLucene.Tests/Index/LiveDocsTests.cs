using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Index;

public sealed class LiveDocsTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;

    public LiveDocsTests(TestDirectoryFixture fixture) => _fixture = fixture;

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
}
