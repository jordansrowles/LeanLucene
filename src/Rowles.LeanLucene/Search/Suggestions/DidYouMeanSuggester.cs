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
        var merged = new Dictionary<string, (int EditDistance, int DocFreq)>(StringComparer.Ordinal);

        foreach (var reader in searcher.GetSegmentReaders())
        {
            var matches = reader.GetFuzzyMatches(fieldPrefix, queryTerm.AsSpan(), maxEdits);
            foreach (var (qualifiedTerm, _) in matches)
            {
                var bareTerm = qualifiedTerm[(field.Length + 1)..];
                int df = reader.GetDocFreqByQualified(qualifiedTerm);
                int distance = LevenshteinDistance.Compute(queryTerm.AsSpan(), bareTerm.AsSpan());

                if (merged.TryGetValue(bareTerm, out var existing))
                    merged[bareTerm] = (Math.Min(existing.EditDistance, distance), existing.DocFreq + df);
                else
                    merged[bareTerm] = (distance, df);
            }
        }

        // Skip exact matches
        merged.Remove(queryTerm);

        var suggestions = new List<Suggestion>(merged.Count);
        foreach (var (term, (dist, df)) in merged)
        {
            float score = df / (1f + dist);
            suggestions.Add(new Suggestion(term, dist, df, score));
        }

        suggestions.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (suggestions.Count > topN)
            suggestions.RemoveRange(topN, suggestions.Count - topN);

        return suggestions;
    }
}
