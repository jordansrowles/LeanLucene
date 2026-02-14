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
        var fieldNames = new HashSet<string>();

        int newDocId = 0;

        foreach (var segInfo in segments)
        {
            using var reader = new SegmentReader(_directory, segInfo);

            foreach (var field in segInfo.FieldNames)
                fieldNames.Add(field);

            for (int oldDocId = 0; oldDocId < segInfo.DocCount; oldDocId++)
            {
                if (!reader.IsLive(oldDocId))
                    continue;

                // Copy stored fields - convert from IReadOnlyDictionary to Dictionary
                var fields = reader.GetStoredFields(oldDocId);
                var mutableFields = new Dictionary<string, List<string>>();
                foreach (var kvp in fields)
                    mutableFields[kvp.Key] = kvp.Value.ToList();
                allStoredFields.Add(mutableFields);

                // Copy vectors if present
                if (reader.HasVectors)
                {
                    var vec = reader.GetVector(oldDocId);
                    allVectors.Add(vec ?? []);
                }

                // Re-index postings for this doc

                newDocId++;
            }
        }

        // Simplified merge: re-read each segment's postings via term dictionary
        newDocId = 0;
        allPostings.Clear();
        allFreqs.Clear();
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

        allStoredFields.Clear();
        allNumericFields.Clear();
        allVectors.Clear();
        int remapDocId = 0;
        foreach (var segInfo in segments)
        {
            using var reader = new SegmentReader(_directory, segInfo);
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

        // Write .pos
        int skipInterval = _skipInterval;
        using (var posStream = new BinaryWriter(File.Create(basePath + ".pos")))
        {
            // Write header at the start of the file
            CodecConstants.WriteHeader(posStream, CodecConstants.PostingsVersion);
            
            foreach (var qt in sortedTerms)
            {
                postingsOffsets[qt] = posStream.BaseStream.Position;
                var ids = postingsData[qt];
                posStream.Write(ids.Length);

                int skipCount = ids.Length >= skipInterval ? (ids.Length - 1) / skipInterval : 0;
                posStream.Write(skipCount);

                if (skipCount > 0)
                {
                    long skipTablePos = posStream.BaseStream.Position;
                    for (int s = 0; s < skipCount; s++)
                    {
                        posStream.Write(0);
                        posStream.Write(0);
                    }
                    long deltaStartPos = posStream.BaseStream.Position;

                    int prev = 0;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        if (i > 0 && i % skipInterval == 0)
                        {
                            int skipIdx = (i / skipInterval) - 1;
                            long currentPos = posStream.BaseStream.Position;
                            posStream.BaseStream.Seek(skipTablePos + skipIdx * 8, SeekOrigin.Begin);
                            posStream.Write(ids[i - 1]);
                            posStream.Write((int)(currentPos - deltaStartPos));
                            posStream.BaseStream.Seek(currentPos, SeekOrigin.Begin);
                        }
                        PostingsWriter.WriteVarInt(posStream, ids[i] - prev);
                        prev = ids[i];
                    }
                }
                else
                {
                    int prev = 0;
                    foreach (var id in ids)
                    {
                        PostingsWriter.WriteVarInt(posStream, id - prev);
                        prev = id;
                    }
                }

                if (allFreqs.TryGetValue(qt, out var freqMap))
                {
                    posStream.Write(true);
                    foreach (var id in ids)
                        PostingsWriter.WriteVarInt(posStream, freqMap.GetValueOrDefault(id, 1));
                }
                else
                {
                    posStream.Write(false);
                }

                // Preserve positional data through merges (v2 format: hasPositions + hasPayloads)
                if (allPositions.TryGetValue(qt, out var posMap) && posMap.Count > 0)
                {
                    posStream.Write(true); // hasPositions
                    posStream.Write(false); // hasPayloads (not preserved through merge yet)
                    foreach (var id in ids)
                    {
                        var positions = posMap.GetValueOrDefault(id, []);
                        PostingsWriter.WriteVarInt(posStream, positions.Length);
                        int prevPos = 0;
                        foreach (var p in positions)
                        {
                            PostingsWriter.WriteVarInt(posStream, p - prevPos);
                            prevPos = p;
                        }
                    }
                }
                else
                {
                    posStream.Write(false); // hasPositions
                    posStream.Write(false); // hasPayloads
                }
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

        // Write .seg
        var mergedInfo = new SegmentInfo
        {
            SegmentId = newSegId,
            DocCount = totalDocs,
            LiveDocCount = totalDocs,
            CommitGeneration = 0,
            FieldNames = fieldNames.ToList()
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

            int count = posInput.ReadInt32();
            int postingSkipCount = posInput.ReadInt32();
            if (postingSkipCount > 0)
                posInput.Seek(posInput.Position + postingSkipCount * 8L);

            var oldIds = ArrayPool<int>.Shared.Rent(count);
            var freqs = ArrayPool<int>.Shared.Rent(count);
            try
            {
                int prev = 0;
                for (int j = 0; j < count; j++)
                {
                    prev += posInput.ReadVarInt();
                    oldIds[j] = prev;
                }

                bool hasFreqs = posInput.ReadBoolean();
                if (hasFreqs)
                {
                    for (int j = 0; j < count; j++)
                        freqs[j] = posInput.ReadVarInt();
                }
                else
                {
                    Array.Fill(freqs, 1, 0, count);
                }

                bool hasPositions = posInput.ReadBoolean();

                bool hasPayloads = false;
                if (postingsVersion >= 2)
                    hasPayloads = posInput.ReadBoolean();

                int[][]? positions = null;
                byte[]?[][]? payloads = null;
                if (hasPositions)
                {
                    positions = new int[count][];
                    if (hasPayloads) payloads = new byte[]?[count][];

                    for (int j = 0; j < count; j++)
                    {
                        int posCount = posInput.ReadVarInt();
                        positions[j] = new int[posCount];
                        if (hasPayloads) payloads![j] = new byte[]?[posCount];
                        int prevPos = 0;
                        for (int k = 0; k < posCount; k++)
                        {
                            prevPos += posInput.ReadVarInt();
                            positions[j][k] = prevPos;
                            if (hasPayloads)
                            {
                                int payloadLen = posInput.ReadVarInt();
                                if (payloadLen > 0)
                                    payloads![j][k] = posInput.ReadBytes(payloadLen);
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
        var basePath = Path.Combine(_directory.DirectoryPath, seg.SegmentId);
        var extensions = new[] { ".seg", ".dic", ".pos", ".nrm", ".fdt", ".fdx", ".del", ".vec", ".num" };
        foreach (var ext in extensions)
        {
            var filePath = basePath + ext;
            if (File.Exists(filePath))
                File.Delete(filePath);
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
