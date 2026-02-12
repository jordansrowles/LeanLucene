using System.Buffers;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Index.Indexer;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Partial class containing sorting functionality for search results.
/// </summary>
public sealed partial class IndexSearcher
{
    /// <summary>
    /// Searches with a custom sort order instead of relevance ranking.
    /// If all segments share an index-time sort matching the requested sort,
    /// results are returned directly with early termination (no post-sort needed).
    /// </summary>
    public TopDocs Search(Query query, int topN, SortField sort)
    {
        if (sort.Type == SortFieldType.Score)
            return Search(query, topN);

        // Check for index-time sorted segments matching the requested sort
        if (CanEarlyTerminate(sort))
        {
            var results = Search(query, topN);
            // Documents are already in the correct order from sorted segments
            return results;
        }

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

        var results2 = sorted.Length > topN ? sorted[..topN] : sorted;
        return new TopDocs(allDocs.TotalHits, results2);
    }

    /// <summary>
    /// Returns true if all segments are index-time sorted in an order that
    /// matches the requested <paramref name="sort"/>, allowing early termination.
    /// </summary>
    private bool CanEarlyTerminate(SortField sort)
    {
        if (_readers.Count == 0) return false;

        foreach (var reader in _readers)
        {
            var sortFields = reader.Info.IndexSortFields;
            if (sortFields is null || sortFields.Count == 0) return false;

            // The requested sort must match the first index sort field
            var first = sortFields[0];
            var parts = first.Split(':');
            if (parts.Length < 3) return false;

            if (!Enum.TryParse<SortFieldType>(parts[0], out var segType)) return false;
            var segField = parts[1];
            var segDesc = bool.TryParse(parts[2], out var d) && d;

            if (segType != sort.Type || segField != sort.FieldName || segDesc != sort.Descending)
                return false;
        }
        return true;
    }

    private static ScoreDoc[] SortByDocId(ScoreDoc[] docs, bool descending)
    {
        Array.Sort(docs, descending
            ? static (a, b) => b.DocId.CompareTo(a.DocId)
            : static (a, b) => a.DocId.CompareTo(b.DocId));
        return docs;
    }

    private ScoreDoc[] SortByNumericField(ScoreDoc[] docs, string fieldName, bool descending)
    {
        var values = ArrayPool<double>.Shared.Rent(docs.Length);
        try
        {
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
            Array.Sort(values, docs, 0, docs.Length);
            if (descending)
                Array.Reverse(docs);
            return docs;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(values, clearArray: false);
        }
    }

    private ScoreDoc[] SortByStringField(ScoreDoc[] docs, string fieldName, bool descending)
    {
        var values = ArrayPool<string>.Shared.Rent(docs.Length);
        try
        {
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
            Array.Sort(values, docs, 0, docs.Length, StringComparer.Ordinal);
            if (descending)
                Array.Reverse(docs, 0, docs.Length);
            return docs;
        }
        finally
        {
            // Clear refs to allow GC of the string instances
            Array.Clear(values, 0, docs.Length);
            ArrayPool<string>.Shared.Return(values, clearArray: false);
        }
    }
}
