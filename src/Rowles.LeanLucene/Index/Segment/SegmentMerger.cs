using System.Buffers;
using System.Text.Json;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.DocValues;
using Rowles.LeanLucene.Codecs.Postings;
using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Segment;

/// <summary>
/// Tiered merge policy. When the number of segments at a given size tier
/// exceeds a configurable threshold, the smallest segments in that tier
/// are merged into one. Old segments are removed only after the merged
/// segment is fully committed.
/// </summary>
public sealed class SegmentMerger
{
    private readonly MMapDirectory _directory;
    private readonly int _mergeThreshold;
    private readonly int _skipInterval;

    /// <summary>Default merge threshold: when this many segments exist, merge.</summary>
    public const int DefaultMergeThreshold = 10;

    /// <summary>Default postings skip interval.</summary>
    public const int DefaultSkipInterval = 128;

    public SegmentMerger(MMapDirectory directory, int mergeThreshold = DefaultMergeThreshold, int skipInterval = DefaultSkipInterval)
    {
        _directory = directory;
        _mergeThreshold = mergeThreshold;
        _skipInterval = skipInterval;
    }

    /// <summary>
    /// Checks if a merge is needed and performs it. Returns the updated segment list.
    /// </summary>
    public List<SegmentInfo> MaybeMerge(List<SegmentInfo> segments, ref int nextSegmentOrdinal)
    {
        if (segments.Count < _mergeThreshold)
            return segments;

        // Group segments by size tier without LINQ allocations
        var tierBuckets = new Dictionary<int, List<SegmentInfo>>();
        foreach (var s in segments)
        {
            int tier = GetSizeTier(s.DocCount);
            if (!tierBuckets.TryGetValue(tier, out var bucket))
            {
                bucket = new List<SegmentInfo>();
                tierBuckets[tier] = bucket;
            }
            bucket.Add(s);
        }

        // Collect tiers that meet the merge threshold, sorted by tier key
        var eligibleTiers = new List<int>();
        foreach (var (tier, bucket) in tierBuckets)
        {
            if (bucket.Count >= _mergeThreshold)
                eligibleTiers.Add(tier);
        }

        if (eligibleTiers.Count == 0)
            return segments;

        eligibleTiers.Sort();
        var result = new List<SegmentInfo>(segments);

        foreach (var tierKey in eligibleTiers)
        {
            var bucket = tierBuckets[tierKey];
            // Take the smallest segments in this tier
            bucket.Sort(static (a, b) => a.DocCount.CompareTo(b.DocCount));
            var toMerge = bucket.Count <= _mergeThreshold ? bucket : bucket.GetRange(0, _mergeThreshold);

            if (toMerge.Count < 2)
                continue;

            var merged = MergeSegments(toMerge, ref nextSegmentOrdinal);
            if (merged == null)
                continue;

            // Remove merged segments, add the new one
            foreach (var seg in toMerge)
            {
                result.Remove(seg);
                CleanupSegmentFiles(seg);
            }
            result.Add(merged);
        }

        return result;
    }

    private SegmentInfo? MergeSegments(List<SegmentInfo> segments, ref int nextSegmentOrdinal)
    {
        var newSegId = $"seg_{nextSegmentOrdinal++}";
        var basePath = Path.Combine(_directory.DirectoryPath, newSegId);

        // Collect all postings from all segments, re-mapping doc IDs
        var allPostings = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
        var allFreqs = new Dictionary<string, Dictionary<int, int>>();
        var allStoredFields = new List<Dictionary<string, List<string>>>();
        var allNumericFields = new Dictionary<string, Dictionary<int, double>>();
        var allVectors = new List<float[]>();
        var fieldNames = new HashSet<string>(StringComparer.Ordinal);

        // Collect the union of all field names across the source segments.
        foreach (var segInfo in segments)
        {
            foreach (var field in segInfo.FieldNames)
                fieldNames.Add(field);
        }

        // Re-read each segment's postings via the term dictionary, remapping doc IDs.
        int newDocId = 0;
        var allPositions = new Dictionary<string, Dictionary<int, int[]>>();

        foreach (var segInfo in segments)
        {
            var segBasePath = Path.Combine(_directory.DirectoryPath, segInfo.SegmentId);
            using var reader = new SegmentReader(_directory, segInfo);

            // Build old→new doc ID mapping (only live docs)
            var docIdMap = new Dictionary<int, int>();
            for (int oldDocId = 0; oldDocId < segInfo.DocCount; oldDocId++)
            {
                if (reader.IsLive(oldDocId))
                {
                    docIdMap[oldDocId] = newDocId++;
                }
            }

            // Re-read the .pos file to get all postings with their qualified terms
            RemapPostings(segBasePath + ".pos", segBasePath + ".dic", docIdMap, allPostings, allFreqs, allPositions);
        }

        int totalDocs = newDocId;
        if (totalDocs == 0)
            return null;

        // Lazily-populated per-field column-stride codec arrays (.fln, .dvn, .dvs).
        // We don't pre-seed by FieldNames because numeric fields aren't always present
        // in segInfo.FieldNames yet still produce DocValues / lengths.
        var allFieldLengths = new Dictionary<string, int[]>(StringComparer.Ordinal);
        var allNumericDocValues = new Dictionary<string, double[]>(StringComparer.Ordinal);
        var allSortedDocValues = new Dictionary<string, string?[]>(StringComparer.Ordinal);

        // Pre-check whether any source segment has term vectors. If so, every doc in the
        // merged segment must have an entry (empty for docs from segments without TVs).
        bool anyTermVectors = false;
        foreach (var segInfo in segments)
        {
            var tvdPath = Path.Combine(_directory.DirectoryPath, segInfo.SegmentId + ".tvd");
            var tvxPath = Path.Combine(_directory.DirectoryPath, segInfo.SegmentId + ".tvx");
            if (File.Exists(tvdPath) && File.Exists(tvxPath))
            {
                anyTermVectors = true;
                break;
            }
        }
        var allTermVectorDocs = anyTermVectors
            ? new List<Dictionary<string, List<TermVectorEntry>>>(totalDocs)
            : null;

        ParentBitSet? mergedParentBitSet = null;

        int remapDocId = 0;
        foreach (var segInfo in segments)
        {
            using var reader = new SegmentReader(_directory, segInfo);

            // Cache per-segment readers to avoid repeated lookups inside the doc loop.
            bool segHasTermVectors = reader.HasTermVectors;
            var segParentBitSet = reader.GetParentBitSet();

            // Pre-fetch per-field length arrays for this segment by reading .fln directly,
            // which lists every field that has length data (including ones not in FieldNames).
            var flnPath = Path.Combine(_directory.DirectoryPath, segInfo.SegmentId + ".fln");
            var segFieldLengths = FieldLengthReader.TryRead(flnPath)
                ?? new Dictionary<string, int[]>(StringComparer.Ordinal);

            // Pre-fetch per-field column-stride DV arrays for this segment.
            // We read .dvn / .dvs directly (rather than iterating segInfo.FieldNames)
            // because numeric fields aren't always recorded in FieldNames yet still
            // produce DocValues.
            var segNumericDvs = NumericDocValuesReader.Read(
                Path.Combine(_directory.DirectoryPath, segInfo.SegmentId + ".dvn"));
            var segSortedDvs = SortedDocValuesReader.Read(
                Path.Combine(_directory.DirectoryPath, segInfo.SegmentId + ".dvs"));

            for (int oldDocId = 0; oldDocId < segInfo.DocCount; oldDocId++)
            {
                if (!reader.IsLive(oldDocId)) continue;

                var fields = reader.GetStoredFields(oldDocId);
                var mutableFields = new Dictionary<string, List<string>>();
                foreach (var kvp in fields)
                    mutableFields[kvp.Key] = kvp.Value.ToList();
                allStoredFields.Add(mutableFields);

                foreach (var field in segInfo.FieldNames)
                {
                    if (reader.TryGetNumericValue(field, oldDocId, out double numVal))
                    {
                        if (!allNumericFields.TryGetValue(field, out var fieldMap))
                        {
                            fieldMap = new Dictionary<int, double>();
                            allNumericFields[field] = fieldMap;
                        }
                        fieldMap[remapDocId] = numVal;
                    }
                }

                // Field lengths (.fln) — one entry per (field, doc).
                foreach (var (field, fl) in segFieldLengths)
                {
                    if ((uint)oldDocId >= (uint)fl.Length) continue;
                    if (!allFieldLengths.TryGetValue(field, out var dst))
                    {
                        dst = new int[totalDocs];
                        allFieldLengths[field] = dst;
                    }
                    dst[remapDocId] = fl[oldDocId];
                }

                // Column-stride numeric DocValues (.dvn).
                foreach (var (field, arr) in segNumericDvs)
                {
                    if ((uint)oldDocId >= (uint)arr.Length) continue;
                    if (!allNumericDocValues.TryGetValue(field, out var dst))
                    {
                        dst = new double[totalDocs];
                        allNumericDocValues[field] = dst;
                    }
                    dst[remapDocId] = arr[oldDocId];
                }

                // Column-stride sorted DocValues (.dvs).
                foreach (var (field, arr) in segSortedDvs)
                {
                    if ((uint)oldDocId >= (uint)arr.Length) continue;
                    if (!allSortedDocValues.TryGetValue(field, out var dst))
                    {
                        dst = new string?[totalDocs];
                        allSortedDocValues[field] = dst;
                    }
                    dst[remapDocId] = arr[oldDocId];
                }

                // Term vectors (.tvd/.tvx) — keep alignment by emitting an empty dict
                // for docs from segments that didn't store vectors.
                if (allTermVectorDocs is not null)
                {
                    Dictionary<string, List<TermVectorEntry>>? tv = segHasTermVectors
                        ? reader.GetTermVectors(oldDocId)
                        : null;
                    allTermVectorDocs.Add(tv ?? []);
                }

                // Parent bitset (.pbs) — remap any source parent docs into the merged space.
                if (segParentBitSet is not null && segParentBitSet.IsParent(oldDocId))
                {
                    mergedParentBitSet ??= new ParentBitSet(totalDocs);
                    mergedParentBitSet.Set(remapDocId);
                }

                if (reader.HasVectors)
                {
                    var vec = reader.GetVector(oldDocId);
                    allVectors.Add(vec ?? []);
                }
                remapDocId++;
            }
        }

        // Write merged segment files
        var sortedTerms = allPostings.Keys.ToList();
        var postingsData = new Dictionary<string, int[]>();
        foreach (var (qt, docList) in allPostings)
            postingsData[qt] = docList.ToArray();

        var postingsOffsets = new Dictionary<string, long>();

        // Write .pos using v3 block-packed format
        using (var posOutput = new Store.IndexOutput(basePath + ".pos"))
        {
            CodecConstants.WriteHeader(posOutput, CodecConstants.PostingsVersion);

            using var blockWriter = new BlockPostingsWriter(posOutput);

            foreach (var qt in sortedTerms)
            {
                var ids = postingsData[qt];
                bool hasFreqsFlag = allFreqs.ContainsKey(qt);
                bool hasPositionsFlag = allPositions.TryGetValue(qt, out var posMap) && posMap.Count > 0;

                // Per-term header
                long headerPos = posOutput.Position;
                posOutput.WriteInt32(0);          // placeholder docFreq
                posOutput.WriteInt64(0L);         // placeholder skipOffset
                posOutput.WriteBoolean(hasFreqsFlag);
                posOutput.WriteBoolean(hasPositionsFlag);
                posOutput.WriteBoolean(false);    // hasPayloads (not preserved through merge)

                blockWriter.StartTerm();
                var freqMap = hasFreqsFlag ? allFreqs[qt] : null;
                foreach (var id in ids)
                    blockWriter.AddPosting(id, freqMap?.GetValueOrDefault(id, 1) ?? 1);
                var meta = blockWriter.FinishTerm();

                // Write positions (VarInt format)
                if (hasPositionsFlag)
                {
                    foreach (var id in ids)
                    {
                        var positions = posMap!.GetValueOrDefault(id, []);
                        posOutput.WriteVarInt(positions.Length);
                        int prevPos = 0;
                        foreach (var p in positions)
                        {
                            posOutput.WriteVarInt(p - prevPos);
                            prevPos = p;
                        }
                    }
                }

                // Fill in header
                long endPos = posOutput.Position;
                posOutput.Seek(headerPos);
                posOutput.WriteInt32(meta.DocFreq);
                posOutput.WriteInt64(meta.SkipOffset);
                posOutput.Seek(endPos);

                postingsOffsets[qt] = headerPos;
            }
        }

        // Write .dic
        TermDictionaryWriter.Write(basePath + ".dic", sortedTerms, postingsOffsets);

        // Write per-field .nrm — carry forward per-field norms from source segments
        var fieldNorms = new Dictionary<string, float[]>(StringComparer.Ordinal);
        foreach (var fieldName in fieldNames)
        {
            var fieldNormsArray = new float[totalDocs];
            int normIdx = 0;
            foreach (var segInfo in segments)
            {
                using var normReader = new SegmentReader(_directory, segInfo);
                for (int oldDocId = 0; oldDocId < segInfo.DocCount; oldDocId++)
                {
                    if (!normReader.IsLive(oldDocId)) continue;
                    fieldNormsArray[normIdx++] = normReader.GetNorm(oldDocId, fieldName);
                }
            }
            fieldNorms[fieldName] = fieldNormsArray;
        }
        NormsWriter.Write(basePath + ".nrm", fieldNorms);

        // Write .fdt + .fdx
        StoredFieldsWriter.Write(basePath + ".fdt", basePath + ".fdx", allStoredFields.ToArray());

        // Write .vec if any
        if (allVectors.Count > 0 && allVectors.Any(v => v.Length > 0))
        {
            var memVectors = new ReadOnlyMemory<float>[allVectors.Count];
            for (int i = 0; i < allVectors.Count; i++)
                memVectors[i] = allVectors[i];
            VectorWriter.Write(basePath + ".vec", memVectors);
        }

        // Write .num if any numeric fields
        if (allNumericFields.Count > 0)
            WriteNumericIndex(basePath + ".num", allNumericFields);

        // Write exact field lengths (.fln) so BM25 scoring on the merged segment
        // matches the unmerged segments precisely (no quantisation loss).
        if (allFieldLengths.Count > 0)
            FieldLengthWriter.Write(basePath + ".fln", allFieldLengths, totalDocs);

        // Write column-stride numeric DocValues (.dvn) — used by sort, collapse, aggregations.
        if (allNumericDocValues.Count > 0)
            NumericDocValuesWriter.Write(basePath + ".dvn", allNumericDocValues, totalDocs);

        // Write column-stride sorted DocValues (.dvs) — used by string sort and collapse.
        if (allSortedDocValues.Count > 0)
            SortedDocValuesWriter.Write(basePath + ".dvs", allSortedDocValues, totalDocs);

        // Write BKD tree (.bkd) for numeric range queries — derived from .dvn data.
        if (allNumericDocValues.Count > 0)
        {
            var bkdData = new Dictionary<string, List<(double Value, int DocId)>>(StringComparer.Ordinal);
            foreach (var (field, arr) in allNumericDocValues)
            {
                var points = new List<(double Value, int DocId)>(totalDocs);
                for (int d = 0; d < totalDocs; d++)
                    points.Add((arr[d], d));
                bkdData[field] = points;
            }
            if (bkdData.Count > 0)
                BKDWriter.Write(basePath + ".bkd", bkdData);
        }

        // Write term vectors (.tvd/.tvx) when at least one source segment had them.
        if (allTermVectorDocs is not null && allTermVectorDocs.Count > 0)
            TermVectorsWriter.Write(basePath + ".tvd", basePath + ".tvx", allTermVectorDocs);

        // Write parent bitset (.pbs) for block-join queries to keep working post-merge.
        if (mergedParentBitSet is not null)
            mergedParentBitSet.WriteTo(basePath + ".pbs");

        // Write .seg
        var mergedInfo = new SegmentInfo
        {
            SegmentId = newSegId,
            DocCount = totalDocs,
            LiveDocCount = totalDocs,
            CommitGeneration = 0,
            FieldNames = fieldNames.ToList(),
            IndexSortFields = segments[0].IndexSortFields
        };
        mergedInfo.WriteTo(basePath + ".seg");

        return mergedInfo;
    }

    private static void RemapPostings(
        string posPath, string dicPath,
        Dictionary<int, int> docIdMap,
        SortedDictionary<string, List<int>> allPostings,
        Dictionary<string, Dictionary<int, int>> allFreqs,
        Dictionary<string, Dictionary<int, int[]>> allPositions)
    {
        // Use TermDictionaryReader to enumerate terms (handles v1 and v2 .dic format)
        using var dicReader = TermDictionaryReader.Open(dicPath);
        var allTerms = dicReader.EnumerateAllTerms();

        // Open .pos file with mmap
        using var posInput = new Store.IndexInput(posPath);
        byte postingsVersion = CodecConstants.ReadHeaderVersion(
            posInput, CodecConstants.PostingsVersion, "postings (.pos)");

        foreach (var (qualifiedTerm, postingsOffset) in allTerms)
        {
            posInput.Seek(postingsOffset);

            int count;
            int[]? oldIds;
            int[]? freqs;
            bool hasPositions;
            bool hasPayloads;

            if (postingsVersion >= 3)
            {
                // V3 block-packed format: per-term header then blocks
                count = posInput.ReadInt32();
                long skipOffset = posInput.ReadInt64();
                bool hasFreqsFlag = posInput.ReadBoolean();
                hasPositions = posInput.ReadBoolean();
                hasPayloads = posInput.ReadBoolean();

                long docStartOffset = posInput.Position;

                // Decode doc IDs + freqs via BlockPostingsEnum
                var blockEnum = BlockPostingsEnum.Create(posInput, docStartOffset, skipOffset, count);
                oldIds = ArrayPool<int>.Shared.Rent(count);
                freqs = ArrayPool<int>.Shared.Rent(count);

                int idx = 0;
                while (blockEnum.NextDoc() != BlockPostingsEnum.NoMoreDocs)
                {
                    oldIds[idx] = blockEnum.DocId;
                    freqs[idx] = hasFreqsFlag ? blockEnum.Freq : 1;
                    idx++;
                }

                // Seek past skip data to position section
                if (hasPositions)
                {
                    posInput.Seek(skipOffset);
                    int skipCount = posInput.ReadInt32();
                    posInput.Seek(posInput.Position + (long)skipCount * 12);
                }
            }
            else
            {
                // V1/V2 VarInt format
                count = posInput.ReadInt32();
                int postingSkipCount = posInput.ReadInt32();
                if (postingSkipCount > 0)
                    posInput.Seek(posInput.Position + postingSkipCount * 8L);

                oldIds = ArrayPool<int>.Shared.Rent(count);
                freqs = ArrayPool<int>.Shared.Rent(count);

                int prev = 0;
                for (int j = 0; j < count; j++)
                {
                    prev += posInput.ReadVarInt();
                    oldIds[j] = prev;
                }

                bool hasFreqsFlag = posInput.ReadBoolean();
                if (hasFreqsFlag)
                {
                    for (int j = 0; j < count; j++)
                        freqs[j] = posInput.ReadVarInt();
                }
                else
                {
                    Array.Fill(freqs, 1, 0, count);
                }

                hasPositions = posInput.ReadBoolean();
                hasPayloads = postingsVersion >= 2 && posInput.ReadBoolean();
            }

            try
            {
                int[][]? positions = null;
                if (hasPositions)
                {
                    positions = new int[count][];
                    for (int j = 0; j < count; j++)
                    {
                        int posCount = posInput.ReadVarInt();
                        positions[j] = new int[posCount];
                        int prevPos = 0;
                        for (int k = 0; k < posCount; k++)
                        {
                            prevPos += posInput.ReadVarInt();
                            positions[j][k] = prevPos;
                            if (hasPayloads)
                            {
                                int payloadLen = posInput.ReadVarInt();
                                if (payloadLen > 0)
                                    posInput.Seek(posInput.Position + payloadLen);
                            }
                        }
                    }
                }

                if (!allPostings.TryGetValue(qualifiedTerm, out var mergedList))
                {
                    mergedList = new List<int>();
                    allPostings[qualifiedTerm] = mergedList;
                }

                if (!allFreqs.TryGetValue(qualifiedTerm, out var mergedFreqs))
                {
                    mergedFreqs = new Dictionary<int, int>();
                    allFreqs[qualifiedTerm] = mergedFreqs;
                }

                if (!allPositions.TryGetValue(qualifiedTerm, out var mergedPositions))
                {
                    mergedPositions = new Dictionary<int, int[]>();
                    allPositions[qualifiedTerm] = mergedPositions;
                }

                for (int j = 0; j < count; j++)
                {
                    if (docIdMap.TryGetValue(oldIds[j], out int newId))
                    {
                        mergedList.Add(newId);
                        mergedFreqs[newId] = freqs[j];
                        if (positions is not null)
                            mergedPositions[newId] = positions[j];
                    }
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(oldIds);
                ArrayPool<int>.Shared.Return(freqs);
            }
        }

        foreach (var (_, list) in allPostings)
            list.Sort();
    }

    private void CleanupSegmentFiles(SegmentInfo seg)
    {
        // Delete every file belonging to this segment (any extension), not a hardcoded list.
        // This is immune to future codec additions and prevents orphan-file disk leaks.
        var pattern = $"{seg.SegmentId}.*";
        foreach (var filePath in Directory.GetFiles(_directory.DirectoryPath, pattern))
        {
            try { File.Delete(filePath); }
            catch { /* best-effort cleanup */ }
        }
    }

    private static int GetSizeTier(int docCount)
    {
        if (docCount <= 0) return 0;
        return (int)Math.Log10(Math.Max(1, docCount));
    }

    private static void WriteNumericIndex(string filePath, Dictionary<string, Dictionary<int, double>> numericIndex)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        writer.Write(numericIndex.Count);
        foreach (var (fieldName, docValues) in numericIndex)
        {
            writer.Write(fieldName);
            writer.Write(docValues.Count);
            foreach (var (docId, value) in docValues)
            {
                writer.Write(docId);
                writer.Write(value);
            }
        }
    }
}
