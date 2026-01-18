using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Search;

[Trait("Category", "Search")]
[Trait("Category", "BooleanQuery")]
public sealed class BooleanQueryStreamingTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BooleanQueryStreamingTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string SubDir(string name)
    {
        var path = System.IO.Path.Combine(_fixture.Path, name);
        System.IO.Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void Must_SingleClause_ReturnsMatchingDocs()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_single_must"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "alpha beta", "gamma delta", "alpha gamma" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void Must_ThreeTerms_IntersectsAll()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_three_must"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[]
        {
            "red green blue",
            "red green yellow",
            "red blue purple",
            "green blue orange",
            "red green blue bright"
        };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "red"), Occur.Must);
        query.Add(new TermQuery("body", "green"), Occur.Must);
        query.Add(new TermQuery("body", "blue"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void Must_NoCommonDocs_ReturnsEmpty()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_must_disjoint"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "only alpha here"));
        writer.AddDocument(doc1);

        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "only beta here"));
        writer.AddDocument(doc2);

        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Must);
        query.Add(new TermQuery("body", "beta"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void Must_NonexistentTerm_ReturnsEmpty()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_must_missing"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "some content"));
        writer.AddDocument(doc);
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "some"), Occur.Must);
        query.Add(new TermQuery("body", "nonexistent"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void Should_SingleClause_ReturnsMatchingDocs()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_single_should"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "alpha beta", "gamma delta", "alpha gamma" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Should);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void Should_MultipleTerms_ScoreSumsCorrectly()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_should_score"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        // Doc 0 matches both Should terms → higher score
        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "alpha beta"));
        writer.AddDocument(doc1);

        // Doc 1 matches only one term
        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "alpha only"));
        writer.AddDocument(doc2);

        // Doc 2 matches only the other term
        var doc3 = new LeanDocument();
        doc3.Add(new TextField("body", "beta only"));
        writer.AddDocument(doc3);

        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Should);
        query.Add(new TermQuery("body", "beta"), Occur.Should);
        var results = searcher.Search(query, 10);

        Assert.Equal(3, results.TotalHits);
        // Doc matching both terms should rank highest
        Assert.Equal(0, results.ScoreDocs[0].DocId);
    }

    [Fact]
    public void MustNot_ExcludesFromMustResults()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_mustnot"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "search engine", "search database", "search cache" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "search"), Occur.Must);
        query.Add(new TermQuery("body", "database"), Occur.MustNot);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void MustNot_MultipleClauses_ExcludesAll()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_multi_mustnot"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "search engine", "search database", "search cache", "search proxy" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "search"), Occur.Must);
        query.Add(new TermQuery("body", "database"), Occur.MustNot);
        query.Add(new TermQuery("body", "cache"), Occur.MustNot);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void Must_WithShould_ShouldBoostsScore()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_must_should"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        // Both match "fast", but doc 0 also matches the Should clause "search"
        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "fast search"));
        writer.AddDocument(doc1);

        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "fast indexing"));
        writer.AddDocument(doc2);

        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "fast"), Occur.Must);
        query.Add(new TermQuery("body", "search"), Occur.Should);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
        // Doc matching both Must + Should should rank higher
        Assert.Equal(0, results.ScoreDocs[0].DocId);
        Assert.True(results.ScoreDocs[0].Score > results.ScoreDocs[1].Score);
    }

    [Fact]
    public void DeletedDocs_ExcludedFromStreamingResults()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_deleted"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "fast search", "fast indexing", "fast querying" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        // Delete doc 1
        writer.DeleteDocuments(new TermQuery("body", "indexing"));
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "fast"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(2, results.TotalHits);
        var docIds = results.ScoreDocs.Select(sd => sd.DocId).ToHashSet();
        Assert.DoesNotContain(1, docIds);
    }

    [Fact]
    public void MustNot_AllExcluded_ReturnsEmpty()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_all_excluded"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        // All docs match both Must and MustNot terms
        var texts = new[] { "fast search", "fast query search" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "fast"), Occur.Must);
        query.Add(new TermQuery("body", "search"), Occur.MustNot);
        var results = searcher.Search(query, 10);

        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void EmptyIndex_ReturnsEmpty()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_empty"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "anything"), Occur.Must);
        var results = searcher.Search(query, 10);

        Assert.Equal(0, results.TotalHits);
    }

    [Fact]
    public void ShouldWithMustNot_StreamingMerge_ExcludesCorrectly()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_should_mustnot"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        var texts = new[] { "alpha beta", "gamma delta", "alpha gamma", "beta delta" };
        foreach (var text in texts)
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", text));
            writer.AddDocument(doc);
        }
        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "alpha"), Occur.Should);
        query.Add(new TermQuery("body", "beta"), Occur.Should);
        query.Add(new TermQuery("body", "gamma"), Occur.MustNot);
        var results = searcher.Search(query, 10);

        // Docs: 0 (alpha beta), 1 (gamma delta), 2 (alpha gamma), 3 (beta delta)
        // Should matches: alpha→[0,2], beta→[0,3] → union [0,2,3]
        // MustNot gamma→[1,2] → exclude doc 2
        // Expected: docs 0 and 3
        Assert.Equal(2, results.TotalHits);
    }

    [Fact]
    public void Should_ThreeTerms_ScoresMultiMatchHigher()
    {
        var dir = new MMapDirectory(SubDir("bool_stream_should_three"));
        using var writer = new IndexWriter(dir, new IndexWriterConfig());

        // Doc 0 matches all three Should terms
        var doc1 = new LeanDocument();
        doc1.Add(new TextField("body", "red green blue"));
        writer.AddDocument(doc1);

        // Doc 1 matches two
        var doc2 = new LeanDocument();
        doc2.Add(new TextField("body", "red green yellow"));
        writer.AddDocument(doc2);

        // Doc 2 matches one
        var doc3 = new LeanDocument();
        doc3.Add(new TextField("body", "red yellow purple"));
        writer.AddDocument(doc3);

        writer.Commit();

        using var searcher = new IndexSearcher(dir);
        var query = new BooleanQuery();
        query.Add(new TermQuery("body", "red"), Occur.Should);
        query.Add(new TermQuery("body", "green"), Occur.Should);
        query.Add(new TermQuery("body", "blue"), Occur.Should);
        var results = searcher.Search(query, 10);

        Assert.Equal(3, results.TotalHits);
        // Doc matching most terms should rank highest
        Assert.Equal(0, results.ScoreDocs[0].DocId);
        Assert.True(results.ScoreDocs[0].Score > results.ScoreDocs[1].Score);
        Assert.True(results.ScoreDocs[1].Score > results.ScoreDocs[2].Score);
    }
}
