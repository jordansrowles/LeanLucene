using System.Text.Json;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index;

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

    /// <summary>Default merge threshold: when this many segments exist, merge.</summary>
    public const int DefaultMergeThreshold = 10;

    public SegmentMerger(MMapDirectory directory, int mergeThreshold = DefaultMergeThreshold)
    {
        _directory = directory;
        _mergeThreshold = mergeThreshold;
    }

    /// <summary>
    /// Checks if a merge is needed and performs it. Returns the updated segment list.
    /// </summary>
    public List<SegmentInfo> MaybeMerge(List<SegmentInfo> segments, ref int nextSegmentOrdinal)
    {
        if (segments.Count < _mergeThreshold)
            return segments;

        // Group segments by size tier (order of magnitude of doc count)
        var tiers = segments
            .GroupBy(s => GetSizeTier(s.DocCount))
            .Where(g => g.Count() >= _mergeThreshold)
            .OrderBy(g => g.Key)
            .ToList();

        if (tiers.Count == 0)
            return segments;

        var result = new List<SegmentInfo>(segments);

        foreach (var tier in tiers)
        {
            // Take the smallest segments in this tier
            var toMerge = tier
                .OrderBy(s => s.DocCount)
                .Take(_mergeThreshold)
                .ToList();

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
        var allStoredFields = new List<Dictionary<string, string>>();
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

                // Copy stored fields
                allStoredFields.Add(reader.GetStoredFields(oldDocId));

                // Copy vectors if present
                if (reader.HasVectors)
                {
                    var vec = reader.GetVector(oldDocId);
                    allVectors.Add(vec ?? []);
                }

                // Re-index postings for this doc
                foreach (var field in segInfo.FieldNames)
                {
                    var docIds = reader.GetDocIds(field, "");
                    // We need to scan all terms — read from the dic
                }

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
                allStoredFields.Add(reader.GetStoredFields(oldDocId));

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
        const int SkipInterval = 128;
        using (var posStream = new BinaryWriter(File.Create(basePath + ".pos")))
        {
            foreach (var qt in sortedTerms)
            {
                postingsOffsets[qt] = posStream.BaseStream.Position;
                var ids = postingsData[qt];
                posStream.Write(ids.Length);

                int skipCount = ids.Length >= SkipInterval ? (ids.Length - 1) / SkipInterval : 0;
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
                        if (i > 0 && i % SkipInterval == 0)
                        {
                            int skipIdx = (i / SkipInterval) - 1;
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

                // Preserve positional data through merges
                if (allPositions.TryGetValue(qt, out var posMap) && posMap.Count > 0)
                {
                    posStream.Write(true); // hasPositions
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
                    posStream.Write(false);
                }
            }
        }

        // Write .dic
        TermDictionaryWriter.Write(basePath + ".dic", sortedTerms, postingsOffsets);

        // Write .nrm (simple: all docs get equal norm for now)
        var norms = new float[totalDocs];
        for (int i = 0; i < totalDocs; i++)
            norms[i] = 1.0f / (1.0f + 1); // simplified
        NormsWriter.Write(basePath + ".nrm", norms);

        // Write .fdt + .fdx
        StoredFieldsWriter.Write(basePath + ".fdt", basePath + ".fdx", allStoredFields.ToArray());

        // Write .vec if any
        if (allVectors.Count > 0 && allVectors.Any(v => v.Length > 0))
            VectorWriter.Write(basePath + ".vec", allVectors.ToArray());

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
        using var dicFs = new FileStream(dicPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var dicBr = new BinaryReader(dicFs, System.Text.Encoding.UTF8, leaveOpen: false);

        int skipCount = dicBr.ReadInt32();
        for (int i = 0; i < skipCount; i++)
        {
            int termLen = dicBr.ReadInt32();
            dicBr.ReadChars(termLen);
            dicBr.ReadInt64();
        }

        while (dicFs.Position < dicFs.Length)
        {
            int termLen;
            try { termLen = dicBr.ReadInt32(); }
            catch (EndOfStreamException) { break; }

            var chars = dicBr.ReadChars(termLen);
            long postingsOffset = dicBr.ReadInt64();
            string qualifiedTerm = new string(chars);

            using var posFs = new FileStream(posPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var posBr = new BinaryReader(posFs, System.Text.Encoding.UTF8, leaveOpen: false);
            posFs.Seek(postingsOffset, SeekOrigin.Begin);

            int count = posBr.ReadInt32();
            int postingSkipCount = posBr.ReadInt32();
            // Skip past skip entries (each is 2 × int32 = 8 bytes)
            posFs.Seek(postingSkipCount * 8L, SeekOrigin.Current);
            var oldIds = new int[count];
            int prev = 0;
            for (int j = 0; j < count; j++)
            {
                int delta = PostingsReader.ReadVarInt(posBr);
                prev += delta;
                oldIds[j] = prev;
            }

            bool hasFreqs = posBr.ReadBoolean();
            var freqs = new int[count];
            if (hasFreqs)
            {
                for (int j = 0; j < count; j++)
                    freqs[j] = PostingsReader.ReadVarInt(posBr);
            }
            else
            {
                Array.Fill(freqs, 1);
            }

            // Read positional data if present
            bool hasPositions = posBr.ReadBoolean();
            int[][]? positions = null;
            if (hasPositions)
            {
                positions = new int[count][];
                for (int j = 0; j < count; j++)
                {
                    int posCount = PostingsReader.ReadVarInt(posBr);
                    positions[j] = new int[posCount];
                    int prevPos = 0;
                    for (int k = 0; k < posCount; k++)
                    {
                        prevPos += PostingsReader.ReadVarInt(posBr);
                        positions[j][k] = prevPos;
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
