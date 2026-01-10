namespace Rowles.LeanLucene.Analysis;

/// <summary>
/// Expands tokens with their configured synonyms, inserting synonym tokens at the
/// same position as the source token so that phrase queries still work correctly.
/// </summary>
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
        // Iterate backwards so insertions don't invalidate earlier indices
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            if (!_synonyms.TryGetValue(tokens[i].Text, out var synonymTerms))
                continue;

            int start = tokens[i].StartOffset;
            int end = tokens[i].EndOffset;
            for (int j = synonymTerms.Length - 1; j >= 0; j--)
                tokens.Insert(i + 1, new Token(synonymTerms[j], start, end));
        }
    }
}
