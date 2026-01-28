using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class AnalyserPipelineTests
{
    [Fact]
    public void Analyser_ComposesTokeniserAndFilters()
    {
        var analyser = new Analyser(
            new Tokeniser(),
            new LowercaseFilter(),
            new StopWordFilter());

        var tokens = analyser.Analyse("The Quick Brown Fox Jumps".AsSpan());
        var texts = tokens.Select(t => t.Text).ToList();

        Assert.DoesNotContain("the", texts); // stop word removed
        Assert.Contains("quick", texts);     // lowercased
        Assert.Contains("brown", texts);
        Assert.Contains("fox", texts);
        Assert.Contains("jumps", texts);
    }

    [Fact]
    public void Analyser_WithStemmer_StemsTokens()
    {
        var analyser = new Analyser(
            new Tokeniser(),
            new LowercaseFilter(),
            new PorterStemmerFilter());

        var tokens = analyser.Analyse("Running Quickly".AsSpan());
        var texts = tokens.Select(t => t.Text).ToList();

        Assert.Contains("run", texts);
    }

    [Fact]
    public void StandardAnalyser_ImplementsIAnalyser()
    {
        IAnalyser analyser = new StandardAnalyser();
        var tokens = analyser.Analyse("Hello World".AsSpan());
        Assert.Equal(2, tokens.Count);
        Assert.Equal("hello", tokens[0].Text);
        Assert.Equal("world", tokens[1].Text);
    }

    [Fact]
    public void ITokenFilter_StopWordFilter_Implements()
    {
        ITokenFilter filter = new StopWordFilter();
        var tokens = new List<Token> { new("the", 0, 3), new("cat", 4, 7) };
        filter.Apply(tokens);
        Assert.Single(tokens);
        Assert.Equal("cat", tokens[0].Text);
    }

    [Fact]
    public void ITokenFilter_LowercaseFilter_Implements()
    {
        ITokenFilter filter = new LowercaseFilter();
        var tokens = new List<Token> { new("HELLO", 0, 5) };
        filter.Apply(tokens);
        Assert.Equal("hello", tokens[0].Text);
    }

    [Fact]
    public void ITokeniser_Tokeniser_Implements()
    {
        ITokeniser tokeniser = new Tokeniser();
        var tokens = tokeniser.Tokenise("one two three".AsSpan());
        Assert.Equal(3, tokens.Count);
    }
}
