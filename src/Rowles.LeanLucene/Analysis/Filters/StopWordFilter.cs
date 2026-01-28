using System.Collections.Frozen;

namespace Rowles.LeanLucene.Analysis.Filters;

/// <summary>
/// Removes common English stop words from a token list using a frozen set
/// for fast, allocation-free lookups.
/// </summary>
public sealed class StopWordFilter : ITokenFilter
{
    /// <summary>Built-in English stop word list.</summary>
    public static readonly IReadOnlyList<string> DefaultStopWords =
    [
        "a", "an", "and", "are", "as", "at", "be", "but", "by",
        "for", "if", "in", "into", "is", "it", "no", "of",
        "on", "or", "such", "that", "the", "their", "then", "there",
        "these", "they", "this", "to", "was", "will", "with", "through"
    ];

    private readonly FrozenSet<string> _stopWords;

    public StopWordFilter() : this(null) { }

    public StopWordFilter(IEnumerable<string>? customStopWords)
    {
        _stopWords = (customStopWords ?? DefaultStopWords).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Returns true if the given term is a stop word.</summary>
    internal bool IsStopWord(string term) => _stopWords.Contains(term);

    /// <summary>Returns true if the given term span is a stop word (zero-alloc).</summary>
    internal bool IsStopWord(ReadOnlySpan<char> term)
        => _stopWords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(term);

    public void Apply(List<Token> tokens)
    {
        tokens.RemoveAll(t => _stopWords.Contains(t.Text));
    }
}
