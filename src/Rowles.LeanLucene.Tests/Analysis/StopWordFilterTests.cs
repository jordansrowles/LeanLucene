using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public class StopWordFilterTests
{
    private readonly StopWordFilter _filter = new();

    [Fact]
    public void Apply_AllStopWords_ReturnsEmptyList()
    {
        var tokens = new List<Token>
        {
            new("the", 0, 3),
            new("is", 4, 6),
            new("a", 7, 8),
        };

        _filter.Apply(tokens);

        Assert.Empty(tokens);
    }

    [Fact]
    public void Apply_NoStopWords_ReturnsAllTokens()
    {
        var tokens = new List<Token>
        {
            new("quick", 0, 5),
            new("fox", 6, 9),
        };

        _filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void Apply_MixedTokens_RemovesOnlyStopWords()
    {
        var tokens = new List<Token>
        {
            new("the", 0, 3),
            new("quick", 4, 9),
            new("brown", 10, 15),
            new("fox", 16, 19),
        };

        _filter.Apply(tokens);

        Assert.Equal(3, tokens.Count);
        Assert.Equal("quick", tokens[0].Text);
        Assert.Equal("brown", tokens[1].Text);
        Assert.Equal("fox", tokens[2].Text);
    }
}
