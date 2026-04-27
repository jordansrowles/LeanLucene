using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Analysers;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class NGramTokeniserTests
{
    [Fact]
    public void NGram_SingleCharTokens()
    {
        var tok = new NGramTokeniser(1, 1);
        var tokens = tok.Tokenise("abc".AsSpan());
        Assert.Equal(3, tokens.Count);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal("c", tokens[2].Text);
    }

    [Fact]
    public void NGram_BigramsAndTrigrams()
    {
        var tok = new NGramTokeniser(2, 3);
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
        var tok = new NGramTokeniser(1, 2);
        var tokens = tok.Tokenise(string.Empty.AsSpan());
        Assert.Empty(tokens);
    }

    [Fact]
    public void NGram_InputShorterThanMinGram_ReturnsEmpty()
    {
        var tok = new NGramTokeniser(3, 4);
        var tokens = tok.Tokenise("ab".AsSpan());
        Assert.Empty(tokens);
    }

    [Fact]
    public void NGram_OffsetValues_AreCorrect()
    {
        var tok = new NGramTokeniser(2, 2);
        var tokens = tok.Tokenise("xyz".AsSpan());
        Assert.Equal(0, tokens[0].StartOffset);
        Assert.Equal(2, tokens[0].EndOffset);
        Assert.Equal(1, tokens[1].StartOffset);
        Assert.Equal(3, tokens[1].EndOffset);
    }
}
