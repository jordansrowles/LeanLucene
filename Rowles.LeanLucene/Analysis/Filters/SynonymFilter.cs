namespace Rowles.LeanLucene.Analysis.Filters;

/// <summary>
/// Expands tokens with their configured synonyms, inserting synonym tokens at the
/// same position as the source token so that phrase queries still work correctly.
/// </summary>
/// <remarks>
/// Consider using <see cref="SynonymGraphFilter"/> instead, which supports
/// multi-token source phrases via trie-based longest-match.
/// </remarks>
[Obsolete("Use SynonymGraphFilter with SynonymMap for multi-token synonym support.")]
public sealed class SynonymFilter : ITokenFilter
{
    private readonly IReadOnlyDictionary<string, string[]> _synonyms;
    private readonly StringComparer _comparer;

    /// <param name="synonyms">
    /// Map from a source term to its synonym terms.
    /// Keys are compared using <paramref name="comparer"/> (default: <see cref="StringComparer.OrdinalIgnoreCase"/>).
    /// </param>
    public SynonymFilter(IReadOnlyDictionary<string, string[]> synonyms,
        StringComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(synonyms);
        _comparer = comparer ?? StringComparer.OrdinalIgnoreCase;

        // Normalise key casing to ensure consistent lookup
        var normalised = new Dictionary<string, string[]>(_comparer);
        foreach (var (k, v) in synonyms)
            normalised[k] = v;
        _synonyms = normalised;
    }

    /// <summary>
    /// For each token that has a synonym mapping, inserts the synonym terms immediately
    /// after the source token, preserving the same start/end offsets so phrase matching
    /// treats them as occupying the same position.
    /// </summary>
    public void Apply(List<Token> tokens)
    {
        // Forward pass: build new list to avoid O(n²) Insert shifts
        var expanded = new List<Token>(tokens.Count + 4);
        for (int i = 0; i < tokens.Count; i++)
        {
            expanded.Add(tokens[i]);
            if (_synonyms.TryGetValue(tokens[i].Text, out var synonymTerms))
            {
                int start = tokens[i].StartOffset;
                int end = tokens[i].EndOffset;
                for (int j = 0; j < synonymTerms.Length; j++)
                    expanded.Add(new Token(synonymTerms[j], start, end));
            }
        }
        tokens.Clear();
        tokens.AddRange(expanded);
    }
}
