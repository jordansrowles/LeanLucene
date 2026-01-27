using System.Buffers;
using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Index;

namespace Rowles.LeanLucene.Search.Searcher;

/// <summary>
/// Partial class containing phrase query execution and position-based scoring logic.
/// </summary>
public sealed partial class IndexSearcher
{
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
            }
            // No fallback to stored fields — positions are required for phrase matching
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

        // For 2 terms (common case): O(n+m) two-pointer merge on sorted positions
        if (termCount == 2)
        {
            var pos0 = postings[0].GetCurrentPositions();
            var pos1 = postings[1].GetCurrentPositions();
            int j = 0;
            for (int i = 0; i < pos0.Length; i++)
            {
                int target = pos0[i] + 1;
                int lowerBound = target - slop;
                int upperBound = target + slop;
                while (j < pos1.Length && pos1[j] < lowerBound)
                    j++;
                if (j >= pos1.Length) break;
                if (pos1[j] <= upperBound)
                    return true;
            }
            return false;
        }

        // 3-term specialisation: two-pointer chain O(p0 + p1 + p2)
        if (termCount == 3)
        {
            var p0 = postings[0].GetCurrentPositions();
            var p1 = postings[1].GetCurrentPositions();
            var p2 = postings[2].GetCurrentPositions();
            int j = 0, k = 0;
            for (int i = 0; i < p0.Length; i++)
            {
                int target1 = p0[i] + 1;
                int lo1 = target1 - slop;
                int hi1 = target1 + slop;
                while (j < p1.Length && p1[j] < lo1) j++;
                if (j >= p1.Length) break;
                if (p1[j] > hi1) continue;

                int target2 = p1[j] + 1;
                int lo2 = target2 - slop;
                int hi2 = target2 + slop;
                while (k < p2.Length && p2[k] < lo2) k++;
                if (k >= p2.Length) break;
                if (p2[k] <= hi2) return true;
            }
            return false;
        }

        // General case: check all position combinations
        // Use ArrayPool to avoid heap allocations
        var rentedArrays = new int[termCount][];
        Span<int> actualLengths = stackalloc int[termCount];
        var termPositions = new List<int[]>(termCount);
        
        try
        {
            for (int i = 0; i < termCount; i++)
            {
                var span = postings[i].GetCurrentPositions();
                var rented = ArrayPool<int>.Shared.Rent(span.Length);
                span.CopyTo(rented);
                rentedArrays[i] = rented;
                actualLengths[i] = span.Length;
                termPositions.Add(rented);
            }
            return HasPositionsWithinSlop(termPositions, actualLengths, slop);
        }
        finally
        {
            for (int i = 0; i < termCount; i++)
            {
                if (rentedArrays[i] != null)
                    ArrayPool<int>.Shared.Return(rentedArrays[i]);
            }
        }
    }

    private static bool HasPositionsWithinSlop(List<int[]> termPositions, ReadOnlySpan<int> actualLengths, int slop)
    {
        int firstTermLength = actualLengths[0];
        int[] firstTermArray = termPositions[0];
        
        for (int startIdx = 0; startIdx < firstTermLength; startIdx++)
        {
            int startPos = firstTermArray[startIdx];
            bool match = true;
            
            for (int i = 1; i < termPositions.Count; i++)
            {
                int expectedPos = startPos + i;
                bool found = false;
                int currentLength = actualLengths[i];
                int[] currentArray = termPositions[i];

                if (slop == 0)
                {
                    found = Array.BinarySearch(currentArray, 0, currentLength, expectedPos) >= 0;
                }
                else
                {
                    // Search for any position within [expectedPos - slop, expectedPos + slop]
                    int idx = Array.BinarySearch(currentArray, 0, currentLength, expectedPos - slop);
                    if (idx < 0) idx = ~idx;
                    for (int j = idx; j < currentLength; j++)
                    {
                        int pos = currentArray[j];
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
}
