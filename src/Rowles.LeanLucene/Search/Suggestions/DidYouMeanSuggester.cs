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

        // Collect raw candidates with offsets for batch docFreq reads
        var rawCandidates = new List<(string BareTerm, long Offset, int Distance, SegmentReader Reader)>();

        foreach (var reader in searcher.GetSegmentReaders())
        {
            var matches = reader.GetFuzzyMatches(fieldPrefix, queryTerm.AsSpan(), maxEdits);
            foreach (var (qualifiedTerm, offset, distance) in matches)
            {
                var bareTerm = qualifiedTerm[(field.Length + 1)..];
                if (string.Equals(bareTerm, queryTerm, StringComparison.Ordinal))
                    continue;
                rawCandidates.Add((bareTerm, offset, distance, reader));
            }
        }

        if (rawCandidates.Count == 0)
            return [];

        // Sort by (reader, offset) for sequential I/O reads
        rawCandidates.Sort(static (a, b) =>
        {
            int cmp = a.Reader.GetHashCode().CompareTo(b.Reader.GetHashCode());
            return cmp != 0 ? cmp : a.Offset.CompareTo(b.Offset);
        });

        // Batch-read docFreq using direct offset reads (no dictionary lookup)
        var candidates = new Dictionary<string, (int EditDistance, int DocFreq)>(StringComparer.Ordinal);
        foreach (var (bareTerm, offset, distance, reader) in rawCandidates)
        {
            int df = reader.ReadDocFreqAtOffset(offset);
            if (candidates.TryGetValue(bareTerm, out var existing))
                candidates[bareTerm] = (Math.Min(existing.EditDistance, distance), existing.DocFreq + df);
            else
                candidates[bareTerm] = (distance, df);
        }

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
