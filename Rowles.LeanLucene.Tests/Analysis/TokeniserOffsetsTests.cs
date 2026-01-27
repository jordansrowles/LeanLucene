using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Tokenisers;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public class TokeniserOffsetsTests
{
    private readonly Tokeniser _tokeniser = new();

    [Fact]
    public void TokeniseOffsets_Sentence_ReturnsCorrectOffsets()
    {
        var offsets = new List<(int Start, int End)>();
        _tokeniser.TokeniseOffsets("The quick brown fox", offsets);

        Assert.Equal(4, offsets.Count);
        Assert.Equal((0, 3), offsets[0]);
        Assert.Equal((4, 9), offsets[1]);
        Assert.Equal((10, 15), offsets[2]);
        Assert.Equal((16, 19), offsets[3]);
    }

    [Fact]
    public void TokeniseOffsets_EmptyInput_ReturnsEmptyList()
    {
        var offsets = new List<(int Start, int End)>();
        _tokeniser.TokeniseOffsets(ReadOnlySpan<char>.Empty, offsets);

        Assert.Empty(offsets);
    }

    [Fact]
    public void TokeniseOffsets_OnlyPunctuation_ReturnsEmptyList()
    {
        var offsets = new List<(int Start, int End)>();
        _tokeniser.TokeniseOffsets("  , . ! ", offsets);

        Assert.Empty(offsets);
    }

    [Fact]
    public void TokeniseOffsets_ClearsExistingEntries()
    {
        var offsets = new List<(int Start, int End)> { (99, 100) };
        _tokeniser.TokeniseOffsets("hello", offsets);

        Assert.Single(offsets);
        Assert.Equal((0, 5), offsets[0]);
    }

    [Fact]
    public void TokeniseOffsets_MatchesTokenise()
    {
        const string input = "Hello, world! This is a test.";
        var tokens = _tokeniser.Tokenise(input);
        var offsets = new List<(int Start, int End)>();
        _tokeniser.TokeniseOffsets(input, offsets);

        Assert.Equal(tokens.Count, offsets.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            Assert.Equal(tokens[i].StartOffset, offsets[i].Start);
            Assert.Equal(tokens[i].EndOffset, offsets[i].End);
        }
    }
}
