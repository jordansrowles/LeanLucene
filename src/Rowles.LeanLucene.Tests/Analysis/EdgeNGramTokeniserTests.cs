using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class EdgeNGramTokeniserTests
{
    [Fact]
    public void EdgeNGram_EmitsEdgeNGrams()
    {
        var tok = new EdgeNGramTokeniser(1, 3);
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
        var tok = new EdgeNGramTokeniser(1, 2);
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
        var tok = new EdgeNGramTokeniser(1, 3);
        var tokens = tok.Tokenise(string.Empty.AsSpan());
        Assert.Empty(tokens);
    }
}
