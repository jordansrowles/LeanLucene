using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

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

        var result = _filter.Apply(tokens);

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_NoStopWords_ReturnsAllTokens()
    {
        var tokens = new List<Token>
        {
            new("quick", 0, 5),
            new("fox", 6, 9),
        };

        var result = _filter.Apply(tokens);

        Assert.Equal(2, result.Count);
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

        var result = _filter.Apply(tokens);

        Assert.Equal(3, result.Count);
        Assert.Equal("quick", result[0].Text);
        Assert.Equal("brown", result[1].Text);
        Assert.Equal("fox", result[2].Text);
    }
}
