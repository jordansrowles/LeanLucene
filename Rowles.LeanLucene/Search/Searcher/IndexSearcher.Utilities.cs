using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Index;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Partial class containing utility methods (GetStoredFields, Explain, Suggest, SearchWithFacets, etc.).
/// </summary>
public sealed partial class IndexSearcher
{
    /// <summary>Retrieves stored fields for a global document ID.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetStoredFields(int globalDocId)
    {
        for (int i = 0; i < _readers.Count; i++)
        {
            int nextBase = i + 1 < _docBases.Length ? _docBases[i + 1] : _totalDocCount;
            if (globalDocId >= _docBases[i] && globalDocId < nextBase)
                return _readers[i].GetStoredFields(globalDocId - _docBases[i]);
        }
        return new Dictionary<string, IReadOnlyList<string>>();
    }

    /// <summary>
    /// Explains the score computation for a specific document and query.
    /// Returns null if the document does not match the query.
    /// </summary>
    public Explanation? Explain(TermQuery query, int globalDocId)
    {
        // Find the segment containing this doc
        int readerIndex = -1;
        for (int i = 0; i < _docBases.Length; i++)
        {
            int nextBase = i + 1 < _docBases.Length ? _docBases[i + 1] : _totalDocCount;
            if (globalDocId >= _docBases[i] && globalDocId < nextBase)
            {
                readerIndex = i;
                break;
            }
        }
        if (readerIndex < 0) return null;

        var reader = _readers[readerIndex];
        int localDocId = globalDocId - _docBases[readerIndex];

        if (!reader.IsLive(localDocId)) return null;

        var qt = query.CachedQualifiedTerm ??= string.Concat(query.Field, "\x00", query.Term);
        using var postings = reader.GetPostingsEnum(qt);
        if (postings.IsExhausted) return null;

        // Find the doc in the postings
        if (!postings.Advance(localDocId) || postings.DocId != localDocId)
            return null;

        int tf = postings.Freq;
        int docLength = reader.GetFieldLength(localDocId, query.Field);
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);

        // Compute global DF
        int globalDF = 0;
        foreach (var r in _readers)
        {
            using var p = r.GetPostingsEnum(qt);
            globalDF += p.DocFreq;
        }

        float idf = Bm25Scorer.Idf(_totalDocCount, globalDF);
        float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, globalDF);
        if (query.Boost != 1.0f) score *= query.Boost;

        return new Explanation
        {
            Score = score,
            Description = $"BM25 score for term '{query.Term}' in field '{query.Field}'",
            Details =
            [
                new Explanation { Score = idf, Description = $"idf(docFreq={globalDF}, docCount={_totalDocCount})" },
                new Explanation { Score = tf, Description = $"termFreq={tf}" },
                new Explanation { Score = docLength, Description = $"fieldLength={docLength}" },
                new Explanation { Score = avgDocLength, Description = $"avgFieldLength={avgDocLength:F2}" },
                new Explanation { Score = query.Boost, Description = $"boost={query.Boost}" }
            ]
        };
    }

    /// <summary>
    /// Returns the top-N terms with the given prefix for auto-complete / suggest,
    /// ranked by global document frequency descending.
    /// </summary>
    /// <param name="prefix">Term prefix to complete (e.g. "hel" → "hello", "help").</param>
    /// <param name="field">Field to scan.</param>
    /// <param name="topN">Maximum number of suggestions to return.</param>
    public IReadOnlyList<(string Term, int DocFreq)> Suggest(string prefix, string field, int topN)
    {
        if (topN <= 0 || _readers.Count == 0)
            return [];

        var qualifiedPrefix = $"{field}\x00{prefix}";
        // Accumulate (term → total docFreq) across all segments
        var termFreqs = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var reader in _readers)
        {
            var matchingTerms = reader.GetTermsWithPrefix(qualifiedPrefix);
            foreach (var (qualifiedTerm, _) in matchingTerms)
            {
                using var postings = reader.GetPostingsEnum(qualifiedTerm);
                if (postings.IsExhausted) continue;
                var bare = qualifiedTerm.AsSpan(field.Length + 1).ToString();
                termFreqs.TryGetValue(bare, out int existing);
                termFreqs[bare] = existing + postings.DocFreq;
            }
        }

        if (termFreqs.Count == 0) return [];

        // Manual sort + range avoids LINQ OrderByDescending().Take() allocation
        var result = new List<(string, int)>(termFreqs.Count);
        foreach (var kv in termFreqs)
            result.Add((kv.Key, kv.Value));
        result.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        if (result.Count > topN)
            result.RemoveRange(topN, result.Count - topN);
        return result;
    }

    /// <summary>Executes a query and returns both top-N results and facet counts for the specified fields.</summary>
    public (TopDocs Results, IReadOnlyList<FacetResult> Facets) SearchWithFacets(
        Query query, int topN, params string[] facetFields)
    {
        var results = Search(query, topN);
        var facetsCollector = new FacetsCollector();

        foreach (var sd in results.ScoreDocs)
        {
            int globalDocId = sd.DocId;
            int readerIndex = ResolveReaderIndex(globalDocId);
            var reader = _readers[readerIndex];
            int localDocId = globalDocId - _docBases[readerIndex];

            foreach (var facetField in facetFields)
            {
                if (reader.TryGetSortedDocValue(facetField, localDocId, out string val) && !string.IsNullOrEmpty(val))
                {
                    facetsCollector.Collect(facetField, val);
                }
                else
                {
                    var stored = reader.GetStoredFields(localDocId);
                    if (stored.TryGetValue(facetField, out var values))
                    {
                        foreach (var v in values)
                            facetsCollector.Collect(facetField, v);
                    }
                }
            }
        }

        return (results, facetsCollector.GetResults());
    }

    private int ResolveReaderIndex(int globalDocId)
    {
        for (int i = _docBases.Length - 1; i >= 0; i--)
        {
            if (globalDocId >= _docBases[i])
                return i;
        }
        return 0;
    }
}
