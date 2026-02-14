using System.Buffers;
using System.Runtime.CompilerServices;
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Holds a snapshot of segment readers and executes queries across all segments.
/// </summary>
public sealed partial class IndexSearcher : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly List<SegmentReader> _readers = [];
    private readonly int[] _docBases;
    private readonly int _totalDocCount;
    private readonly IndexStats _stats;
    private readonly ISimilarity _similarity;
    private readonly IndexSearcherConfig _config;
    private PostingsEnum[]? _postingsBuffer;
    private ScoreDoc[]? _collectorHeapCache;
    private readonly QueryCache? _queryCache;

    /// <summary>Corpus-wide statistics computed at construction.</summary>
    public IndexStats Stats => _stats;

    /// <summary>The query result cache, or null if caching is disabled.</summary>
    public QueryCache? Cache => _queryCache;

    /// <summary>The metrics collector for this searcher.</summary>
    public Diagnostics.IMetricsCollector Metrics => _config.Metrics;

    /// <summary>Exposes the underlying segment readers for advanced use (e.g., spelling suggestions).</summary>
    internal IReadOnlyList<SegmentReader> GetSegmentReaders() => _readers;

    /// <summary>Calculates the on-disk size of the index.</summary>
    public Diagnostics.IndexSizeReport GetIndexSize()
        => Diagnostics.IndexSizeCalculator.Calculate(_directory.DirectoryPath);

    public IndexSearcher(MMapDirectory directory, ISimilarity? similarity = null)
        : this(directory, new IndexSearcherConfig { Similarity = similarity ?? Bm25Similarity.Instance })
    {
    }

    public IndexSearcher(MMapDirectory directory, IndexSearcherConfig config)
    {
        _directory = directory;
        _config = config;
        _similarity = config.Similarity;

        var (segmentIds, generation) = LoadLatestCommitWithGeneration();
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

        // Try to load persisted stats first; fall back to expensive recomputation
        var statsPath = IndexStats.GetStatsPath(directory.DirectoryPath, generation);
        _stats = IndexStats.TryLoadFrom(statsPath) ?? ComputeStats();

        if (config.EnableQueryCache)
            _queryCache = new QueryCache(config.QueryCacheMaxEntries);
    }

    public IndexSearcher(MMapDirectory directory, IReadOnlyList<SegmentInfo> segments, ISimilarity? similarity = null)
        : this(directory, segments, new IndexSearcherConfig { Similarity = similarity ?? Bm25Similarity.Instance })
    {
    }

    public IndexSearcher(MMapDirectory directory, IReadOnlyList<SegmentInfo> segments, IndexSearcherConfig config)
    {
        _directory = directory;
        _config = config;
        _similarity = config.Similarity;
        foreach (var info in segments)
            _readers.Add(new SegmentReader(directory, info));

        _docBases = AssignDocBases();
        _totalDocCount = _docBases.Length > 0
            ? _docBases[^1] + _readers[^1].MaxDoc
            : 0;
        _stats = ComputeStats();

        if (config.EnableQueryCache)
            _queryCache = new QueryCache(config.QueryCacheMaxEntries);
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

        // Check query cache
        if (_queryCache is not null)
        {
            var cached = _queryCache.TryGet(query, topN);
            if (cached is not null)
            {
                _config.Metrics.RecordCacheHit();
                return cached;
            }
            _config.Metrics.RecordCacheMiss();
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = SearchCore(query, topN);
        sw.Stop();
        _config.Metrics.RecordSearchLatency(sw.Elapsed);

        _config.SlowQueryLog?.MaybeLog(query, sw.Elapsed, result.TotalHits);
        _config.SearchAnalytics?.Record(query, sw.Elapsed, result.TotalHits,
            _queryCache is not null && _queryCache.TryGet(query, topN) is not null);

        _queryCache?.Put(query, topN, result);
        return result;
    }

    private TopDocs SearchCore(Query query, int topN)
    {
        // MoreLikeThis is a cross-segment query: extract terms, build BooleanQuery, delegate
        if (query is MoreLikeThisQuery mlt)
            return ExecuteMoreLikeThis(mlt, topN);

        // RRF: execute each child query independently, then fuse by rank
        if (query is RrfQuery rrf)
            return ExecuteRrfQuery(rrf, topN);

        // Block join: execute child query, map results to parent docs
        if (query is BlockJoinQuery bjq)
            return ExecuteBlockJoinQuery(bjq, topN);

        // Fast path for the most common query type — avoids
        // PrecomputeGlobalDocFreqs allocation and does only 1 dictionary
        // lookup per segment instead of 2.
        if (query is TermQuery tq)
            return SearchTermQuery(tq, topN);

        // Fast path for BooleanQuery with all-TermQuery clauses — compute
        // global DFs inline without the generic PrecomputeGlobalDocFreqs tree walk
        if (query is BooleanQuery bq && IsAllTermQueryBoolean(bq))
            return SearchBooleanTermQueryFast(bq, topN);

        // Pattern-based queries (Prefix, Wildcard, Fuzzy) don't have static terms,
        // so PrecomputeGlobalDocFreqs produces an empty dictionary. Skip the tree walk.
        bool skipGlobalDFs = query is PrefixQuery or WildcardQuery or FuzzyQuery;
        var globalDFs = skipGlobalDFs
            ? new Dictionary<(string Field, string Term), int>()
            : PrecomputeGlobalDocFreqs(query);
        var collector = new TopNCollector(topN);

        if (_readers.Count == 1 || !_config.ParallelSearch)
        {
            foreach (var reader in _readers)
                ExecuteQuery(query, reader, globalDFs, ref collector);
        }
        else
        {
            int maxDop = _config.MaxConcurrency > 0 ? _config.MaxConcurrency : Environment.ProcessorCount;
            var lockObj = new Lock();
            Parallel.ForEach(_readers, new ParallelOptions { MaxDegreeOfParallelism = maxDop }, reader =>
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

    private (List<string> SegmentIds, int Generation) LoadLatestCommitWithGeneration()
    {
        var recovery = IndexRecovery.RecoverLatestCommit(_directory.DirectoryPath);
        return recovery is not null
            ? (recovery.SegmentIds, recovery.Generation)
            : ([], 0);
    }

    private List<string> LoadLatestCommit()
    {
        var (ids, _) = LoadLatestCommitWithGeneration();
        return ids;
    }

    public void Dispose()
    {
        foreach (var reader in _readers)
            reader.Dispose();
    }

    private TopDocs ExecuteRrfQuery(RrfQuery rrf, int topN)
    {
        if (rrf.Queries.Count == 0) return TopDocs.Empty;

        // Execute each child query independently to get ranked result lists
        var childResults = new TopDocs[rrf.Queries.Count];
        for (int i = 0; i < rrf.Queries.Count; i++)
            childResults[i] = SearchCore(rrf.Queries[i], topN);

        return RrfQuery.Combine(childResults, topN, rrf.K);
    }

    private TopDocs ExecuteBlockJoinQuery(BlockJoinQuery bjq, int topN)
    {
        // Map child docs directly to parent docs using a bitset — no intermediate List<int>
        var parentBits = ArrayPool<bool>.Shared.Rent(_totalDocCount);
        Array.Clear(parentBits, 0, _totalDocCount);
        int parentCount = 0;

        try
        {
            // Stream child doc IDs directly into parent mapping without materialising TopDocs
            CollectParentsFromChildQuery(bjq.ChildQuery, parentBits, ref parentCount);

            if (parentCount == 0)
                return TopDocs.Empty;

            // Collect parent doc IDs from the bitset directly into TopNCollector
            int take = Math.Min(parentCount, topN);
            float score = bjq.Boost;
            var collector = new TopNCollector(take);
            for (int i = 0; i < _totalDocCount && collector.TotalHits < parentCount; i++)
            {
                if (parentBits[i])
                    collector.Collect(i, score);
            }

            return collector.ToTopDocs();
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(parentBits, clearArray: false);
        }
    }

    /// <summary>
    /// Executes a child query once across all segments, mapping each matching child doc ID to its parent
    /// and setting the parent bit directly — uses a single SearchCore call instead of per-segment collectors.
    /// </summary>
    private void CollectParentsFromChildQuery(Query childQuery, bool[] parentBits, ref int parentCount)
    {
        // Single-pass child query execution: one collector across all segments
        var childDocs = SearchCore(childQuery, _totalDocCount);

        foreach (var sd in childDocs.ScoreDocs)
        {
            int globalDocId = sd.DocId;

            // Binary search to find which segment owns this doc ID
            int r = Array.BinarySearch(_docBases, globalDocId);
            if (r < 0) r = ~r - 1;
            if (r < 0) continue;

            var reader = _readers[r];
            int docBase = _docBases[r];
            int localDocId = globalDocId - docBase;
            var pbs = reader.GetParentBitSet();

            if (pbs is not null)
            {
                int parentLocal = pbs.NextParent(localDocId);
                if (parentLocal >= 0)
                {
                    int globalParent = docBase + parentLocal;
                    if (!parentBits[globalParent])
                    {
                        parentBits[globalParent] = true;
                        parentCount++;
                    }
                }
            }
        }
    }

    // Reusable buffer for PrecomputeGlobalDocFreqs to avoid per-call HashSet allocation
    private readonly HashSet<(string Field, string Term)> _docFreqTermsBuf = new();

    private Dictionary<(string Field, string Term), int> PrecomputeGlobalDocFreqs(Query query)
    {
        _docFreqTermsBuf.Clear();
        CollectTerms(query, _docFreqTermsBuf);

        var result = new Dictionary<(string Field, string Term), int>(_docFreqTermsBuf.Count);
        foreach (var (field, term) in _docFreqTermsBuf)
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

}
