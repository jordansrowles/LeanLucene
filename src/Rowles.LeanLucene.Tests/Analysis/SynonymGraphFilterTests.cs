using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Analysis.Filters;

namespace Rowles.LeanLucene.Tests.Analysis;

public class SynonymGraphFilterTests
{
    [Fact]
    public void SingleTokenSynonym_ExpandsCorrectly()
    {
        var map = new SynonymMap();
        map.Add("quick", ["fast", "rapid"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("quick", 0, 5),
            new("fox", 6, 9)
        };

        filter.Apply(tokens);

        Assert.Equal(4, tokens.Count); // quick + fast + rapid + fox
        Assert.Equal("quick", tokens[0].Text);
        Assert.Equal("fast", tokens[1].Text);
        Assert.Equal("rapid", tokens[2].Text);
        Assert.Equal("fox", tokens[3].Text);

        // Synonym tokens share the same offset as source
        Assert.Equal(0, tokens[1].StartOffset);
        Assert.Equal(5, tokens[1].EndOffset);
    }

    [Fact]
    public void MultiTokenSynonym_LongestMatch()
    {
        var map = new SynonymMap();
        map.Add("new york", ["nyc"]);
        map.Add("new york city", ["nyc", "big apple"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("new", 0, 3),
            new("york", 4, 8),
            new("city", 9, 13),
            new("park", 14, 18)
        };

        filter.Apply(tokens);

        // Should match "new york city" (3 tokens) → keep originals + add synonyms
        var texts = tokens.Select(t => t.Text).ToList();
        Assert.Contains("nyc", texts);
        Assert.Contains("big apple", texts);
        Assert.Contains("park", texts);
    }

    [Fact]
    public void NoMatch_PassesThrough()
    {
        var map = new SynonymMap();
        map.Add("quick", ["fast"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("slow", 0, 4),
            new("fox", 5, 8)
        };

        filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("slow", tokens[0].Text);
        Assert.Equal("fox", tokens[1].Text);
    }

    [Fact]
    public void EmptyTokenList_NoError()
    {
        var map = new SynonymMap();
        map.Add("test", ["exam"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>();

        filter.Apply(tokens);

        Assert.Empty(tokens);
    }

    [Fact]
    public void CaseInsensitive_MatchesLowercase()
    {
        var map = new SynonymMap();
        map.Add("Quick", ["fast"]); // Added with mixed case

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("quick", 0, 5) // lowercase in token stream
        };

        filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("quick", tokens[0].Text);
        Assert.Equal("fast", tokens[1].Text);
    }

    [Fact]
    public void MultipleSynonymsInSequence()
    {
        var map = new SynonymMap();
        map.Add("big", ["large"]);
        map.Add("cat", ["feline"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("big", 0, 3),
            new("cat", 4, 7)
        };

        filter.Apply(tokens);

        var texts = tokens.Select(t => t.Text).ToList();
        Assert.Contains("large", texts);
        Assert.Contains("feline", texts);
    }

    [Fact]
    public void SynonymMap_TrieStructure_PartialMatchNotExpanded()
    {
        var map = new SynonymMap();
        map.Add("ice cream", ["gelato"]);
        // "ice" alone should NOT match

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("ice", 0, 3),
            new("cold", 4, 8) // not "cream", so no match
        };

        filter.Apply(tokens);

        Assert.Equal(2, tokens.Count);
        Assert.Equal("ice", tokens[0].Text);
        Assert.Equal("cold", tokens[1].Text);
    }

    [Fact]
    public void OriginalTokensPreserved_WithSynonyms()
    {
        var map = new SynonymMap();
        map.Add("usa", ["united states", "america"]);

        var filter = new SynonymGraphFilter(map);
        var tokens = new List<Token>
        {
            new("usa", 0, 3)
        };

        filter.Apply(tokens);

        // Original "usa" should still be present
        Assert.Equal("usa", tokens[0].Text);
        Assert.Equal(3, tokens.Count); // usa + united states + america
    }
}
