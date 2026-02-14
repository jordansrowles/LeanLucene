using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index.Indexer;
using Rowles.LeanLucene.Search.Searcher;
using Rowles.LeanLucene.Search.Suggestions;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;

namespace Rowles.LeanLucene.Tests.Search.Suggestions;

public sealed class DidYouMeanTests : IClassFixture<TestDirectoryFixture>
{
    private readonly string _path;

    public DidYouMeanTests(TestDirectoryFixture fixture) => _path = fixture.Path;

    [Fact]
    public void Suggest_ReturnsClosestTermByEditDistance()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Suggest_ReturnsClosestTermByEditDistance));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, new IndexWriterConfig()))
        {
            foreach (var word in new[] { "hello", "help", "hero", "world" })
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("body", word));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        using var searcher = new IndexSearcher(mmap);

        // Act — "helo" is 1 edit from "hello" and "hero"
        var suggestions = DidYouMeanSuggester.Suggest(searcher, "body", "helo", maxEdits: 2, topN: 5);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Term == "hello");
    }

    [Fact]
    public void Suggest_ExcludesExactMatch()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Suggest_ExcludesExactMatch));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, new IndexWriterConfig()))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(mmap);

        // Act
        var suggestions = DidYouMeanSuggester.Suggest(searcher, "body", "hello");

        // Assert — exact match should not appear
        Assert.DoesNotContain(suggestions, s => s.Term == "hello");
    }

    [Fact]
    public void Suggest_RespectsTopN()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Suggest_RespectsTopN));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, new IndexWriterConfig()))
        {
            foreach (var word in new[] { "cat", "car", "cap", "cab", "can", "cam" })
            {
                var doc = new LeanDocument();
                doc.Add(new TextField("body", word));
                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        using var searcher = new IndexSearcher(mmap);

        // Act
        var suggestions = DidYouMeanSuggester.Suggest(searcher, "body", "cas", topN: 3);

        // Assert
        Assert.True(suggestions.Count <= 3);
    }

    [Fact]
    public void Suggest_ReturnsEmptyForNoMatches()
    {
        // Arrange
        var dir = Path.Combine(_path, nameof(Suggest_ReturnsEmptyForNoMatches));
        Directory.CreateDirectory(dir);
        var mmap = new MMapDirectory(dir);

        using (var writer = new IndexWriter(mmap, new IndexWriterConfig()))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(mmap);

        // Act — "zzzzz" is far from any indexed term
        var suggestions = DidYouMeanSuggester.Suggest(searcher, "body", "zzzzz", maxEdits: 1);

        // Assert
        Assert.Empty(suggestions);
    }
}
