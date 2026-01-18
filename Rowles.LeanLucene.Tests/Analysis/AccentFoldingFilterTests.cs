using Rowles.LeanLucene.Analysis;

namespace Rowles.LeanLucene.Tests.Analysis;

[Trait("Category", "Analysis")]
public class AccentFoldingFilterTests
{
    private readonly AccentFoldingFilter _filter = new();

    [Fact]
    public void Apply_AccentedTokens_FoldsToAscii()
    {
        // Arrange
        var tokens = new List<Token>
        {
            new("café", 0, 4),
            new("naïve", 5, 10),
            new("résumé", 11, 17)
        };

        // Act
        _filter.Apply(tokens);

        // Assert
        Assert.Equal("cafe", tokens[0].Text);
        Assert.Equal("naive", tokens[1].Text);
        Assert.Equal("resume", tokens[2].Text);
    }

    [Fact]
    public void Apply_AsciiTokens_UnchangedReferences()
    {
        // Arrange
        var original = "hello";
        var tokens = new List<Token> { new(original, 0, 5) };

        // Act
        _filter.Apply(tokens);

        // Assert — original reference returned when no change needed
        Assert.Same(original, tokens[0].Text);
    }

    [Fact]
    public void Apply_EmptyList_NoError()
    {
        var tokens = new List<Token>();
        _filter.Apply(tokens);
        Assert.Empty(tokens);
    }

    [Theory]
    [InlineData("über", "uber")]
    [InlineData("señor", "senor")]
    [InlineData("Ångström", "Angstrom")]
    public void Fold_VariousDiacritics_FoldsCorrectly(string input, string expected)
    {
        var result = AccentFoldingFilter.Fold(input);
        Assert.Equal(expected, result);
    }
}
