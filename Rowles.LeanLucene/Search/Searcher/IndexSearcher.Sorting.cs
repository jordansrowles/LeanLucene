using Rowles.LeanLucene.Index;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Partial class containing sorting functionality for search results.
/// </summary>
public sealed partial class IndexSearcher
{
    /// <summary>
    /// Searches with a custom sort order instead of relevance ranking.
    /// First finds all matches, then sorts by the specified field.
    /// </summary>
    public TopDocs Search(Query query, int topN, SortField sort)
    {
        if (sort.Type == SortFieldType.Score)
            return Search(query, topN);

        // Collect all matching docs — use a generous limit to capture enough for re-sorting
        var allDocs = Search(query, Math.Max(topN, _totalDocCount));
        if (allDocs.TotalHits == 0) return TopDocs.Empty;

        var sorted = sort.Type switch
        {
            SortFieldType.DocId => SortByDocId(allDocs.ScoreDocs, sort.Descending),
            SortFieldType.Numeric => SortByNumericField(allDocs.ScoreDocs, sort.FieldName, sort.Descending),
            SortFieldType.String => SortByStringField(allDocs.ScoreDocs, sort.FieldName, sort.Descending),
            _ => allDocs.ScoreDocs
        };

        var results = sorted.Length > topN ? sorted[..topN] : sorted;
        return new TopDocs(allDocs.TotalHits, results);
    }

    private static ScoreDoc[] SortByDocId(ScoreDoc[] docs, bool descending)
    {
        var copy = docs.ToArray();
        Array.Sort(copy, descending
            ? static (a, b) => b.DocId.CompareTo(a.DocId)
            : static (a, b) => a.DocId.CompareTo(b.DocId));
        return copy;
    }

    private ScoreDoc[] SortByNumericField(ScoreDoc[] docs, string fieldName, bool descending)
    {
        var values = new double[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            double val = 0;
            int globalId = docs[i].DocId;
            bool found = false;
            for (int r = 0; r < _readers.Count; r++)
            {
                int nextBase = r + 1 < _docBases.Length ? _docBases[r + 1] : _totalDocCount;
                if (globalId >= _docBases[r] && globalId < nextBase)
                {
                    found = _readers[r].TryGetNumericValue(fieldName, globalId - _docBases[r], out val);
                    break;
                }
            }
            if (!found)
            {
                var stored = GetStoredFields(globalId);
                if (stored.TryGetValue(fieldName, out var sv) && sv.Count > 0)
                    double.TryParse(sv[0], System.Globalization.CultureInfo.InvariantCulture, out val);
            }
            values[i] = val;
        }

        // Sort docs in-place using values as the sort key
        Array.Sort(values, docs);
        if (descending)
            Array.Reverse(docs);
        return docs;
    }

    private ScoreDoc[] SortByStringField(ScoreDoc[] docs, string fieldName, bool descending)
    {
        var values = new string[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            string val = string.Empty;
            int globalId = docs[i].DocId;
            bool found = false;
            for (int r = 0; r < _readers.Count; r++)
            {
                int nextBase = r + 1 < _docBases.Length ? _docBases[r + 1] : _totalDocCount;
                if (globalId >= _docBases[r] && globalId < nextBase)
                {
                    found = _readers[r].TryGetSortedDocValue(fieldName, globalId - _docBases[r], out val);
                    break;
                }
            }
            if (!found)
            {
                var stored = GetStoredFields(globalId);
                if (stored.TryGetValue(fieldName, out var sv) && sv.Count > 0)
                    val = sv[0];
            }
            values[i] = val;
        }

        // Sort docs in-place using values as the sort key
        Array.Sort(values, docs, StringComparer.Ordinal);
        if (descending)
            Array.Reverse(docs);
        return docs;
    }
}
