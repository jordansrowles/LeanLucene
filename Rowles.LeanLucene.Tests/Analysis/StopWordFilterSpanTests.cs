using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

public class StopWordFilterSpanTests
{
    private readonly StopWordFilter _filter = new();

    [Theory]
    [InlineData("the", true)]
    [InlineData("and", true)]
    [InlineData("is", true)]
    [InlineData("THE", true)]
    [InlineData("hello", false)]
    [InlineData("world", false)]
    public void IsStopWord_Span_MatchesStringOverload(string word, bool expected)
    {
        Assert.Equal(expected, _filter.IsStopWord(word.AsSpan()));
        Assert.Equal(expected, _filter.IsStopWord(word));
    }

    [Fact]
    public void IsStopWord_EmptySpan_ReturnsFalse()
    {
        Assert.False(_filter.IsStopWord(ReadOnlySpan<char>.Empty));
    }
}
