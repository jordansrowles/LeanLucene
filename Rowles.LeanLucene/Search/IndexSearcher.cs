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
    private readonly ISimilarity _similarity;
    private PostingsEnum[]? _postingsBuffer;
    private ScoreDoc[]? _collectorHeapCache;

    /// <summary>Corpus-wide statistics computed at construction.</summary>
    public IndexStats Stats => _stats;

    public IndexSearcher(MMapDirectory directory, ISimilarity? similarity = null)
    {
        _directory = directory;
        _similarity = similarity ?? Bm25Similarity.Instance;

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

    public IndexSearcher(MMapDirectory directory, IReadOnlyList<SegmentInfo> segments, ISimilarity? similarity = null)
    {
        _directory = directory;
        _similarity = similarity ?? Bm25Similarity.Instance;
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
    /// Searches with cancellation support. Checks the token between segments and between
    /// inner sub-clauses, allowing long-running queries to be interrupted.
    /// </summary>
    public TopDocs Search(Query query, int topN, CancellationToken cancellationToken)
    {
        if (topN <= 0 || _readers.Count == 0)
            return TopDocs.Empty;

        cancellationToken.ThrowIfCancellationRequested();

        if (query is TermQuery tq)
            return SearchTermQuery(tq, topN);

        var globalDFs = PrecomputeGlobalDocFreqs(query);
        var collector = new TopNCollector(topN);

        foreach (var reader in _readers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExecuteQuery(query, reader, globalDFs, ref collector);
        }

        return collector.ToTopDocs();
    }

    /// <summary>
    /// Parses a query string and searches with cancellation support.
    /// </summary>
    public TopDocs Search(string queryString, string defaultField, int topN,
        IAnalyser? analyser, CancellationToken cancellationToken)
    {
        analyser ??= new StandardAnalyser();
        var parser = new QueryParser(defaultField, analyser);
        var query = parser.Parse(queryString);
        return Search(query, topN, cancellationToken);
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

        var sorted = termFreqs.OrderByDescending(kv => kv.Value).Take(topN);
        var result = new List<(string, int)>(Math.Min(topN, termFreqs.Count));
        foreach (var kv in sorted)
            result.Add((kv.Key, kv.Value));
        return result;
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
                if (stored.TryGetValue(fieldName, out var values) && values.Count > 0)
                    double.TryParse(values[0], System.Globalization.CultureInfo.InvariantCulture, out val);
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
                if (stored.TryGetValue(fieldName, out var values) && values.Count > 0)
                    val = values[0];
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

        var qt = string.Concat(query.Field, "\x00", query.Term);
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
        var (idf, k1BOverAvgDL) = _similarity.PrecomputeFactors(_totalDocCount, globalDF, avgDocLength);
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
                    int docLength = reader.GetFieldLength(docId, query.Field);
                    float score = _similarity.ScorePrecomputed(idf, k1BOverAvgDL, tf, docLength);
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
            case TermRangeQuery trq:
                ExecuteTermRangeQuery(trq, reader, globalDFs, ref collector);
                break;
            case ConstantScoreQuery csq:
                ExecuteConstantScoreQuery(csq, reader, globalDFs, ref collector);
                break;
            case DisjunctionMaxQuery dmq:
                ExecuteDisjunctionMaxQuery(dmq, reader, globalDFs, ref collector);
                break;
            case RegexpQuery rxq:
                ExecuteRegexpQuery(rxq, reader, globalDFs, ref collector);
                break;
            case FunctionScoreQuery fsq:
                ExecuteFunctionScoreQuery(fsq, reader, globalDFs, ref collector);
                break;
            case SpanNearQuery snq:
                ExecuteSpanNearQuery(snq, reader, globalDFs, ref collector);
                break;
            case SpanOrQuery soq:
                ExecuteSpanOrQuery(soq, reader, globalDFs, ref collector);
                break;
            case SpanNotQuery snotq:
                ExecuteSpanNotQuery(snotq, reader, globalDFs, ref collector);
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
            int docLength = reader.GetFieldLength(docId, query.Field);
            float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
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
        var mustFields = mustCount > 0 ? new string[mustCount] : null;
        var shouldEnums = shouldCount > 0 ? new PostingsEnum[shouldCount] : null;
        var shouldFactors = shouldCount > 0 ? new (float Idf, float K1BOverAvgDL)[shouldCount] : null;
        var shouldFields = shouldCount > 0 ? new string[shouldCount] : null;
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
                var factors = _similarity.PrecomputeFactors(_totalDocCount, docFreq, avgDocLength);

                switch (clause.Occur)
                {
                    case Occur.Must:
                        mustEnums![mi] = postings;
                        mustFactors![mi] = factors;
                        mustFields![mi] = tq.Field;
                        mi++;
                        break;
                    case Occur.Should:
                        shouldEnums![si] = postings;
                        shouldFactors![si] = factors;
                        shouldFields![si] = tq.Field;
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
                    (mustFields![0], mustFields[leaderIdx]) = (mustFields[leaderIdx], mustFields[0]);
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

                    // Compute BM25 score summed across Must terms (using per-field lengths)
                    float score = 0f;
                    for (int i = 0; i < mustCount; i++)
                    {
                        int docLength = reader.GetFieldLength(docId, mustFields![i]);
                        score += _similarity.ScorePrecomputed(
                            mustFactors![i].Idf, mustFactors[i].K1BOverAvgDL,
                            mustEnums[i].Freq, docLength);
                    }

                    // Add Should bonus
                    for (int i = 0; i < shouldCount; i++)
                    {
                        if (shouldEnums![i].Advance(docId) && shouldEnums[i].DocId == docId)
                        {
                            int docLength = reader.GetFieldLength(docId, shouldFields![i]);
                            score += _similarity.ScorePrecomputed(
                                shouldFactors![i].Idf, shouldFactors[i].K1BOverAvgDL,
                                shouldEnums[i].Freq, docLength);
                        }
                    }

                    collector.Collect(docBase + docId, score);
                }
            }
            else
            {
                // Should-only: streaming OR merge across all Should PostingsEnums.
                // Uses stackalloc to track current docId per enum — no heap allocation.
                Span<int> currentDocs = stackalloc int[shouldCount];
                for (int i = 0; i < shouldCount; i++)
                    currentDocs[i] = shouldEnums![i].MoveNext() ? shouldEnums[i].DocId : int.MaxValue;

                while (true)
                {
                    // Find minimum docId across all enums
                    int minDoc = int.MaxValue;
                    for (int i = 0; i < shouldCount; i++)
                    {
                        if (currentDocs[i] < minDoc)
                            minDoc = currentDocs[i];
                    }
                    if (minDoc == int.MaxValue) break;

                    if (hasDeletions && !reader.IsLive(minDoc))
                    {
                        for (int i = 0; i < shouldCount; i++)
                        {
                            if (currentDocs[i] == minDoc)
                                currentDocs[i] = shouldEnums![i].MoveNext() ? shouldEnums[i].DocId : int.MaxValue;
                        }
                        continue;
                    }

                    // Sum scores for all enums positioned at minDoc (using per-field lengths)
                    float score = 0f;
                    for (int i = 0; i < shouldCount; i++)
                    {
                        if (currentDocs[i] == minDoc)
                        {
                            int docLength = reader.GetFieldLength(minDoc, shouldFields![i]);
                            score += _similarity.ScorePrecomputed(
                                shouldFactors![i].Idf, shouldFactors[i].K1BOverAvgDL,
                                shouldEnums![i].Freq, docLength);
                            currentDocs[i] = shouldEnums[i].MoveNext() ? shouldEnums[i].DocId : int.MaxValue;
                        }
                    }

                    // Check MustNot exclusion (Advance is forward-only; minDoc is monotonic)
                    bool excluded = false;
                    for (int i = 0; i < mustNotCount; i++)
                    {
                        if (mustNotEnums![i].Advance(minDoc) && mustNotEnums[i].DocId == minDoc)
                        {
                            excluded = true;
                            break;
                        }
                    }
                    if (!excluded)
                        collector.Collect(docBase + minDoc, score);
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
                    int docLength = reader.GetFieldLength(docId, tq.Field);
                    float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
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
            case TermRangeQuery trq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteTermRangeQuery(trq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case ConstantScoreQuery csq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteConstantScoreQuery(csq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case DisjunctionMaxQuery dmq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteDisjunctionMaxQuery(dmq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case RegexpQuery rxq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteRegexpQuery(rxq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case FunctionScoreQuery fsq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteFunctionScoreQuery(fsq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case SpanNearQuery snq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteSpanNearQuery(snq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case SpanOrQuery soq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteSpanOrQuery(soq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
                break;
            }
            case SpanNotQuery snotq:
            {
                var subCollector = new TopNCollector(10000);
                ExecuteSpanNotQuery(snotq, reader, globalDFs, ref subCollector);
                var subDocs = subCollector.ToTopDocs();
                foreach (var sd in subDocs.ScoreDocs)
                    results.Add(new ScoreDoc(sd.DocId - reader.DocBase, sd.Score));
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
            if (stored.TryGetValue(query.Field, out var values) && values.Count > 0 && double.TryParse(values[0], out var val))
            {
                if (val >= query.Min && val <= query.Max)
                    collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecutePhraseQuery(PhraseQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        if (query.Terms.Length == 0) return;

        int termCount = query.Terms.Length;
        var qualifiedTerms = new string[termCount];
        for (int i = 0; i < termCount; i++)
            qualifiedTerms[i] = string.Concat(query.Field, "\x00", query.Terms[i]);

        // Open position-aware PostingsEnums for all terms
        Span<PostingsEnum> postingsArr = new PostingsEnum[termCount];
        for (int i = 0; i < termCount; i++)
        {
            postingsArr[i] = reader.GetPostingsEnumWithPositions(qualifiedTerms[i]);
            if (postingsArr[i].IsExhausted)
            {
                for (int j = 0; j <= i; j++)
                    postingsArr[j].Dispose();
                return;
            }
        }

        // Find leader (rarest term) for efficient intersection
        int leaderIdx = 0;
        for (int i = 1; i < termCount; i++)
        {
            if (postingsArr[i].DocFreq < postingsArr[leaderIdx].DocFreq)
                leaderIdx = i;
        }

        int docBase = reader.DocBase;
        float boost = query.Boost;
        float score = boost != 1.0f ? boost : 1.0f;
        int slop = query.Slop;

        // Streaming merge: iterate leader, advance followers
        while (postingsArr[leaderIdx].MoveNext())
        {
            int docId = postingsArr[leaderIdx].DocId;
            if (!reader.IsLive(docId)) continue;

            bool allMatch = true;
            for (int i = 0; i < termCount; i++)
            {
                if (i == leaderIdx) continue;
                if (!postingsArr[i].Advance(docId) || postingsArr[i].DocId != docId)
                {
                    allMatch = false;
                    break;
                }
            }

            if (!allMatch) continue;

            // All terms present in this doc — check positions inline
            bool hasAllPositions = true;
            for (int i = 0; i < termCount; i++)
            {
                if (postingsArr[i].GetCurrentPositions().IsEmpty)
                {
                    hasAllPositions = false;
                    break;
                }
            }

            if (hasAllPositions && HasPositionsWithinSlopSpan(postingsArr, termCount, leaderIdx, slop))
            {
                collector.Collect(docBase + docId, score);
                continue;
            }

            if (!hasAllPositions && slop == 0)
            {
                var stored = reader.GetStoredFields(docId);
                if (stored.TryGetValue(query.Field, out var values) && values.Count > 0 && ContainsPhrase(values[0], query.Terms))
                    collector.Collect(docBase + docId, score);
            }
        }

        for (int i = 0; i < termCount; i++)
            postingsArr[i].Dispose();
    }

    /// <summary>
    /// Checks whether positions from all terms form a valid phrase within the given slop,
    /// using ReadOnlySpan positions from PostingsEnum.
    /// </summary>
    private static bool HasPositionsWithinSlopSpan(Span<PostingsEnum> postings, int termCount, int leaderIdx, int slop)
    {
        if (termCount == 1) return true;

        // For 2 terms (common case): optimised inline check
        if (termCount == 2)
        {
            var pos0 = postings[0].GetCurrentPositions();
            var pos1 = postings[1].GetCurrentPositions();
            for (int i = 0; i < pos0.Length; i++)
            {
                for (int j = 0; j < pos1.Length; j++)
                {
                    int diff = pos1[j] - pos0[i] - 1;
                    if (slop == 0 ? diff == 0 : Math.Abs(diff) <= slop)
                        return true;
                }
            }
            return false;
        }

        // General case: check all position combinations
        // Collect positions into a temporary list for HasPositionsWithinSlop
        var termPositions = new List<int[]>(termCount);
        for (int i = 0; i < termCount; i++)
        {
            var span = postings[i].GetCurrentPositions();
            termPositions.Add(span.ToArray());
        }
        return HasPositionsWithinSlop(termPositions, slop);
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
                int docLength = reader.GetFieldLength(docId, query.Field);
                float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
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
                int docLength = reader.GetFieldLength(docId, query.Field);
                float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecuteFuzzyQuery(FuzzyQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var matchingTerms = reader.GetFuzzyMatches(fieldPrefix, query.Term.AsSpan(), query.MaxEdits);
        if (matchingTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;
        var queryTermSpan = query.Term.AsSpan();

        foreach (var (qualifiedTerm, _) in matchingTerms)
        {
            var termPart = qualifiedTerm.AsSpan(query.Field.Length + 1);
            int distance = LevenshteinDistance.Compute(queryTermSpan, termPart);

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
                int docLength = reader.GetFieldLength(docId, query.Field);
                float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq) * distanceFactor;
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecuteTermRangeQuery(TermRangeQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var matchingTerms = reader.GetTermsInRange(fieldPrefix, query.LowerTerm, query.UpperTerm,
            query.IncludeLower, query.IncludeUpper);
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
                int docLength = reader.GetFieldLength(docId, query.Field);
                float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
                if (boost != 1.0f) score *= boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private void ExecuteConstantScoreQuery(ConstantScoreQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        // Execute the inner query into a temporary collector, then replace scores
        var innerCollector = new TopNCollector(10000);
        ExecuteQuery(query.Inner, reader, globalDFs, ref innerCollector);

        float constantScore = query.ConstantScore;
        if (query.Boost != 1.0f) constantScore *= query.Boost;

        foreach (var sd in innerCollector.ToTopDocs().ScoreDocs)
            collector.Collect(sd.DocId, constantScore);
    }

    private void ExecuteDisjunctionMaxQuery(DisjunctionMaxQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        if (query.Disjuncts.Count == 0) return;

        // Collect per-docId: max score + all scores for tiebreaker
        var docScores = new Dictionary<int, (float Max, float OtherSum)>();

        foreach (var disjunct in query.Disjuncts)
        {
            var subCollector = new TopNCollector(10000);
            ExecuteQuery(disjunct, reader, globalDFs, ref subCollector);

            foreach (var sd in subCollector.ToTopDocs().ScoreDocs)
            {
                if (docScores.TryGetValue(sd.DocId, out var existing))
                {
                    if (sd.Score > existing.Max)
                        docScores[sd.DocId] = (sd.Score, existing.OtherSum + existing.Max);
                    else
                        docScores[sd.DocId] = (existing.Max, existing.OtherSum + sd.Score);
                }
                else
                {
                    docScores[sd.DocId] = (sd.Score, 0f);
                }
            }
        }

        float tieBreaker = query.TieBreakerMultiplier;
        float boost = query.Boost;
        foreach (var (docId, (max, otherSum)) in docScores)
        {
            float score = max + tieBreaker * otherSum;
            if (boost != 1.0f) score *= boost;
            collector.Collect(docId, score);
        }
    }

    private void ExecuteRegexpQuery(RegexpQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var matchingTerms = reader.GetTermsMatchingRegex(fieldPrefix, query.CompiledRegex);
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
                int docLength = reader.GetFieldLength(docId, query.Field);
                float score = _similarity.Score(tf, docLength, avgDocLength, _totalDocCount, docFreq);
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
            case ConstantScoreQuery csq:
                CollectTerms(csq.Inner, terms);
                break;
            case DisjunctionMaxQuery dmq:
                foreach (var d in dmq.Disjuncts)
                    CollectTerms(d, terms);
                break;
            // Expansion queries (prefix/wildcard/fuzzy/range/regexp) resolve terms at execution
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
        var fieldLengthSums = new Dictionary<string, long>(StringComparer.Ordinal);
        var fieldDocCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var reader in _readers)
        {
            for (int docId = 0; docId < reader.MaxDoc; docId++)
            {
                if (!reader.IsLive(docId)) continue;
                liveDocCount++;

                // Accumulate per-field lengths
                foreach (var field in reader.Info.FieldNames)
                {
                    int fieldLen = reader.GetFieldLength(docId, field);
                    fieldLengthSums[field] = fieldLengthSums.GetValueOrDefault(field) + fieldLen;
                    fieldDocCounts[field] = fieldDocCounts.GetValueOrDefault(field) + 1;
                }
            }
        }

        var avgFieldLengths = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var (field, sum) in fieldLengthSums)
        {
            int count = fieldDocCounts.GetValueOrDefault(field, 1);
            avgFieldLengths[field] = count > 0 ? (float)sum / count : 1.0f;
        }

        return new IndexStats(_totalDocCount, liveDocCount, avgFieldLengths, fieldDocCounts);
    }

    // --- S2-5: Faceted search ---

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

    // --- S2-6: FunctionScoreQuery ---

    private void ExecuteFunctionScoreQuery(FunctionScoreQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        // Execute inner query into temporary collector
        var innerCollector = new TopNCollector(10000);
        ExecuteQuery(query.Inner, reader, globalDFs, ref innerCollector);
        var innerDocs = innerCollector.ToTopDocs();

        int docBase = reader.DocBase;
        foreach (var sd in innerDocs.ScoreDocs)
        {
            int localDocId = sd.DocId - docBase;
            if (reader.TryGetNumericValue(query.NumericField, localDocId, out double fieldValue))
            {
                float combined = FunctionScoreQuery.Combine(sd.Score, fieldValue, query.Mode);
                collector.Collect(sd.DocId, combined * query.Boost);
            }
            else
            {
                collector.Collect(sd.DocId, sd.Score * query.Boost);
            }
        }
    }

    // --- S2-4: SpanQuery execution ---

    private void ExecuteSpanNearQuery(SpanNearQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        // Collect spans per clause
        var clauseSpans = new List<List<Span>>(query.Clauses.Count);
        foreach (var clause in query.Clauses)
        {
            var spans = CollectSpans(clause, reader);
            if (spans.Count == 0) return; // AND semantics: all clauses must match
            clauseSpans.Add(spans);
        }

        // Find documents present in all clause spans
        var docSets = clauseSpans.Select(s => s.Select(sp => sp.DocId).ToHashSet()).ToList();
        var commonDocs = docSets[0];
        for (int i = 1; i < docSets.Count; i++)
            commonDocs.IntersectWith(docSets[i]);

        int docBase = reader.DocBase;
        foreach (int docId in commonDocs)
        {
            // Check positional constraints
            var clausePositions = clauseSpans
                .Select(s => s.Where(sp => sp.DocId == docId).Select(sp => sp.Start).OrderBy(p => p).ToList())
                .ToList();

            if (CheckNearConstraint(clausePositions, query.Slop, query.InOrder))
            {
                float score = 1.0f * query.Boost;
                collector.Collect(docBase + docId, score);
            }
        }
    }

    private static bool CheckNearConstraint(List<List<int>> clausePositions, int slop, bool inOrder)
    {
        // Simple check: for each position in clause[0], find matching positions in other clauses
        foreach (int pos0 in clausePositions[0])
        {
            bool allMatch = true;
            int prevPos = pos0;
            for (int c = 1; c < clausePositions.Count; c++)
            {
                bool found = false;
                foreach (int posC in clausePositions[c])
                {
                    int distance = Math.Abs(posC - prevPos);
                    if (distance <= slop + 1)
                    {
                        if (inOrder && posC <= prevPos) continue;
                        prevPos = posC;
                        found = true;
                        break;
                    }
                }
                if (!found) { allMatch = false; break; }
            }
            if (allMatch) return true;
        }
        return false;
    }

    private void ExecuteSpanOrQuery(SpanOrQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var seen = new HashSet<int>();
        int docBase = reader.DocBase;
        foreach (var clause in query.Clauses)
        {
            var spans = CollectSpans(clause, reader);
            foreach (var span in spans)
            {
                if (seen.Add(span.DocId))
                    collector.Collect(docBase + span.DocId, 1.0f * query.Boost);
            }
        }
    }

    private void ExecuteSpanNotQuery(SpanNotQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var includeSpans = CollectSpans(query.Include, reader);
        var excludeSpans = CollectSpans(query.Exclude, reader);

        // Exclude documents that have any exclude span
        var excludedDocs = new HashSet<int>();
        foreach (var s in excludeSpans)
            excludedDocs.Add(s.DocId);

        int docBase = reader.DocBase;
        var seen = new HashSet<int>();
        foreach (var span in includeSpans)
        {
            if (!excludedDocs.Contains(span.DocId) && seen.Add(span.DocId))
                collector.Collect(docBase + span.DocId, 1.0f * query.Boost);
        }
    }

    private List<Span> CollectSpans(SpanQuery query, SegmentReader reader)
    {
        var spans = new List<Span>();
        switch (query)
        {
            case SpanTermQuery stq:
            {
                var qt = string.Concat(stq.Field, "\x00", stq.Term);
                using var pe = reader.GetPostingsEnumWithPositions(qt);
                while (pe.MoveNext())
                {
                    var positions = pe.GetCurrentPositions();
                    foreach (int pos in positions)
                        spans.Add(new Span(pe.DocId, pos, pos + 1));
                }
                break;
            }
            case SpanNearQuery snq:
            {
                // Recursive: collect matching spans
                var clauseSpans = snq.Clauses.Select(c => CollectSpans(c, reader)).ToList();
                var commonDocs = clauseSpans[0].Select(s => s.DocId).ToHashSet();
                for (int i = 1; i < clauseSpans.Count; i++)
                    commonDocs.IntersectWith(clauseSpans[i].Select(s => s.DocId));

                foreach (int docId in commonDocs)
                {
                    var clausePositions = clauseSpans
                        .Select(s => s.Where(sp => sp.DocId == docId).Select(sp => sp.Start).OrderBy(p => p).ToList())
                        .ToList();
                    if (CheckNearConstraint(clausePositions, snq.Slop, snq.InOrder))
                    {
                        int minPos = clausePositions.Min(p => p.Min());
                        int maxPos = clausePositions.Max(p => p.Max());
                        spans.Add(new Span(docId, minPos, maxPos + 1));
                    }
                }
                break;
            }
            case SpanOrQuery soq:
                foreach (var clause in soq.Clauses)
                    spans.AddRange(CollectSpans(clause, reader));
                break;
            case SpanNotQuery snotq:
            {
                var includeSpans = CollectSpans(snotq.Include, reader);
                var excludeSpans = CollectSpans(snotq.Exclude, reader);
                var excludedDocs = new HashSet<int>();
                foreach (var s in excludeSpans)
                    excludedDocs.Add(s.DocId);
                foreach (var span in includeSpans)
                {
                    if (!excludedDocs.Contains(span.DocId))
                        spans.Add(span);
                }
                break;
            }
        }
        return spans;
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
