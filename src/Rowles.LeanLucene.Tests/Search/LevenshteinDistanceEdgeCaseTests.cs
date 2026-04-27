using Rowles.LeanLucene.Search;
using Rowles.LeanLucene.Search.Simd;
using Rowles.LeanLucene.Search.Parsing;
using Rowles.LeanLucene.Search.Highlighting;
using Xunit.Abstractions;

namespace Rowles.LeanLucene.Tests.Search;

/// <summary>
/// Direct unit tests for LevenshteinDistance edge cases not covered by existing tests.
/// </summary>
[Trait("Category", "Search")]
[Trait("Category", "UnitTest")]
public sealed class LevenshteinDistanceEdgeCaseTests
{
    private readonly ITestOutputHelper _output;

    public LevenshteinDistanceEdgeCaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BothStringsEmpty_ReturnsZero()
    {
        int dist = LevenshteinDistance.Compute("", "");
        _output.WriteLine($"Levenshtein('', '') = {dist}");
        Assert.Equal(0, dist);
    }

    [Fact]
    public void LargeStrings_ExercisesHeapFallbackPath()
    {
        // Strings longer than 256 chars exercise the `new int[]` fallback past stackalloc
        var a = new string('a', 300);
        var b = new string('a', 300);
        b = b[..299] + "b"; // change last char

        int dist = LevenshteinDistance.Compute(a, b);
        _output.WriteLine($"Levenshtein(300-char 'a's, 299 'a's + 'b') = {dist}");
        Assert.Equal(1, dist);
    }

    [Fact]
    public void AllDifferentCharacters_ReturnsMaxLength()
    {
        // "abc" vs "xyz" — all 3 chars different, distance = 3
        int dist = LevenshteinDistance.Compute("abc", "xyz");
        _output.WriteLine($"Levenshtein('abc', 'xyz') = {dist}");
        Assert.Equal(3, dist);
    }

    [Fact]
    public void AllDifferent_LargerStrings_ReturnsLongerLength()
    {
        var a = "abcde";
        var b = "vwxyz";
        int dist = LevenshteinDistance.Compute(a, b);
        _output.WriteLine($"Levenshtein('{a}', '{b}') = {dist}");
        Assert.Equal(5, dist);
    }

    [Fact]
    public void DifferentLengths_AllDifferent_ReturnsMaxOfLengths()
    {
        int dist = LevenshteinDistance.Compute("ab", "xyz");
        _output.WriteLine($"Levenshtein('ab', 'xyz') = {dist}");
        // "ab" → "xyz": need 2 substitutions + 1 insertion = 3
        Assert.Equal(3, dist);
    }

    [Fact]
    public void SymmetricProperty_OrderDoesNotMatter()
    {
        int d1 = LevenshteinDistance.Compute("kitten", "sitting");
        int d2 = LevenshteinDistance.Compute("sitting", "kitten");
        _output.WriteLine($"Levenshtein('kitten','sitting') = {d1}, reverse = {d2}");
        Assert.Equal(d1, d2);
    }

    [Fact]
    public void SingleCharStrings_OneSubstitution()
    {
        int dist = LevenshteinDistance.Compute("a", "b");
        _output.WriteLine($"Levenshtein('a', 'b') = {dist}");
        Assert.Equal(1, dist);
    }

    [Fact]
    public void OnlyInsertions_ReturnsLengthDifference()
    {
        int dist = LevenshteinDistance.Compute("abc", "abcdef");
        _output.WriteLine($"Levenshtein('abc', 'abcdef') = {dist}");
        Assert.Equal(3, dist);
    }
}
