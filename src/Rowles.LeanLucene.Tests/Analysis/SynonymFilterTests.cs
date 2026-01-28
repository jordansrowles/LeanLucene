using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class SynonymFilterTests
{
    private static SynonymFilter Build(params (string src, string[] syns)[] pairs)
    {
        var dict = pairs.ToDictionary(p => p.src, p => p.syns);
        return new SynonymFilter(dict);
    }

    [Fact]
    public void Apply_InsertsAllSynonymTokens()
    {
        var filter = Build(("car", ["automobile", "vehicle"]));
        var tokens = new List<Token>
        {
            new("buy", 0, 3),
            new("car", 4, 7),
        };

        filter.Apply(tokens);

        Assert.Equal(4, tokens.Count); // buy, car, automobile, vehicle
        Assert.Equal("car", tokens[1].Text);
        Assert.Equal("automobile", tokens[2].Text);
        Assert.Equal("vehicle", tokens[3].Text);
    }

    [Fact]
    public void Apply_SynonymTokensShareSourceOffsets()
    {
        var filter = Build(("quick", ["fast"]));
        var tokens = new List<Token> { new("quick", 0, 5) };
        filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
        Assert.Equal(tokens[0].StartOffset, tokens[1].StartOffset);
        Assert.Equal(tokens[0].EndOffset, tokens[1].EndOffset);
    }

    [Fact]
    public void Apply_NoSynonymMatch_Unchanged()
    {
        var filter = Build(("quick", ["fast"]));
        var tokens = new List<Token> { new("slow", 0, 4) };
        filter.Apply(tokens);

        Assert.Single(tokens);
        Assert.Equal("slow", tokens[0].Text);
    }

    [Fact]
    public void Apply_CaseInsensitiveMatch()
    {
        var filter = Build(("QUICK", ["fast"]));
        var tokens = new List<Token> { new("quick", 0, 5) };
        filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("fast", tokens[1].Text);
    }

    [Fact]
    public void Apply_EmptyInput_NoOp()
    {
        var filter = Build(("a", ["b"]));
        var tokens = new List<Token>();
        filter.Apply(tokens);
        Assert.Empty(tokens);
    }
}
