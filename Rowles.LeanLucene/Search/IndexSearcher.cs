using System.Buffers;
using System.Text.Json;
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Search;

/// <summary>
/// Holds a snapshot of segment readers and executes queries across all segments.
/// </summary>
public sealed class IndexSearcher : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly List<SegmentReader> _readers = [];
    private readonly int[] _docBases;
    private readonly int _totalDocCount;
    private readonly IndexStats _stats;
    private PostingsEnum[]? _postingsBuffer;
    private ScoreDoc[]? _collectorHeapCache;

    /// <summary>Corpus-wide statistics computed at construction.</summary>
    public IndexStats Stats => _stats;

    public IndexSearcher(MMapDirectory directory)
    {
        _directory = directory;

        var segmentIds = LoadLatestCommit();
        foreach (var segId in segmentIds)
        {
            var segPath = Path.Combine(directory.DirectoryPath, segId + ".seg");
            if (!File.Exists(segPath)) continue;
            var info = SegmentInfo.ReadFrom(segPath);
            _readers.Add(new SegmentReader(directory, info));
        }

        _docBases = AssignDocBases();
        _totalDocCount = _docBases.Length > 0
            ? _docBases[^1] + _readers[^1].MaxDoc
            : 0;
        _stats = ComputeStats();
    }

    public IndexSearcher(MMapDirectory directory, IReadOnlyList<SegmentInfo> segments)
    {
        _directory = directory;
        foreach (var info in segments)
            _readers.Add(new SegmentReader(directory, info));

        _docBases = AssignDocBases();
        _totalDocCount = _docBases.Length > 0
            ? _docBases[^1] + _readers[^1].MaxDoc
            : 0;
        _stats = ComputeStats();
    }

    private int[] AssignDocBases()
    {
        var bases = new int[_readers.Count];
        int docBase = 0;
        for (int i = 0; i < _readers.Count; i++)
        {
            bases[i] = docBase;
            _readers[i].DocBase = docBase;
            docBase += _readers[i].MaxDoc;
        }
        return bases;
    }

    public TopDocs Search(Query query, int topN)
    {
        if (topN <= 0 || _readers.Count == 0)
            return TopDocs.Empty;

        // Fast path for the most common query type — avoids
        // PrecomputeGlobalDocFreqs allocation and does only 1 dictionary
        // lookup per segment instead of 2.
        if (query is TermQuery tq)
            return SearchTermQuery(tq, topN);

        var globalDFs = PrecomputeGlobalDocFreqs(query);
        var collector = new TopNCollector(topN);

        if (_readers.Count == 1)
        {
            ExecuteQuery(query, _readers[0], globalDFs, ref collector);
        }
        else
        {
            var lockObj = new Lock();
            Parallel.ForEach(_readers, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, reader =>
            {
                var localCollector = new TopNCollector(topN);
                ExecuteQuery(query, reader, globalDFs, ref localCollector);
                var localDocs = localCollector.ToTopDocs();
                lock (lockObj)
                {
                    foreach (var sd in localDocs.ScoreDocs)
                        collector.Collect(sd.DocId, sd.Score);
                }
            });
        }

        return collector.ToTopDocs();
    }

    /// <summary>
    /// Parses a query string, applies analysis, and searches.
    /// </summary>
    public TopDocs Search(string queryString, string defaultField, int topN, IAnalyser? analyser = null)
    {
        analyser ??= new StandardAnalyser();
        var parser = new QueryParser(defaultField, analyser);
        var query = parser.Parse(queryString);
        return Search(query, topN);
    }

    /// <summary>
    /// Searches with a custom sort order instead of relevance ranking.
    /// First finds all matches, then sorts by the specified field.
    /// </summary>
    public TopDocs Search(Query query, int topN, SortField sort)
    {
        if (sort.Type == SortFieldType.Score)
            return Search(query, topN);

        // Collect all matching docs with scores
        var allDocs = Search(query, int.MaxValue / 2 > 0 ? 10000 : 10000);
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
        var withValues = new (ScoreDoc Doc, double Value)[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            double val = 0;
            int globalId = docs[i].DocId;
            // Use DocValues (fast column-stride) with fallback to stored fields
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
                if (stored.TryGetValue(fieldName, out var str))
                    double.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, out val);
            }
            withValues[i] = (docs[i], val);
        }

        Array.Sort(withValues, descending
            ? static (a, b) => b.Value.CompareTo(a.Value)
            : static (a, b) => a.Value.CompareTo(b.Value));

        var result = new ScoreDoc[withValues.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = withValues[i].Doc;
        return result;
    }

    private ScoreDoc[] SortByStringField(ScoreDoc[] docs, string fieldName, bool descending)
    {
        var withValues = new (ScoreDoc Doc, string Value)[docs.Length];
        for (int i = 0; i < docs.Length; i++)
        {
            string val = string.Empty;
            int globalId = docs[i].DocId;
            // Try SortedDocValues first, fallback to stored fields
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
                val = stored.GetValueOrDefault(fieldName, string.Empty);
            }
            withValues[i] = (docs[i], val);
        }

        Array.Sort(withValues, descending
            ? static (a, b) => string.Compare(b.Value, a.Value, StringComparison.Ordinal)
            : static (a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));

        var result = new ScoreDoc[withValues.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = withValues[i].Doc;
        return result;
    }

    /// <summary>Retrieves stored fields for a global document ID.</summary>
    public Dictionary<string, string> GetStoredFields(int globalDocId)
    {
        for (int i = 0; i < _readers.Count; i++)
        {
            int nextBase = i + 1 < _docBases.Length ? _docBases[i + 1] : _totalDocCount;
            if (globalDocId >= _docBases[i] && globalDocId < nextBase)
                return _readers[i].GetStoredFields(globalDocId - _docBases[i]);
        }
        return new Dictionary<string, string>();
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

        var qt = string.Concat(query.Field, "\x00", query.Term);
        using var postings = reader.GetPostingsEnum(qt);
        if (postings.IsExhausted) return null;

        // Find the doc in the postings
        if (!postings.Advance(localDocId) || postings.DocId != localDocId)
            return null;

        int tf = postings.Freq;
        int docLength = reader.GetFieldLength(localDocId);
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);

        // Compute global DF
        int globalDF = 0;
        foreach (var r in _readers)
        {
            using var p = r.GetPostingsEnum(qt);
            globalDF += p.DocFreq;
        }

        float idf = Bm25Scorer.Idf(_totalDocCount, globalDF);
        float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, globalDF);
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

    private TopDocs SearchTermQuery(TermQuery query, int topN)
    {
        var qt = query.CachedQualifiedTerm ??= string.Concat(query.Field, "\x00", query.Term);
        int readerCount = _readers.Count;

        // Reuse a pre-allocated array to avoid per-query allocation
        if (_postingsBuffer is null || _postingsBuffer.Length < readerCount)
            _postingsBuffer = new PostingsEnum[readerCount];
        var postingsArr = _postingsBuffer;
        int globalDF = 0;
        for (int i = 0; i < readerCount; i++)
        {
            postingsArr[i] = _readers[i].GetPostingsEnum(qt);
            globalDF += postingsArr[i].DocFreq;
        }

        if (globalDF == 0)
        {
            for (int i = 0; i < readerCount; i++)
                postingsArr[i].Dispose();
            return TopDocs.Empty;
        }

        // Phase 2: score using already-decoded postings
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        var (idf, k1BOverAvgDL) = Bm25Scorer.PrecomputeFactors(_totalDocCount, globalDF, avgDocLength);
        float boost = query.Boost;

        // Reuse the backing ScoreDoc[] across queries to avoid per-query allocation
        if (_collectorHeapCache is null || _collectorHeapCache.Length < topN)
            _collectorHeapCache = new ScoreDoc[topN];
        var collector = new TopNCollector(_collectorHeapCache, topN);

        try
        {
            for (int i = 0; i < readerCount; i++)
            {
                ref var postings = ref postingsArr[i];
                if (postings.IsExhausted) continue;

                var reader = _readers[i];
                int docBase = reader.DocBase;
                bool hasDeletions = reader.HasDeletions;

                while (postings.MoveNext())
                {
                    int docId = postings.DocId;
                    if (hasDeletions && !reader.IsLive(docId)) continue;

                    int tf = postings.Freq;
                    int docLength = reader.GetFieldLength(docId);
                    float score = Bm25Scorer.ScorePrecomputed(idf, k1BOverAvgDL, tf, docLength);
                    if (boost != 1.0f) score *= boost;
                    collector.Collect(docBase + docId, score);
                }
            }
        }
        finally
        {
            for (int i = 0; i < readerCount; i++)
                postingsArr[i].Dispose();
        }

        return collector.ToTopDocs();
    }

    private void ExecuteQuery(Query query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        switch (query)
        {
            case TermQuery tq:
                ExecuteTermQuery(tq, reader, globalDFs, ref collector);
                break;
            case BooleanQuery bq:
                ExecuteBooleanQuery(bq, reader, globalDFs, ref collector);
                break;
            case RangeQuery rq:
                ExecuteRangeQuery(rq, reader, ref collector);
                break;
            case PhraseQuery pq:
                ExecutePhraseQuery(pq, reader, ref collector);
                break;
            case VectorQuery vq:
                ExecuteVectorQuery(vq, reader, ref collector);
                break;
            case PrefixQuery pfq:
                ExecutePrefixQuery(pfq, reader, globalDFs, ref collector);
                break;
            case WildcardQuery wq:
                ExecuteWildcardQuery(wq, reader, globalDFs, ref collector);
                break;
            case FuzzyQuery fq:
                ExecuteFuzzyQuery(fq, reader, globalDFs, ref collector);
                break;
        }
    }

    private void ExecuteTermQuery(TermQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var qt = string.Concat(query.Field, "\x00", query.Term);
        using var postings = reader.GetPostingsEnum(qt);
        if (postings.IsExhausted) return;

        int docFreq = globalDFs.GetValueOrDefault((query.Field, query.Term), postings.DocFreq);
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;

        while (postings.MoveNext())
        {
            int docId = postings.DocId;
            if (!reader.IsLive(docId)) continue;

            int tf = postings.Freq;
            int docLength = reader.GetFieldLength(docId);
            float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
            if (query.Boost != 1.0f) score *= query.Boost;
            collector.Collect(docBase + docId, score);
        }
    }

    private void ExecuteBooleanQuery(BooleanQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var clauses = query.Clauses;
        if (clauses.Count == 0) return;

        // Single-pass clause counting + TermQuery check (no List<Query> allocs)
        int mustCount = 0, shouldCount = 0, mustNotCount = 0;
        bool allTermQueries = true;
        foreach (var clause in clauses)
        {
            switch (clause.Occur)
            {
                case Occur.Must: mustCount++; break;
                case Occur.Should: shouldCount++; break;
                case Occur.MustNot: mustNotCount++; break;
            }
            if (clause.Query is not TermQuery) allTermQueries = false;
        }

        if (mustCount == 0 && shouldCount == 0) return;

        // Fast path: all clauses are TermQuery → streaming PostingsEnum merge
        if (allTermQueries)
        {
            ExecuteBooleanStreaming(clauses, reader, globalDFs, ref collector,
                mustCount, shouldCount, mustNotCount);
            return;
        }

        // Fallback for complex sub-queries (nested BooleanQuery, RangeQuery, etc.)
        ExecuteBooleanFallback(query, reader, globalDFs, ref collector);
    }

    /// <summary>
    /// Streaming BooleanQuery execution for all-TermQuery clauses.
    /// Uses PostingsEnum merge instead of materialising HashSets and score maps.
    /// </summary>
    private void ExecuteBooleanStreaming(IReadOnlyList<BooleanClause> clauses,
        SegmentReader reader, Dictionary<(string Field, string Term), int> globalDFs,
        ref TopNCollector collector, int mustCount, int shouldCount, int mustNotCount)
    {
        var mustEnums = mustCount > 0 ? new PostingsEnum[mustCount] : null;
        var mustFactors = mustCount > 0 ? new (float Idf, float K1BOverAvgDL)[mustCount] : null;
        var shouldEnums = shouldCount > 0 ? new PostingsEnum[shouldCount] : null;
        var shouldFactors = shouldCount > 0 ? new (float Idf, float K1BOverAvgDL)[shouldCount] : null;
        var mustNotEnums = mustNotCount > 0 ? new PostingsEnum[mustNotCount] : null;

        int mi = 0, si = 0, mni = 0;

        try
        {
            foreach (var clause in clauses)
            {
                var tq = (TermQuery)clause.Query;
                var qt = string.Concat(tq.Field, "\x00", tq.Term);
                var postings = reader.GetPostingsEnum(qt);

                int docFreq = globalDFs.GetValueOrDefault((tq.Field, tq.Term), postings.DocFreq);
                float avgDocLength = _stats.GetAvgFieldLength(tq.Field);
                var factors = Bm25Scorer.PrecomputeFactors(_totalDocCount, docFreq, avgDocLength);

                switch (clause.Occur)
                {
                    case Occur.Must:
                        mustEnums![mi] = postings;
                        mustFactors![mi] = factors;
                        mi++;
                        break;
                    case Occur.Should:
                        shouldEnums![si] = postings;
                        shouldFactors![si] = factors;
                        si++;
                        break;
                    case Occur.MustNot:
                        mustNotEnums![mni] = postings;
                        mni++;
                        break;
                }
            }

            int docBase = reader.DocBase;
            bool hasDeletions = reader.HasDeletions;

            if (mustCount > 0)
            {
                // Sort Must enums by DocFreq ascending — rarest term leads
                int leaderIdx = 0;
                for (int i = 1; i < mustCount; i++)
                {
                    if (mustEnums![i].DocFreq < mustEnums[leaderIdx].DocFreq)
                        leaderIdx = i;
                }
                if (leaderIdx != 0)
                {
                    (mustEnums![0], mustEnums[leaderIdx]) = (mustEnums[leaderIdx], mustEnums[0]);
                    (mustFactors![0], mustFactors[leaderIdx]) = (mustFactors[leaderIdx], mustFactors[0]);
                }

                // Stream through leader, advance followers
                while (mustEnums![0].MoveNext())
                {
                    int docId = mustEnums[0].DocId;
                    if (hasDeletions && !reader.IsLive(docId)) continue;

                    // Check all followers match this doc
                    bool allMatch = true;
                    for (int i = 1; i < mustCount; i++)
                    {
                        if (!mustEnums[i].Advance(docId) || mustEnums[i].DocId != docId)
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (!allMatch) continue;

                    // Check MustNot exclusions
                    bool excluded = false;
                    for (int i = 0; i < mustNotCount; i++)
                    {
                        if (mustNotEnums![i].Advance(docId) && mustNotEnums[i].DocId == docId)
                        {
                            excluded = true;
                            break;
                        }
                    }
                    if (excluded) continue;

                    // Compute BM25 score summed across Must terms
                    int docLength = reader.GetFieldLength(docId);
                    float score = 0f;
                    for (int i = 0; i < mustCount; i++)
                    {
                        score += Bm25Scorer.ScorePrecomputed(
                            mustFactors![i].Idf, mustFactors[i].K1BOverAvgDL,
                            mustEnums[i].Freq, docLength);
                    }

                    // Add Should bonus
                    for (int i = 0; i < shouldCount; i++)
                    {
                        if (shouldEnums![i].Advance(docId) && shouldEnums[i].DocId == docId)
                        {
                            score += Bm25Scorer.ScorePrecomputed(
                                shouldFactors![i].Idf, shouldFactors[i].K1BOverAvgDL,
                                shouldEnums[i].Freq, docLength);
                        }
                    }

                    collector.Collect(docBase + docId, score);
                }
            }
            else
            {
                // Should-only: need score accumulation for docs matching multiple terms
                var scoreMap = new Dictionary<int, float>();
                for (int i = 0; i < shouldCount; i++)
                {
                    while (shouldEnums![i].MoveNext())
                    {
                        int docId = shouldEnums[i].DocId;
                        if (hasDeletions && !reader.IsLive(docId)) continue;

                        int docLength = reader.GetFieldLength(docId);
                        float termScore = Bm25Scorer.ScorePrecomputed(
                            shouldFactors![i].Idf, shouldFactors[i].K1BOverAvgDL,
                            shouldEnums[i].Freq, docLength);
                        scoreMap[docId] = scoreMap.GetValueOrDefault(docId) + termScore;
                    }
                }

                // Build exclusion set if MustNot present
                HashSet<int>? exclusions = null;
                if (mustNotCount > 0)
                {
                    exclusions = new HashSet<int>();
                    for (int i = 0; i < mustNotCount; i++)
                    {
                        while (mustNotEnums![i].MoveNext())
                            exclusions.Add(mustNotEnums[i].DocId);
                    }
                }

                foreach (var (docId, score) in scoreMap)
                {
                    if (exclusions?.Contains(docId) == true) continue;
                    collector.Collect(docBase + docId, score);
                }
            }
        }
        finally
        {
            if (mustEnums != null)
                for (int i = 0; i < mi; i++) mustEnums[i].Dispose();
            if (shouldEnums != null)
                for (int i = 0; i < si; i++) shouldEnums[i].Dispose();
            if (mustNotEnums != null)
                for (int i = 0; i < mni; i++) mustNotEnums[i].Dispose();
        }
    }

    /// <summary>
    /// Fallback BooleanQuery execution for mixed clause types (nested BooleanQuery, RangeQuery, etc.).
    /// Uses the original materialisation approach via ExecuteSubQuery.
    /// </summary>
    private void ExecuteBooleanFallback(BooleanQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var scoreMap = new Dictionary<int, float>();
        HashSet<int> candidates;

        // Inline categorisation: iterate clauses by Occur without building separate lists
        bool hasMust = false;
        foreach (var clause in query.Clauses)
        {
            if (clause.Occur == Occur.Must) { hasMust = true; break; }
        }

        if (hasMust)
        {
            var mustSets = new List<HashSet<int>>();
            foreach (var clause in query.Clauses)
            {
                if (clause.Occur != Occur.Must) continue;
                var results = ExecuteSubQuery(clause.Query, reader, globalDFs);
                var idSet = new HashSet<int>(results.Count);
                foreach (var sr in results)
                {
                    idSet.Add(sr.DocId);
                    scoreMap[sr.DocId] = scoreMap.GetValueOrDefault(sr.DocId) + sr.Score;
                }
                mustSets.Add(idSet);
            }
            candidates = mustSets[0];
            for (int i = 1; i < mustSets.Count; i++)
                candidates.IntersectWith(mustSets[i]);

            foreach (var clause in query.Clauses)
            {
                if (clause.Occur != Occur.Should) continue;
                var results = ExecuteSubQuery(clause.Query, reader, globalDFs);
                foreach (var sr in results)
                {
                    if (candidates.Contains(sr.DocId))
                        scoreMap[sr.DocId] = scoreMap.GetValueOrDefault(sr.DocId) + sr.Score;
                }
            }
        }
        else
        {
            candidates = new HashSet<int>();
            foreach (var clause in query.Clauses)
            {
                if (clause.Occur != Occur.Should) continue;
                var results = ExecuteSubQuery(clause.Query, reader, globalDFs);
                foreach (var sr in results)
                {
                    candidates.Add(sr.DocId);
                    scoreMap[sr.DocId] = scoreMap.GetValueOrDefault(sr.DocId) + sr.Score;
                }
            }

            if (candidates.Count == 0) return;
        }

        foreach (var clause in query.Clauses)
        {
            if (clause.Occur != Occur.MustNot) continue;
            var results = ExecuteSubQuery(clause.Query, reader, globalDFs);
            foreach (var sr in results)
                candidates.Remove(sr.DocId);
        }

        int docBase = reader.DocBase;
        foreach (var id in candidates)
        {
            if (!reader.IsLive(id)) continue;
            collector.Collect(docBase + id, scoreMap.GetValueOrDefault(id, 1.0f));
        }
    }

    /// <summary>Collects sub-query results into a list (used by BooleanQuery for set operations).</summary>
    private List<ScoreDoc> ExecuteSubQuery(Query query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs)
    {
        var results = new List<ScoreDoc>();
        switch (query)
        {
            case TermQuery tq:
            {
                var qt = string.Concat(tq.Field, "\x00", tq.Term);
                using var postings = reader.GetPostingsEnum(qt);
                if (postings.IsExhausted) break;
                int docFreq = globalDFs.GetValueOrDefault((tq.Field, tq.Term), postings.DocFreq);
                float avgDocLength = _stats.GetAvgFieldLength(tq.Field);
                while (postings.MoveNext())
                {
                    int docId = postings.DocId;
                    if (!reader.IsLive(docId)) continue;
                    int tf = postings.Freq;
                    int docLength = reader.GetFieldLength(docId);
                    float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
                    results.Add(new ScoreDoc(docId, score));
                }
                break;
            }
            case BooleanQuery bq:
            {
                // Nested boolean: use a sub-collector and extract results
                var subCollector = new TopNCollector(int.MaxValue / 2 > 0 ? 10000 : 10000);
                ExecuteBooleanQuery(bq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case RangeQuery rq:
            {
                var rangeResults = reader.GetNumericRange(rq.Field, rq.Min, rq.Max);
                float rqScore = rq.Boost != 1.0f ? rq.Boost : 1.0f;
                foreach (var r in rangeResults)
                    results.Add(new ScoreDoc(r.DocId, rqScore));
                break;
            }
        }
        return results;
    }

    private void ExecuteRangeQuery(RangeQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        int docBase = reader.DocBase;
        float score = query.Boost != 1.0f ? query.Boost : 1.0f;
        var rangeResults = reader.GetNumericRange(query.Field, query.Min, query.Max);
        if (rangeResults.Count > 0)
        {
            foreach (var r in rangeResults)
                collector.Collect(docBase + r.DocId, score);
            return;
        }

        for (int docId = 0; docId < reader.MaxDoc; docId++)
        {
            if (!reader.IsLive(docId)) continue;

            var stored = reader.GetStoredFields(docId);
            if (stored.TryGetValue(query.Field, out var valStr) && double.TryParse(valStr, out var val))
            {
                if (val >= query.Min && val <= query.Max)
                    collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecutePhraseQuery(PhraseQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        if (query.Terms.Length == 0) return;

        // Use PostingsEnum to gather candidate doc IDs efficiently
        var qualifiedTerms = new string[query.Terms.Length];
        for (int i = 0; i < query.Terms.Length; i++)
            qualifiedTerms[i] = string.Concat(query.Field, "\x00", query.Terms[i]);

        // Find intersection of doc IDs across all terms using PostingsEnum
        using var firstPostings = reader.GetPostingsEnum(qualifiedTerms[0]);
        if (firstPostings.IsExhausted) return;

        var candidateBuf = ArrayPool<int>.Shared.Rent(firstPostings.DocFreq);
        int candidateCount = 0;
        while (firstPostings.MoveNext())
            candidateBuf[candidateCount++] = firstPostings.DocId;

        // Intersect with remaining terms
        for (int t = 1; t < qualifiedTerms.Length && candidateCount > 0; t++)
        {
            using var postings = reader.GetPostingsEnum(qualifiedTerms[t]);
            if (postings.IsExhausted) { candidateCount = 0; break; }

            int writeIdx = 0;
            for (int i = 0; i < candidateCount; i++)
            {
                if (postings.Advance(candidateBuf[i]) && postings.DocId == candidateBuf[i])
                    candidateBuf[writeIdx++] = candidateBuf[i];
            }
            candidateCount = writeIdx;
        }

        if (candidateCount == 0)
        {
            ArrayPool<int>.Shared.Return(candidateBuf);
            return;
        }

        int docBase = reader.DocBase;
        float boost = query.Boost;
        float score = boost != 1.0f ? boost : 1.0f;
        int termCount = query.Terms.Length;

        // Reusable position list for phrase checking (avoids per-doc List<int[]> alloc)
        var termPositions = new List<int[]>(termCount);

        for (int c = 0; c < candidateCount; c++)
        {
            int docId = candidateBuf[c];
            if (!reader.IsLive(docId)) continue;

            termPositions.Clear();
            bool hasAllPositions = true;
            foreach (var term in query.Terms)
            {
                var positions = reader.GetPositions(query.Field, term, docId);
                if (positions == null || positions.Length == 0)
                {
                    hasAllPositions = false;
                    break;
                }
                termPositions.Add(positions);
            }

            if (hasAllPositions && HasPositionsWithinSlop(termPositions, query.Slop))
            {
                collector.Collect(docBase + docId, score);
                continue;
            }

            if (!hasAllPositions && query.Slop == 0)
            {
                var stored = reader.GetStoredFields(docId);
                if (stored.TryGetValue(query.Field, out var text) && ContainsPhrase(text, query.Terms))
                    collector.Collect(docBase + docId, score);
            }
        }

        ArrayPool<int>.Shared.Return(candidateBuf);
    }

    private void ExecuteVectorQuery(VectorQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        if (!reader.HasVectors) return;

        int docBase = reader.DocBase;
        for (int docId = 0; docId < reader.MaxDoc; docId++)
        {
            if (!reader.IsLive(docId)) continue;

            var docVector = reader.GetVector(docId);
            if (docVector == null || docVector.Length == 0) continue;

            float similarity = VectorQuery.CosineSimilarity(query.QueryVector, docVector);
            collector.Collect(docBase + docId, similarity);
        }
    }

    private void ExecutePrefixQuery(PrefixQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var qualifiedPrefix = $"{query.Field}\x00{query.Prefix}";
        var matchingTerms = reader.GetTermsWithPrefix(qualifiedPrefix);
        if (matchingTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;

        foreach (var (qualifiedTerm, _) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnum(qualifiedTerm);
            if (postings.IsExhausted) continue;

            var termPart = qualifiedTerm.AsSpan(query.Field.Length + 1).ToString();
            int docFreq = globalDFs.GetValueOrDefault((query.Field, termPart), postings.DocFreq);

            while (postings.MoveNext())
            {
                int docId = postings.DocId;
                if (!reader.IsLive(docId)) continue;

                int tf = postings.Freq;
                int docLength = reader.GetFieldLength(docId);
                float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecuteWildcardQuery(WildcardQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var matchingTerms = reader.GetTermsMatching(fieldPrefix, query.Pattern.AsSpan());
        if (matchingTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;

        foreach (var (qualifiedTerm, _) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnum(qualifiedTerm);
            if (postings.IsExhausted) continue;

            var termPart = qualifiedTerm.AsSpan(query.Field.Length + 1).ToString();
            int docFreq = globalDFs.GetValueOrDefault((query.Field, termPart), postings.DocFreq);

            while (postings.MoveNext())
            {
                int docId = postings.DocId;
                if (!reader.IsLive(docId)) continue;

                int tf = postings.Freq;
                int docLength = reader.GetFieldLength(docId);
                float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecuteFuzzyQuery(FuzzyQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var allTerms = reader.GetAllTermsForField(fieldPrefix);
        if (allTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;
        var queryTermSpan = query.Term.AsSpan();

        foreach (var (qualifiedTerm, _) in allTerms)
        {
            var termPart = qualifiedTerm.AsSpan(query.Field.Length + 1);
            int distance = LevenshteinDistance.Compute(queryTermSpan, termPart);
            if (distance > query.MaxEdits) continue;

            using var postings = reader.GetPostingsEnum(qualifiedTerm);
            if (postings.IsExhausted) continue;

            float distanceFactor = 1.0f - ((float)distance / (query.MaxEdits + 1));
            var termStr = termPart.ToString();
            int docFreq = globalDFs.GetValueOrDefault((query.Field, termStr), postings.DocFreq);

            while (postings.MoveNext())
            {
                int docId = postings.DocId;
                if (!reader.IsLive(docId)) continue;

                int tf = postings.Freq;
                int docLength = reader.GetFieldLength(docId);
                float score = Bm25Scorer.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq) * distanceFactor;
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private static bool ContainsPhrase(string text, string[] terms)
    {
        // Tokenise the text and look for consecutive matches without LINQ
        var textSpan = text.AsSpan();
        // Count words first
        int wordCount = 0;
        int idx = 0;
        while (idx < textSpan.Length)
        {
            while (idx < textSpan.Length && textSpan[idx] == ' ') idx++;
            if (idx >= textSpan.Length) break;
            wordCount++;
            while (idx < textSpan.Length && textSpan[idx] != ' ') idx++;
        }

        if (wordCount < terms.Length) return false;

        // Extract word boundaries
        var starts = new int[wordCount];
        var lengths = new int[wordCount];
        int wi = 0;
        idx = 0;
        while (idx < textSpan.Length)
        {
            while (idx < textSpan.Length && textSpan[idx] == ' ') idx++;
            if (idx >= textSpan.Length) break;
            int start = idx;
            while (idx < textSpan.Length && textSpan[idx] != ' ') idx++;
            starts[wi] = start;
            lengths[wi] = idx - start;
            wi++;
        }

        for (int i = 0; i <= wordCount - terms.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < terms.Length; j++)
            {
                var wordSpan = textSpan.Slice(starts[i + j], lengths[i + j]);
                // Trim punctuation from word boundaries
                while (wordSpan.Length > 0 && !char.IsLetterOrDigit(wordSpan[^1]))
                    wordSpan = wordSpan[..^1];
                while (wordSpan.Length > 0 && !char.IsLetterOrDigit(wordSpan[0]))
                    wordSpan = wordSpan[1..];

                if (!wordSpan.Equals(terms[j].AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }
            if (match) return true;
        }
        return false;
    }

    private static bool HasPositionsWithinSlop(List<int[]> termPositions, int slop)
    {
        foreach (var startPos in termPositions[0])
        {
            bool match = true;
            for (int i = 1; i < termPositions.Count; i++)
            {
                int expectedPos = startPos + i;
                bool found = false;

                if (slop == 0)
                {
                    found = Array.BinarySearch(termPositions[i], expectedPos) >= 0;
                }
                else
                {
                    // Search for any position within [expectedPos - slop, expectedPos + slop]
                    int idx = Array.BinarySearch(termPositions[i], expectedPos - slop);
                    if (idx < 0) idx = ~idx;
                    for (int j = idx; j < termPositions[i].Length; j++)
                    {
                        int pos = termPositions[i][j];
                        if (pos > expectedPos + slop) break;
                        if (pos >= expectedPos - slop)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private Dictionary<(string Field, string Term), int> PrecomputeGlobalDocFreqs(Query query)
    {
        var terms = new HashSet<(string Field, string Term)>();
        CollectTerms(query, terms);

        var result = new Dictionary<(string Field, string Term), int>(terms.Count);
        foreach (var (field, term) in terms)
        {
            var qt = string.Concat(field, "\x00", term);
            int total = 0;
            foreach (var reader in _readers)
                total += reader.GetDocFreqByQualified(qt);
            result[(field, term)] = total;
        }
        return result;
    }

    private static void CollectTerms(Query query, HashSet<(string Field, string Term)> terms)
    {
        switch (query)
        {
            case TermQuery tq:
                terms.Add((tq.Field, tq.Term));
                break;
            case BooleanQuery bq:
                foreach (var clause in bq.Clauses)
                    CollectTerms(clause.Query, terms);
                break;
            case PhraseQuery pq:
                foreach (var term in pq.Terms)
                    terms.Add((pq.Field, term));
                break;
            // Expansion queries (prefix/wildcard/fuzzy) resolve terms at execution
            // time per-segment, so no static term collection is needed here.
        }
    }

    private List<string> LoadLatestCommit()
    {
        var files = Directory.GetFiles(_directory.DirectoryPath, "segments_*");
        if (files.Length == 0) return [];

        var latest = files
            .Select(f => (File: f, Gen: int.TryParse(Path.GetFileName(f).Replace("segments_", ""), out int g) ? g : -1))
            .Where(x => x.Gen >= 0)
            .OrderByDescending(x => x.Gen)
            .FirstOrDefault();

        if (latest.File == null) return [];

        var json = File.ReadAllText(latest.File);
        using var doc = JsonDocument.Parse(json);
        var segments = doc.RootElement.GetProperty("Segments");
        return segments.EnumerateArray().Select(e => e.GetString()!).ToList();
    }

    public void Dispose()
    {
        foreach (var reader in _readers)
            reader.Dispose();
    }

    private IndexStats ComputeStats()
    {
        if (_readers.Count == 0)
            return IndexStats.Empty;

        int liveDocCount = 0;
        var fieldLengthSums = new Dictionary<string, long>();
        var fieldDocCounts = new Dictionary<string, int>();

        foreach (var reader in _readers)
        {
            for (int docId = 0; docId < reader.MaxDoc; docId++)
            {
                if (!reader.IsLive(docId)) continue;
                liveDocCount++;

                int fieldLen = reader.GetFieldLength(docId);
                foreach (var field in reader.Info.FieldNames)
                {
                    fieldLengthSums[field] = fieldLengthSums.GetValueOrDefault(field) + fieldLen;
                    fieldDocCounts[field] = fieldDocCounts.GetValueOrDefault(field) + 1;
                }
            }
        }

        var avgFieldLengths = new Dictionary<string, float>();
        foreach (var (field, sum) in fieldLengthSums)
        {
            int count = fieldDocCounts.GetValueOrDefault(field, 1);
            avgFieldLengths[field] = count > 0 ? (float)sum / count : 1.0f;
        }

        return new IndexStats(_totalDocCount, liveDocCount, avgFieldLengths, fieldDocCounts);
    }
}
