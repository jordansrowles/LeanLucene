using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Analysers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class PorterStemmerFilterTests
{
    private readonly PorterStemmerFilter _filter = new();

    private string Stem(string word)
    {
        var tokens = new List<Token> { new(word, 0, word.Length) };
        _filter.Apply(tokens);
        return tokens[0].Text;
    }

    [Theory]
    [InlineData("caresses", "caress")]
    [InlineData("ponies", "poni")]
    [InlineData("cats", "cat")]
    [InlineData("feed", "feed")]
    [InlineData("agreed", "agre")]
    [InlineData("plastered", "plaster")]
    [InlineData("motoring", "motor")]
    [InlineData("sing", "sing")]
    public void Stem_ClassicPorterTestVectors(string input, string expected)
    {
        Assert.Equal(expected, Stem(input));
    }

    [Fact]
    public void Apply_EmptyList_NoChanges()
    {
        var tokens = new List<Token>();
        _filter.Apply(tokens);
        Assert.Empty(tokens);
    }

    [Fact]
    public void Apply_ShortWords_Unchanged()
    {
        // Words with 2 or fewer chars should not be stemmed
        Assert.Equal("a", Stem("a"));
        Assert.Equal("an", Stem("an"));
    }

    [Fact]
    public void Apply_MultipleTokens_AllStemmed()
    {
        var tokens = new List<Token>
        {
            new("running", 0, 7),
            new("cats", 8, 12),
            new("happily", 13, 20)
        };
        _filter.Apply(tokens);

        Assert.Equal("run", tokens[0].Text);
        Assert.Equal("cat", tokens[1].Text);
        Assert.Equal("happili", tokens[2].Text);
    }

    [Fact]
    public void Apply_AlreadyStemmed_TokenUnchanged()
    {
        var token = new Token("run", 0, 3);
        var tokens = new List<Token> { token };
        _filter.Apply(tokens);
        // Token should still be "run" — no double-stemming
        Assert.Equal("run", tokens[0].Text);
    }
}
