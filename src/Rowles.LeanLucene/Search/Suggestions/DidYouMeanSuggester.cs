using Rowles.LeanLucene.Index.Segment;

namespace Rowles.LeanLucene.Search.Suggestions;

/// <summary>
/// "Did you mean?" spelling correction using the per-segment term dictionary.
/// Scans terms via FSTReader's byte-level Levenshtein with ASCII fast-path,
/// then scores by docFreq / (1 + editDistance).
/// </summary>
public static class DidYouMeanSuggester
{
    /// <summary>
    /// Suggests corrections for <paramref name="queryTerm"/> in the given <paramref name="field"/>.
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

        // Collect candidates with edit distances and doc frequencies in a single pass
        var candidates = new Dictionary<string, (int EditDistance, int DocFreq)>(StringComparer.Ordinal);

        foreach (var reader in searcher.GetSegmentReaders())
        {
            var matches = reader.GetFuzzyMatches(fieldPrefix, queryTerm.AsSpan(), maxEdits);
            foreach (var (qualifiedTerm, _, distance) in matches)
            {
                var bareTerm = qualifiedTerm[(field.Length + 1)..];
                if (string.Equals(bareTerm, queryTerm, StringComparison.Ordinal))
                    continue;

                int df = reader.GetDocFreqByQualified(qualifiedTerm);
                if (candidates.TryGetValue(bareTerm, out var existing))
                    candidates[bareTerm] = (Math.Min(existing.EditDistance, distance), existing.DocFreq + df);
                else
                    candidates[bareTerm] = (distance, df);
            }
        }

        if (candidates.Count == 0)
            return [];

        // Score and rank
        var suggestions = new List<Suggestion>(Math.Min(candidates.Count, topN * 2));
        foreach (var (bareTerm, (dist, df)) in candidates)
        {
            float score = df / (1f + dist);
            suggestions.Add(new Suggestion(bareTerm, dist, df, score));
        }

        suggestions.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        if (suggestions.Count > topN)
            suggestions.RemoveRange(topN, suggestions.Count - topN);

        return suggestions;
    }
}
