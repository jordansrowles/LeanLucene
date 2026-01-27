using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Index;
using Rowles.LeanLucene.Search.Geo;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Partial class containing specialised query execution methods (Prefix, Wildcard, Fuzzy, Range, Regex, etc.).
/// </summary>
public sealed partial class IndexSearcher
{
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

    private void ExecutePrefixQuery(PrefixQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var qualifiedPrefix = $"{query.Field}\x00{query.Prefix}";
        var matchingTerms = reader.GetTermsWithPrefix(qualifiedPrefix);
        if (matchingTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;

        foreach (var (qualifiedTerm, postingsOffset) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnumAtOffset(postingsOffset);
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

        foreach (var (qualifiedTerm, postingsOffset) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnumAtOffset(postingsOffset);
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

        foreach (var (qualifiedTerm, postingsOffset) in matchingTerms)
        {
            var termPart = qualifiedTerm.AsSpan(query.Field.Length + 1);
            int distance = LevenshteinDistance.Compute(queryTermSpan, termPart);

            using var postings = reader.GetPostingsEnumAtOffset(postingsOffset);
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

        foreach (var (qualifiedTerm, postingsOffset) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnumAtOffset(postingsOffset);
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

    private void ExecuteRegexpQuery(RegexpQuery query, SegmentReader reader,
        Dictionary<(string Field, string Term), int> globalDFs, ref TopNCollector collector)
    {
        var fieldPrefix = $"{query.Field}\x00";
        var matchingTerms = reader.GetTermsMatchingRegex(fieldPrefix, query.CompiledRegex);
        if (matchingTerms.Count == 0) return;

        float boost = query.Boost;
        float avgDocLength = _stats.GetAvgFieldLength(query.Field);
        int docBase = reader.DocBase;

        foreach (var (qualifiedTerm, postingsOffset) in matchingTerms)
        {
            using var postings = reader.GetPostingsEnumAtOffset(postingsOffset);
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

    private void ExecuteGeoBoundingBoxQuery(GeoBoundingBoxQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        int docBase = reader.DocBase;
        float score = query.Boost != 1.0f ? query.Boost : 1.0f;
        string latField = query.Field + "_lat";
        string lonField = query.Field + "_lon";

        // Use numeric range index on lat to get candidates
        var latCandidates = reader.GetNumericRange(latField, query.MinLat, query.MaxLat);
        if (latCandidates.Count == 0) return;

        foreach (var (docId, lat) in latCandidates)
        {
            if (!reader.IsLive(docId)) continue;
            if (!reader.TryGetNumericValue(lonField, docId, out double lon)) continue;
            if (lon >= query.MinLon && lon <= query.MaxLon)
                collector.Collect(docBase + docId, score);
        }
    }

    private void ExecuteGeoDistanceQuery(GeoDistanceQuery query, SegmentReader reader, ref TopNCollector collector)
    {
        int docBase = reader.DocBase;
        float score = query.Boost != 1.0f ? query.Boost : 1.0f;
        string latField = query.Field + "_lat";
        string lonField = query.Field + "_lon";

        // Compute a conservative bounding box for the distance to narrow candidates
        double latDelta = query.RadiusMetres / 111_320.0; // ~111km per degree lat
        double lonDelta = query.RadiusMetres / (111_320.0 * Math.Cos(query.CentreLat * Math.PI / 180.0));
        double minLat = query.CentreLat - latDelta;
        double maxLat = query.CentreLat + latDelta;

        var latCandidates = reader.GetNumericRange(latField, minLat, maxLat);
        if (latCandidates.Count == 0) return;

        double minLon = query.CentreLon - lonDelta;
        double maxLon = query.CentreLon + lonDelta;

        foreach (var (docId, lat) in latCandidates)
        {
            if (!reader.IsLive(docId)) continue;
            if (!reader.TryGetNumericValue(lonField, docId, out double lon)) continue;
            if (lon < minLon || lon > maxLon) continue;

            // Precise Haversine check
            double dist = GeoEncodingUtils.HaversineDistance(query.CentreLat, query.CentreLon, lat, lon);
            if (dist <= query.RadiusMetres)
                collector.Collect(docBase + docId, score);
        }
    }
}
