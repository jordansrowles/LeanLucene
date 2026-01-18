using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public sealed class AnalysisTests
{
    [Fact]
    public void Tokeniser_BasicSentence_ProducesCorrectTokens()
    {
        var tokeniser = new Tokeniser();
        var input = "The quick brown fox".AsSpan();
        var tokens = tokeniser.Tokenise(input);

        var expected = new[] { "The", "quick", "brown", "fox" };
        Assert.Equal(expected.Length, tokens.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], tokens[i].Text.ToString());
            // Verify offsets point back to original text
            Assert.Equal(expected[i],
                input.Slice(tokens[i].StartOffset, tokens[i].EndOffset - tokens[i].StartOffset).ToString());
        }
    }

    [Fact]
    public void Tokeniser_Punctuation_IsExcludedFromTokens()
    {
        var tokeniser = new Tokeniser();
        var tokens = tokeniser.Tokenise("hello, world!".AsSpan());

        Assert.Equal(2, tokens.Count);
        Assert.Equal("hello", tokens[0].Text.ToString());
        Assert.Equal("world", tokens[1].Text.ToString());
    }

    [Fact]
    public void LowercaseFilter_MutatesInPlace_NoAllocation()
    {
        var filter = new LowercaseFilter();
        char[] buffer = "Hello WORLD FoO".ToCharArray();
        filter.Apply(buffer.AsSpan());
        Assert.Equal("hello world foo", new string(buffer));
    }

    [Fact]
    public void StopWordFilter_RemovesCommonWords()
    {
        // Analyse "to be or not to be" — only "not" should survive.
        var analyser = new StandardAnalyser();
        var tokens = analyser.Analyse("to be or not to be".AsSpan());

        Assert.Single(tokens);
        Assert.Equal("not", tokens[0].Text.ToString());
    }

    [Fact]
    public void StandardAnalyser_EndToEnd_LowercasesTokenisesFilters()
    {
        var analyser = new StandardAnalyser();
        var tokens = analyser.Analyse("Running quickly through THE forest".AsSpan());

        var expected = new[] { "running", "quickly", "forest" };
        Assert.Equal(expected.Length, tokens.Count);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], tokens[i].Text.ToString());
    }
}
