using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Search;

public sealed class ExplainTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    public ExplainTests(TestDirectoryFixture fixture) => _fixture = fixture;

    private string SubDir(string name)
    {
        var path = System.IO.Path.Combine(_fixture.Path, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Explain_MatchingDoc_ReturnsBreakdown()
    {
        // Arrange
        var dir = new MMapDirectory(SubDir("explain_match"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "search engine performance"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new TermQuery("body", "search");

        // Act
        var explanation = searcher.Explain(query, 0);

        // Assert
        Assert.NotNull(explanation);
        Assert.True(explanation.Score > 0);
        Assert.Contains("BM25", explanation.Description);
        Assert.True(explanation.Details.Length >= 3);
    }

    [Fact]
    public void Explain_NonMatchingDoc_ReturnsNull()
    {
        // Arrange
        var dir = new MMapDirectory(SubDir("explain_nomatch"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "unrelated content"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new TermQuery("body", "search");

        // Act
        var explanation = searcher.Explain(query, 0);

        // Assert
        Assert.Null(explanation);
    }

    [Fact]
    public void Explain_InvalidDocId_ReturnsNull()
    {
        // Arrange
        var dir = new MMapDirectory(SubDir("explain_invalid"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "test document"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);

        // Act — doc ID 999 doesn't exist
        var explanation = searcher.Explain(new TermQuery("body", "test"), 999);

        // Assert
        Assert.Null(explanation);
    }

    [Fact]
    public void Explain_ToString_ProducesReadableOutput()
    {
        // Arrange
        var dir = new MMapDirectory(SubDir("explain_tostring"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        var doc = new LeanDocument();
        doc.Add(new TextField("body", "search engine performance"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);

        // Act
        var explanation = searcher.Explain(new TermQuery("body", "search"), 0);

        // Assert
        var text = explanation!.ToString();
        Assert.Contains("idf", text);
        Assert.Contains("termFreq", text);
    }
}
