using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Tests IAutomaton implementations (Prefix, Wildcard, Levenshtein) and their intersection
/// with the FSTReader term dictionary.
/// </summary>
[Trait("Category", "Codecs")]
public sealed class FSTAutomatonTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public FSTAutomatonTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string DicPath(string name) => Path.Combine(_fixture.Path, name + ".dic");

    private TermDictionaryReader BuildDictionary(string name, params string[] bareTerms)
    {
        var path = DicPath(name);
        var fieldPrefix = "body\0";
        var terms = bareTerms.Select(t => fieldPrefix + t).Order().ToList();
        var offsets = new Dictionary<string, long>();
        long offset = 100;
        foreach (var t in terms)
            offsets[t] = offset++;

        TermDictionaryWriter.Write(path, terms, offsets);
        return TermDictionaryReader.Open(path);
    }

    // ── Prefix Automaton ─────────────────────────────────────────────────

    [Fact]
    public void PrefixAutomaton_MatchesCorrectTerms()
    {
        using var reader = BuildDictionary("prefix_test",
            "sea", "search", "searching", "set", "seal", "zebra");

        var automaton = new PrefixAutomaton("sea");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        Assert.Contains("sea", terms);
        Assert.Contains("search", terms);
        Assert.Contains("searching", terms);
        Assert.Contains("seal", terms);
        Assert.DoesNotContain("set", terms);
        Assert.DoesNotContain("zebra", terms);
    }

    [Fact]
    public void PrefixAutomaton_EmptyPrefix_MatchesAll()
    {
        using var reader = BuildDictionary("prefix_empty",
            "apple", "banana", "cherry");

        var automaton = new PrefixAutomaton("");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);

        Assert.Equal(3, results.Count);
    }

    // ── Wildcard Automaton ───────────────────────────────────────────────

    [Fact]
    public void WildcardAutomaton_StarPattern_MatchesCorrectly()
    {
        using var reader = BuildDictionary("wildcard_star",
            "search", "stitch", "seat", "scratch", "such");

        var automaton = new WildcardAutomaton("s*ch");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        Assert.Contains("search", terms);
        Assert.Contains("stitch", terms);
        Assert.Contains("such", terms);
        Assert.DoesNotContain("seat", terms);
    }

    [Fact]
    public void WildcardAutomaton_QuestionMark_MatchesSingleChar()
    {
        using var reader = BuildDictionary("wildcard_question",
            "cat", "cot", "cut", "cart", "coat");

        var automaton = new WildcardAutomaton("c?t");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        Assert.Contains("cat", terms);
        Assert.Contains("cot", terms);
        Assert.Contains("cut", terms);
        Assert.DoesNotContain("cart", terms);
        Assert.DoesNotContain("coat", terms);
    }

    [Fact]
    public void WildcardAutomaton_StarOnly_MatchesAll()
    {
        using var reader = BuildDictionary("wildcard_all",
            "alpha", "beta", "gamma");

        var automaton = new WildcardAutomaton("*");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);

        Assert.Equal(3, results.Count);
    }

    // ── Levenshtein Automaton ────────────────────────────────────────────

    [Fact]
    public void LevenshteinAutomaton_Edit1_MatchesInsertionOnly()
    {
        using var reader = BuildDictionary("lev1",
            "search", "scratch", "seat", "serch");

        var automaton = new LevenshteinAutomaton("serch", 1);
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        // "search" is edit distance 1 from "serch" (insert 'a')
        Assert.Contains("search", terms);
        // "serch" is edit distance 0 (exact)
        Assert.Contains("serch", terms);
        // "scratch" is too far
        Assert.DoesNotContain("scratch", terms);
    }

    [Fact]
    public void LevenshteinAutomaton_Edit2_MatchesSubstitutionAndInsertion()
    {
        using var reader = BuildDictionary("lev2",
            "vector", "vectir", "victor", "venture", "very");

        var automaton = new LevenshteinAutomaton("vectr", 2);
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        // "vector" = insert 'o' (edit 1)
        Assert.Contains("vector", terms);
        // "vectir" = substitute 'r'→'i' + insert 'r' (edit 2)
        Assert.Contains("vectir", terms);
    }

    [Fact]
    public void LevenshteinAutomaton_ExactMatch_Edit0()
    {
        using var reader = BuildDictionary("lev_exact",
            "hello", "world");

        var automaton = new LevenshteinAutomaton("hello", 0);
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToList();

        Assert.Single(terms);
        Assert.Equal("hello", terms[0]);
    }

    // ── Integration: Large Dictionary ───────────────────────────────────

    [Fact]
    public void LargeDictionary_PrefixIntersect_MatchesBruteForce()
    {
        // Generate 1000 terms
        var bareTerms = Enumerable.Range(0, 1000)
            .Select(i => $"term{i:D4}")
            .ToArray();

        using var reader = BuildDictionary("large_prefix", bareTerms);

        // Prefix "term00" should match term0000..term0099
        var automaton = new PrefixAutomaton("term00");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);

        // Brute force
        var expected = bareTerms.Where(t => t.StartsWith("term00")).ToList();
        Assert.Equal(expected.Count, results.Count);
    }

    [Fact]
    public void LargeDictionary_WildcardIntersect_MatchesBruteForce()
    {
        var bareTerms = Enumerable.Range(0, 500)
            .Select(i => $"word{i:D3}")
            .ToArray();

        using var reader = BuildDictionary("large_wildcard", bareTerms);

        // "word?5?" should match word050, word051, ..., word059, word150, ..., word459
        var automaton = new WildcardAutomaton("word?5?");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        var terms = results.Select(r => r.Term.Replace("body\0", "")).ToHashSet();

        var expected = bareTerms.Where(t =>
            t.Length == 7 && t[4] != '\0' && t[5] == '5' && t[6] != '\0').ToHashSet();

        Assert.Equal(expected, terms);
    }

    // ── Edge Cases ──────────────────────────────────────────────────────

    [Fact]
    public void EmptyDictionary_IntersectReturnsEmpty()
    {
        var path = DicPath("automaton_empty");
        TermDictionaryWriter.Write(path, [], []);
        using var reader = TermDictionaryReader.Open(path);

        var automaton = new PrefixAutomaton("any");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        Assert.Empty(results);
    }

    [Fact]
    public void PrefixAutomaton_NoMatches_ReturnsEmpty()
    {
        using var reader = BuildDictionary("no_match", "apple", "banana");

        var automaton = new PrefixAutomaton("xyz");
        var results = FSTAutomaton.Intersect(reader, "body\0", automaton);
        Assert.Empty(results);
    }
}
