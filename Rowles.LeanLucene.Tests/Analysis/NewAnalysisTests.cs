using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

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

public sealed class NGramTokenizerTests
{
    [Fact]
    public void NGram_SingleCharTokens()
    {
        var tok = new NGramTokenizer(1, 1);
        var tokens = tok.Tokenise("abc".AsSpan());
        Assert.Equal(3, tokens.Count);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal("c", tokens[2].Text);
    }

    [Fact]
    public void NGram_BigramsAndTrigrams()
    {
        var tok = new NGramTokenizer(2, 3);
        var tokens = tok.Tokenise("abcd".AsSpan());
        // Expected: ab, abc, bc, bcd, cd
        var texts = tokens.Select(t => t.Text).ToList();
        Assert.Contains("ab", texts);
        Assert.Contains("abc", texts);
        Assert.Contains("bc", texts);
        Assert.Contains("bcd", texts);
        Assert.Contains("cd", texts);
    }

    [Fact]
    public void NGram_EmptyInput_ReturnsEmptyList()
    {
        var tok = new NGramTokenizer(1, 2);
        var tokens = tok.Tokenise(string.Empty.AsSpan());
        Assert.Empty(tokens);
    }

    [Fact]
    public void NGram_InputShorterThanMinGram_ReturnsEmpty()
    {
        var tok = new NGramTokenizer(3, 4);
        var tokens = tok.Tokenise("ab".AsSpan());
        Assert.Empty(tokens);
    }

    [Fact]
    public void NGram_OffsetValues_AreCorrect()
    {
        var tok = new NGramTokenizer(2, 2);
        var tokens = tok.Tokenise("xyz".AsSpan());
        Assert.Equal(0, tokens[0].StartOffset);
        Assert.Equal(2, tokens[0].EndOffset);
        Assert.Equal(1, tokens[1].StartOffset);
        Assert.Equal(3, tokens[1].EndOffset);
    }
}

public sealed class EdgeNGramTokenizerTests
{
    [Fact]
    public void EdgeNGram_EmitsEdgeNGrams()
    {
        var tok = new EdgeNGramTokenizer(1, 3);
        var tokens = tok.Tokenise("hello".AsSpan());
        var texts = tokens.Select(t => t.Text).ToList();
        Assert.Contains("h", texts);
        Assert.Contains("he", texts);
        Assert.Contains("hel", texts);
        // Should NOT contain "hell" (max=3) or "ello"
        Assert.DoesNotContain("hell", texts);
        Assert.DoesNotContain("ello", texts);
    }

    [Fact]
    public void EdgeNGram_MultipleWords_EachWordHasEdgeNGrams()
    {
        var tok = new EdgeNGramTokenizer(1, 2);
        var tokens = tok.Tokenise("hi me".AsSpan());
        var texts = tokens.Select(t => t.Text).ToList();
        Assert.Contains("h", texts);
        Assert.Contains("hi", texts);
        Assert.Contains("m", texts);
        Assert.Contains("me", texts);
    }

    [Fact]
    public void EdgeNGram_EmptyInput_ReturnsEmpty()
    {
        var tok = new EdgeNGramTokenizer(1, 3);
        var tokens = tok.Tokenise(string.Empty.AsSpan());
        Assert.Empty(tokens);
    }
}
