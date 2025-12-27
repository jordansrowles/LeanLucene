using System.Collections.Frozen;

namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Removes common English stop words from a token list using a frozen set
/// for fast, allocation-free lookups.
/// </summary>
public sealed class StopWordFilter
{
    private static readonly FrozenSet<string> StopWords = new[]
    {
        "a", "an", "and", "are", "as", "at", "be", "but", "by",
        "for", "if", "in", "into", "is", "it", "no", "of",
        "on", "or", "such", "that", "the", "their", "then", "there",
        "these", "they", "this", "to", "was", "will", "with", "through"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns true if the given term is a stop word.</summary>
    public bool IsStopWord(string term) => StopWords.Contains(term);

    public List<Token> Apply(List<Token> tokens)
    {
        tokens.RemoveAll(t => StopWords.Contains(t.Text));
        return tokens;
    }
}
