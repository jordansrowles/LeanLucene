using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public class StandardAnalyserTests
{
    private readonly StandardAnalyser _analyser = new();

    [Fact]
    public void Analyse_AllStopWords_ReturnsOnlyNonStopWords()
    {
        var result = _analyser.Analyse("to be or not to be");

        Assert.Single(result);
        Assert.Equal("not", result[0].Text);
    }

    [Fact]
    public void Analyse_MixedCaseWithStopWords_ReturnsLowercasedNonStopWords()
    {
        var result = _analyser.Analyse("Running quickly through THE forest");

        Assert.Equal(3, result.Count);
        Assert.Equal("running", result[0].Text);
        Assert.Equal("quickly", result[1].Text);
        Assert.Equal("forest", result[2].Text);
    }

    [Fact]
    public void Analyse_EmptyInput_ReturnsEmptyList()
    {
        var result = _analyser.Analyse(ReadOnlySpan<char>.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Analyse_SingleNonStopWord_ReturnsSingleLowercasedToken()
    {
        var result = _analyser.Analyse("Hello");

        Assert.Single(result);
        Assert.Equal("hello", result[0].Text);
    }
}
