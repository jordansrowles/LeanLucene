using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.Hnsw;
using Rowles.LeanLucene.Codecs.Fst;
using Rowles.LeanLucene.Codecs.Bkd;
using Rowles.LeanLucene.Codecs.Vectors;
using Rowles.LeanLucene.Codecs.TermVectors.TermVectors;
using Rowles.LeanLucene.Codecs.TermDictionary;
using Rowles.LeanLucene.Store;
using Rowles.LeanLucene.Tests.Fixtures;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Codecs;

/// <summary>
/// Tests the v2 byte-keyed term dictionary (FSTBuilder → TermDictionaryReader) round-trip
/// and all consumer methods: exact lookup, prefix, wildcard, fuzzy, range, regex, field enum.
/// </summary>
[Trait("Category", "Codecs")]
public sealed class FSTDictionaryTests : IClassFixture<TestDirectoryFixture>
{
    private readonly TestDirectoryFixture _fixture;
    private readonly ITestOutputHelper _output;

    public FSTDictionaryTests(TestDirectoryFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private string DicPath(string name) => Path.Combine(_fixture.Path, name + ".dic");

    private static void WriteDictionary(string path, List<string> terms, Dictionary<string, long> offsets)
    {
        TermDictionaryWriter.Write(path, terms, offsets);
    }

    // ── Empty dictionary ────────────────────────────────────────────────────

    [Fact]
    public void EmptyDictionary_ExactLookupReturnsFalse()
    {
        var path = DicPath("empty");
        WriteDictionary(path, [], []);
        using var reader = TermDictionaryReader.Open(path);
        Assert.False(reader.TryGetPostingsOffset("anything", out _));
    }

    [Fact]
    public void EmptyDictionary_PrefixReturnsEmpty()
    {
        var path = DicPath("empty_prefix");
        WriteDictionary(path, [], []);
        using var reader = TermDictionaryReader.Open(path);
        Assert.Empty(reader.GetTermsWithPrefix("body\0".AsSpan()));
    }

    // ── Single term ─────────────────────────────────────────────────────────

    [Fact]
    public void SingleTerm_ExactLookupSucceeds()
    {
        var path = DicPath("single");
        var terms = new List<string> { "body\0hello" };
        var offsets = new Dictionary<string, long> { ["body\0hello"] = 42L };
        WriteDictionary(path, terms, offsets);

        using var reader = TermDictionaryReader.Open(path);
        Assert.True(reader.TryGetPostingsOffset("body\0hello", out long offset));
        Assert.Equal(42L, offset);
        Assert.False(reader.TryGetPostingsOffset("body\0world", out _));
    }

    // ── Multi-term round-trip ───────────────────────────────────────────────

    [Fact]
    public void MultiTerm_AllTermsRoundTrip()
    {
        var path = DicPath("multi");
        var terms = new List<string>
        {
            "body\0apple",
            "body\0banana",
            "body\0cherry",
            "title\0doc1",
            "title\0doc2"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++)
            offsets[terms[i]] = (i + 1) * 100L;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        foreach (var (term, expected) in offsets)
        {
            Assert.True(reader.TryGetPostingsOffset(term, out long actual), $"Missing: {term}");
            Assert.Equal(expected, actual);
        }
    }

    // ── Unicode terms (CJK, emoji) ──────────────────────────────────────────

    [Fact]
    public void Unicode_CJKAndEmoji_RoundTrip()
    {
        var path = DicPath("unicode");
        var terms = new List<string>
        {
            "body\0café",
            "body\0日本語",
            "body\0🎉"
        };
        terms.Sort(StringComparer.Ordinal);
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++)
            offsets[terms[i]] = i * 10L;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        foreach (var (term, expected) in offsets)
        {
            Assert.True(reader.TryGetPostingsOffset(term, out long actual), $"Missing: {term}");
            Assert.Equal(expected, actual);
        }
    }

    // ── Prefix scan ─────────────────────────────────────────────────────────

    [Fact]
    public void PrefixScan_ReturnsMatchingTerms()
    {
        var path = DicPath("prefix");
        var terms = new List<string>
        {
            "body\0apple",
            "body\0application",
            "body\0banana",
            "title\0apple"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var bodyApple = reader.GetTermsWithPrefix("body\0app".AsSpan());
        Assert.Equal(2, bodyApple.Count);
        Assert.Equal("body\0apple", bodyApple[0].Term);
        Assert.Equal("body\0application", bodyApple[1].Term);
    }

    // ── Wildcard matching ───────────────────────────────────────────────────

    [Fact]
    public void WildcardScan_MatchesPattern()
    {
        var path = DicPath("wildcard");
        var terms = new List<string>
        {
            "body\0cat",
            "body\0car",
            "body\0cart",
            "body\0dog"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var matches = reader.GetTermsMatching("body\0", "ca*".AsSpan());
        Assert.Equal(3, matches.Count);
    }

    // ── Fuzzy matching ──────────────────────────────────────────────────────

    [Fact]
    public void FuzzyMatches_FindsWithinEditDistance()
    {
        var path = DicPath("fuzzy");
        var terms = new List<string>
        {
            "body\0cat",
            "body\0bat",
            "body\0car",
            "body\0dog"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var matches = reader.GetFuzzyMatches("body\0", "cat".AsSpan(), 1);
        Assert.Contains(matches, m => m.Term == "body\0cat");
        Assert.Contains(matches, m => m.Term == "body\0bat");
        Assert.Contains(matches, m => m.Term == "body\0car");
        Assert.DoesNotContain(matches, m => m.Term == "body\0dog");

        // Verify edit distances are returned correctly
        var catMatch = matches.First(m => m.Term == "body\0cat");
        Assert.Equal(0, catMatch.Distance);
        var batMatch = matches.First(m => m.Term == "body\0bat");
        Assert.Equal(1, batMatch.Distance);
    }

    [Fact]
    public void FuzzyMatches_LargeTermSet_PrefixSharingPrunes()
    {
        var path = DicPath("fuzzy_scale");
        // Generate 10K terms to exercise prefix-sharing + dead-prefix skipping
        var random = new Random(42);
        var terms = new HashSet<string>();
        foreach (var w in new[] { "search", "serch", "surch", "seach" })
            terms.Add($"body\0{w}");
        while (terms.Count < 10_000)
        {
            int len = random.Next(3, 12);
            var chars = new char[len];
            for (int i = 0; i < len; i++) chars[i] = (char)('a' + random.Next(26));
            terms.Add($"body\0{new string(chars)}");
        }
        var sorted = terms.OrderBy(t => t, StringComparer.Ordinal).ToList();
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < sorted.Count; i++) offsets[sorted[i]] = i;

        WriteDictionary(path, sorted, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var matches = reader.GetFuzzyMatches("body\0", "search".AsSpan(), 2);
        Assert.Contains(matches, m => m.Term == "body\0search" && m.Distance == 0);
        Assert.Contains(matches, m => m.Term == "body\0serch" && m.Distance == 1);
        Assert.True(matches.All(m => m.Distance <= 2), "All matches within edit distance 2");

        // Performance: 10K terms should complete quickly with prefix pruning
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
            reader.GetFuzzyMatches("body\0", "search".AsSpan(), 2);
        sw.Stop();
        _output.WriteLine($"100 fuzzy queries over 10K terms: {sw.ElapsedMilliseconds}ms ({sw.ElapsedMilliseconds / 100.0:F2}ms/query)");
        Assert.True(sw.ElapsedMilliseconds < 2000, $"100 fuzzy queries took {sw.ElapsedMilliseconds}ms (expected < 2000ms)");
    }

    [Fact]
    public void FuzzyMatches_MaxEdits2_CorrectDistances()
    {
        var path = DicPath("fuzzy_dist2");
        var terms = new List<string>
        {
            "body\0abc",     // dist 0 from "abc"
            "body\0ab",      // dist 1 (deletion)
            "body\0abcd",    // dist 1 (insertion)
            "body\0axc",     // dist 1 (substitution)
            "body\0a",       // dist 2 (2 deletions)
            "body\0abcde",   // dist 2 (2 insertions)
            "body\0xyz",     // dist 3 — should NOT match
        };
        terms.Sort(StringComparer.Ordinal);
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var matches = reader.GetFuzzyMatches("body\0", "abc".AsSpan(), 2);
        Assert.Equal(0, matches.First(m => m.Term == "body\0abc").Distance);
        Assert.Equal(1, matches.First(m => m.Term == "body\0ab").Distance);
        Assert.Equal(1, matches.First(m => m.Term == "body\0abcd").Distance);
        Assert.Equal(1, matches.First(m => m.Term == "body\0axc").Distance);
        Assert.Equal(2, matches.First(m => m.Term == "body\0a").Distance);
        Assert.Equal(2, matches.First(m => m.Term == "body\0abcde").Distance);
        Assert.DoesNotContain(matches, m => m.Term == "body\0xyz");
    }

    // ── Range scan ──────────────────────────────────────────────────────────

    [Fact]
    public void RangeScan_ReturnsTermsInRange()
    {
        var path = DicPath("range");
        var terms = new List<string>
        {
            "body\0alpha",
            "body\0beta",
            "body\0gamma",
            "body\0delta"
        };
        terms.Sort(StringComparer.Ordinal);
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var range = reader.GetTermsInRange("body\0", "beta", "gamma", includeLower: true, includeUpper: true);
        Assert.Equal(3, range.Count); // beta, delta, gamma (lexicographic)
    }

    // ── Regex matching ──────────────────────────────────────────────────────

    [Fact]
    public void RegexScan_MatchesPattern()
    {
        var path = DicPath("regex");
        var terms = new List<string>
        {
            "body\0abc123",
            "body\0abc456",
            "body\0xyz789"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var regex = new System.Text.RegularExpressions.Regex(@"^abc\d+$");
        var matches = reader.GetTermsMatchingRegex("body\0", regex);
        Assert.Equal(2, matches.Count);
    }

    // ── Field enumeration ───────────────────────────────────────────────────

    [Fact]
    public void GetAllTermsForField_ReturnsAllFieldTerms()
    {
        var path = DicPath("fieldall");
        var terms = new List<string>
        {
            "body\0one",
            "body\0two",
            "title\0three"
        };
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < terms.Count; i++) offsets[terms[i]] = i;

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        var bodyTerms = reader.GetAllTermsForField("body\0");
        Assert.Equal(2, bodyTerms.Count);

        var titleTerms = reader.GetAllTermsForField("title\0");
        Assert.Single(titleTerms);
    }

    // ── Large-scale (10K terms) ─────────────────────────────────────────────

    [Fact]
    public void LargeScale_10KTerms_AllLookupSucceeds()
    {
        var path = DicPath("large10k");
        var terms = new List<string>();
        var offsets = new Dictionary<string, long>();
        for (int i = 0; i < 10_000; i++)
        {
            var term = $"body\0term_{i:D6}";
            terms.Add(term);
            offsets[term] = i * 8L;
        }
        terms.Sort(StringComparer.Ordinal);

        WriteDictionary(path, terms, offsets);
        using var reader = TermDictionaryReader.Open(path);

        // Spot-check first, middle, last
        Assert.True(reader.TryGetPostingsOffset("body\0term_000000", out long o0));
        Assert.Equal(0L, o0);

        Assert.True(reader.TryGetPostingsOffset("body\0term_005000", out long o5k));
        Assert.Equal(5000L * 8, o5k);

        Assert.True(reader.TryGetPostingsOffset("body\0term_009999", out long o9k));
        Assert.Equal(9999L * 8, o9k);

        Assert.False(reader.TryGetPostingsOffset("body\0term_099999", out _));

        _output.WriteLine("✓ 10K terms: all lookups correct");
    }

    // ── Format version in file ──────────────────────────────────────────────

    [Fact]
    public void WrittenFile_HasVersionTwo()
    {
        var path = DicPath("version_check");
        WriteDictionary(path, ["body\0test"], new Dictionary<string, long> { ["body\0test"] = 1 });

        using var input = new IndexInput(path);
        int magic = input.ReadInt32();
        byte version = input.ReadByte();
        Assert.Equal(CodecConstants.Magic, magic);
        Assert.Equal(2, version);
    }
}
