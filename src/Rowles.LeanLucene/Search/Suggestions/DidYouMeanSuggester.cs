using Rowles.LeanLucene.Index.Segment;

namespace Rowles.LeanLucene.Search.Suggestions;

/// <summary>
/// "Did you mean?" spelling correction based on indexed terms.
/// Returns the closest terms by Levenshtein distance, weighted by document frequency.
/// </summary>
public static class DidYouMeanSuggester
{
    /// <summary>
    /// Suggests corrections for <paramref name="queryTerm"/> in the given <paramref name="field"/>.
    /// Score = docFreq / (1 + editDistance).
    /// </summary>
    public static List<Suggestion> Suggest(
        Searcher.IndexSearcher searcher,
        string field,
        string queryTerm,
        int maxEdits = 2,
        int topN = 5)
    {
        ArgumentNullException.ThrowIfNull(searcher);
        ArgumentException.ThrowIfNullOrEmpty(field);
        ArgumentException.ThrowIfNullOrEmpty(queryTerm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topN);

        var fieldPrefix = string.Concat(field, "\x00");

        // Two-pass approach: first collect candidates with edit distances,
        // then score only viable candidates. Avoids GetDocFreqByQualified for
        // terms that can't make it into the top-N.
        var candidates = new Dictionary<string, (string QualifiedTerm, int EditDistance)>(StringComparer.Ordinal);

        foreach (var reader in searcher.GetSegmentReaders())
        {
            var matches = reader.GetFuzzyMatches(fieldPrefix, queryTerm.AsSpan(), maxEdits);
            foreach (var (qualifiedTerm, _, distance) in matches)
            {
                var bareTerm = qualifiedTerm[(field.Length + 1)..];
                if (string.Equals(bareTerm, queryTerm, StringComparison.Ordinal))
                    continue; // Skip exact matches early

                if (candidates.TryGetValue(bareTerm, out var existing))
                    candidates[bareTerm] = (qualifiedTerm, Math.Min(existing.EditDistance, distance));
                else
                    candidates[bareTerm] = (qualifiedTerm, distance);
            }
        }

        if (candidates.Count == 0)
            return [];

        // Score candidates: only compute docFreq for those that pass the distance filter
        var suggestions = new List<Suggestion>(Math.Min(candidates.Count, topN * 2));
        foreach (var (bareTerm, (qualifiedTerm, dist)) in candidates)
        {
            int df = 0;
            foreach (var reader in searcher.GetSegmentReaders())
                df += reader.GetDocFreqByQualified(qualifiedTerm);

            float score = df / (1f + dist);
            suggestions.Add(new Suggestion(bareTerm, dist, df, score));
        }

        suggestions.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        if (suggestions.Count > topN)
            suggestions.RemoveRange(topN, suggestions.Count - topN);

        return suggestions;
    }
}
