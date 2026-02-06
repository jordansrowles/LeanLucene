using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Analysis;

public sealed class TokenBudgetTests : IClassFixture<TestDirectoryFixture>
{
    private readonly string _path;

    public TokenBudgetTests(TestDirectoryFixture fixture) => _path = fixture.Path;

    [Fact]
    public void Truncate_LimitsTokensToConfiguredBudget()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Truncate_LimitsTokensToConfiguredBudget));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig
        {
            MaxTokensPerDocument = 3,
            TokenBudgetPolicy = TokenBudgetPolicy.Truncate
        };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "one two three four five six"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act — search for a term that would only exist beyond the budget
        using var searcher = new IndexSearcher(mmap);
        var hitsFour = searcher.Search(new TermQuery("body", "four"), 10);
        var hitsOne = searcher.Search(new TermQuery("body", "one"), 10);

        // Assert
        Assert.Equal(0, hitsFour.TotalHits);
        Assert.Equal(1, hitsOne.TotalHits);
    }

    [Fact]
    public void Reject_ThrowsWhenBudgetExceeded()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Reject_ThrowsWhenBudgetExceeded));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig
        {
            MaxTokensPerDocument = 2,
            TokenBudgetPolicy = TokenBudgetPolicy.Reject
        };
        using var writer = new IndexWriter(mmap, config);

        var doc = new LeanDocument();
        doc.Add(new TextField("body", "one two three"));

        // Act & Assert
        var ex = Assert.Throws<TokenBudgetExceededException>(
            () => writer.AddDocument(doc));
        Assert.Equal(2, ex.Budget);
        Assert.True(ex.TokenCount > 2);
    }

    [Fact]
    public void Warn_AllowsAllTokensThroughWithoutThrowing()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Warn_AllowsAllTokensThroughWithoutThrowing));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig
        {
            MaxTokensPerDocument = 2,
            TokenBudgetPolicy = TokenBudgetPolicy.Warn
        };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "one two three four"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        // Act — all tokens should be indexed
        using var searcher = new IndexSearcher(mmap);
        var hits = searcher.Search(new TermQuery("body", "four"), 10);

        // Assert
        Assert.Equal(1, hits.TotalHits);
    }

    [Fact]
    public void ZeroBudget_MeansUnlimited()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(ZeroBudget_MeansUnlimited));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);
        var config = new IndexWriterConfig { MaxTokensPerDocument = 0 };

        using (var writer = new IndexWriter(mmap, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "a b c d e f g h i j"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(mmap);
        var hits = searcher.Search(new TermQuery("body", "j"), 10);

        // Assert — all tokens indexed
        Assert.Equal(1, hits.TotalHits);
    }
}
