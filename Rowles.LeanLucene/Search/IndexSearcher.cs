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
        var mustSets = new List<HashSet<int>>();
        var shouldSets = new List<HashSet<int>>();
        var mustNotSets = new List<HashSet<int>>();
        var scoreMap = new Dictionary<int, float>();

        foreach (var clause in query.Clauses)
        {
            var subResults = ExecuteSubQuery(clause.Query, reader, globalDFs);
            var idSet = new HashSet<int>(subResults.Count);

            foreach (var sr in subResults)
            {
                idSet.Add(sr.DocId);
                scoreMap[sr.DocId] = scoreMap.GetValueOrDefault(sr.DocId) + sr.Score;
            }

            switch (clause.Occur)
            {
                case Occur.Must:
                    mustSets.Add(idSet);
                    break;
                case Occur.Should:
                    shouldSets.Add(idSet);
                    break;
                case Occur.MustNot:
                    mustNotSets.Add(idSet);
                    break;
            }
        }

        HashSet<int> candidates;

        if (mustSets.Count > 0)
        {
            candidates = new HashSet<int>(mustSets[0]);
            for (int i = 1; i < mustSets.Count; i++)
                candidates.IntersectWith(mustSets[i]);
        }
        else if (shouldSets.Count > 0)
        {
            candidates = new HashSet<int>();
            foreach (var s in shouldSets)
                candidates.UnionWith(s);
        }
        else
        {
            return;
        }

        foreach (var s in mustNotSets)
            candidates.ExceptWith(s);

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
        var termDocSets = new List<HashSet<int>>(query.Terms.Length);
        foreach (var t in query.Terms)
            termDocSets.Add(new HashSet<int>(reader.GetDocIds(query.Field, t)));

        if (termDocSets.Count == 0) return;

        var candidates = new HashSet<int>(termDocSets[0]);
        for (int i = 1; i < termDocSets.Count; i++)
            candidates.IntersectWith(termDocSets[i]);

        int docBase = reader.DocBase;
        float boost = query.Boost;
        foreach (var docId in candidates)
        {
            if (!reader.IsLive(docId)) continue;

            var termPositions = new List<int[]>(query.Terms.Length);
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
                float score = boost != 1.0f ? boost : 1.0f;
                collector.Collect(docBase + docId, score);
                continue;
            }

            if (!hasAllPositions && query.Slop == 0)
            {
                var stored = reader.GetStoredFields(docId);
                if (stored.TryGetValue(query.Field, out var text) && ContainsPhrase(text, query.Terms))
                    collector.Collect(docBase + docId, boost != 1.0f ? boost : 1.0f);
            }
        }
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
        // Tokenise the text and look for consecutive matches
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .ToArray();

        var lowerTerms = terms.Select(t => t.ToLowerInvariant()).ToArray();

        for (int i = 0; i <= words.Length - lowerTerms.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < lowerTerms.Length; j++)
            {
                if (words[i + j] != lowerTerms[j])
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
