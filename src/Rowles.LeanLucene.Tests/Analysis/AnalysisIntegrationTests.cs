using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Analysers;
using Rowles.LeanLucene.Analysis.Tokenisers;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Analysis;

/// <summary>
/// End-to-end integration tests verifying that custom analysers (NGramTokeniser,
/// EdgeNGramTokeniser, AccentFoldingFilter) work correctly through the full
/// index→search pipeline.
/// </summary>
[Trait("Category", "Analysis")]
[Trait("Category", "Integration")]
public sealed class AnalysisIntegrationTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AnalysisIntegrationTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string SubDir(string name)
    {
        var path = Path.Combine(_fixture.Path, name);
        Directory.CreateDirectory(path);
        return path;
    }

    // ── NGramTokeniser end-to-end───────────────────────────────────────────

    [Fact]
    public void NGramTokeniser_Index_PartialWordSearchFindsDocument()
    {
        var analyser = new Analyser(new NGramTokeniser(2, 3));
        var dir = new MMapDirectory(SubDir("ngram_e2e"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);

        // "ll" is a bigram of "hello"
        var results = searcher.Search(new TermQuery("body", "ll"), 10);
        _output.WriteLine($"NGram E2E: TermQuery('ll') on 'hello' → {results.TotalHits} hits");
        Assert.Equal(1, results.TotalHits);

        // "ell" is a trigram of "hello"
        var results2 = searcher.Search(new TermQuery("body", "ell"), 10);
        _output.WriteLine($"NGram E2E: TermQuery('ell') on 'hello' → {results2.TotalHits} hits");
        Assert.Equal(1, results2.TotalHits);
    }

    [Fact]
    public void NGramTokeniser_BigramOnIndex_MatchesExpectedDocs()
    {
        var analyser = new Analyser(new NGramTokeniser(2, 2));
        var dir = new MMapDirectory(SubDir("ngram_bigram"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc1 = new LeanDocument();
            doc1.Add(new TextField("body", "cat"));
            writer.AddDocument(doc1);

            var doc2 = new LeanDocument();
            doc2.Add(new TextField("body", "cart"));
            writer.AddDocument(doc2);

            var doc3 = new LeanDocument();
            doc3.Add(new TextField("body", "dog"));
            writer.AddDocument(doc3);

            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);

        // "ca" bigram should match "cat" and "cart" but not "dog"
        var results = searcher.Search(new TermQuery("body", "ca"), 10);
        _output.WriteLine($"NGram bigram: TermQuery('ca') → {results.TotalHits} hits");
        Assert.Equal(2, results.TotalHits);
    }

    // ── EdgeNGramTokeniser end-to-end ───────────────────────────────────────

    [Fact]
    public void EdgeNGramTokeniser_PrefixAutocomplete_TermQueryFindsDoc()
    {
        var analyser = new Analyser(new EdgeNGramTokeniser(1, 4));
        var dir = new MMapDirectory(SubDir("edgengram_e2e"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello world"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);

        // "he" is an edge n-gram of "hello"
        var results = searcher.Search(new TermQuery("body", "he"), 10);
        _output.WriteLine($"EdgeNGram E2E: TermQuery('he') on 'hello world' → {results.TotalHits} hits");
        Assert.Equal(1, results.TotalHits);

        // "wo" is an edge n-gram of "world"
        var results2 = searcher.Search(new TermQuery("body", "wo"), 10);
        _output.WriteLine($"EdgeNGram E2E: TermQuery('wo') on 'hello world' → {results2.TotalHits} hits");
        Assert.Equal(1, results2.TotalHits);
    }

    [Fact]
    public void EdgeNGramTokeniser_NonEdgeSubstring_DoesNotMatch()
    {
        var analyser = new Analyser(new EdgeNGramTokeniser(2, 3));
        var dir = new MMapDirectory(SubDir("edgengram_nonedge"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "hello"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);

        // "lo" is NOT an edge n-gram of "hello" (it's in the middle)
        var results = searcher.Search(new TermQuery("body", "lo"), 10);
        _output.WriteLine($"EdgeNGram: TermQuery('lo') on 'hello' → {results.TotalHits} hits (expected 0)");
        Assert.Equal(0, results.TotalHits);
    }

    // ── AccentFoldingFilter end-to-end ──────────────────────────────────────

    [Fact]
    public void AccentFoldingFilter_IndexAccented_SearchUnaccented_FindsDoc()
    {
        var analyser = new Analyser(
            new Tokeniser(),
            new LowercaseFilter(),
            new AccentFoldingFilter());

        var dir = new MMapDirectory(SubDir("accent_e2e"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "café naïve résumé"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);

        foreach (var term in new[] { "cafe", "naive", "resume" })
        {
            var results = searcher.Search(new TermQuery("body", term), 10);
            _output.WriteLine($"AccentFolding E2E: TermQuery('{term}') → {results.TotalHits} hits");
            Assert.Equal(1, results.TotalHits);
        }
    }

    [Fact]
    public void AccentFoldingFilter_IndexUnaccented_SearchUnaccented_StillWorks()
    {
        var analyser = new Analyser(
            new Tokeniser(),
            new LowercaseFilter(),
            new AccentFoldingFilter());

        var dir = new MMapDirectory(SubDir("accent_noaccent"));
        var config = new IndexWriterConfig { DefaultAnalyser = analyser };

        using (var writer = new IndexWriter(dir, config))
        {
            var doc = new LeanDocument();
            doc.Add(new TextField("body", "cafe naive resume"));
            writer.AddDocument(doc);
            writer.Commit();
        }

        using var searcher = new IndexSearcher(dir);
        var results = searcher.Search(new TermQuery("body", "cafe"), 10);
        _output.WriteLine($"AccentFolding (no accents): TermQuery('cafe') → {results.TotalHits} hits");
        Assert.Equal(1, results.TotalHits);
    }
}
