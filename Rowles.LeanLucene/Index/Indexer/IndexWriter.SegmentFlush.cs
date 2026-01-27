using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.DocValues;
using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    private void FlushSegment()
    {
        if (_bufferedDocCount == 0) return;

        // Track how many documents we're flushing so we can release the corresponding semaphore slots.
        int docCountToFlush = _bufferedDocCount;

        var segId = $"seg_{_nextSegmentOrdinal++}";
        var basePath = Path.Combine(_directory.DirectoryPath, segId);

        // Collect all field names
        var fieldNames = _fieldNames.ToList();

        // Write segment metadata (.seg)
        var segInfo = new SegmentInfo
        {
            SegmentId = segId,
            DocCount = _bufferedDocCount,
            LiveDocCount = _bufferedDocCount,
            CommitGeneration = _commitGeneration,
            FieldNames = fieldNames
        };
        segInfo.WriteTo(basePath + ".seg");

        // Write term dictionary and postings per field
        // Sort qualified terms for the dictionary
        _sortedTermsBuffer.Clear();
        _sortedTermsBuffer.AddRange(_postings.Keys);
        _sortedTermsBuffer.Sort(StringComparer.Ordinal);
        var postingsOffsets = new Dictionary<string, long>();

        // Write all postings to a single .pos file using pooled IndexOutput
        int skipInterval = _config.PostingsSkipInterval;
        using (var posOutput = new IndexOutput(basePath + ".pos"))
        {
            // Write header at the start of the file
            CodecConstants.WriteHeader(posOutput, CodecConstants.PostingsVersion);
            
            foreach (var qt in _sortedTermsBuffer)
            {
                var acc = _postings[qt];
                var ids = acc.DocIds;

                postingsOffsets[qt] = posOutput.Position;
                posOutput.WriteInt32(ids.Length);

                // Write skip pointer entries (every skipInterval docs)
                int skipCount = ids.Length >= skipInterval ? (ids.Length - 1) / skipInterval : 0;
                posOutput.WriteInt32(skipCount);

                if (skipCount > 0)
                {
                    // Reserve space for skip entries, fill in after writing deltas
                    long skipTablePos = posOutput.Position;
                    for (int s = 0; s < skipCount; s++)
                    {
                        posOutput.WriteInt32(0); // placeholder docId
                        posOutput.WriteInt32(0); // placeholder byte offset
                    }
                    long deltaStartPos = posOutput.Position;

                    int prev = 0;
                    for (int i = 0; i < ids.Length; i++)
                    {
                        if (i > 0 && i % skipInterval == 0)
                        {
                            int skipIdx = (i / skipInterval) - 1;
                            long currentPos = posOutput.Position;
                            posOutput.Seek(skipTablePos + skipIdx * 8);
                            posOutput.WriteInt32(ids[i - 1]); // docId at boundary
                            posOutput.WriteInt32((int)(currentPos - deltaStartPos)); // byte offset
                            posOutput.Seek(currentPos);
                        }
                        posOutput.WriteVarInt(ids[i] - prev);
                        prev = ids[i];
                    }
                }
                else
                {
                    // Small posting list — write deltas directly
                    int prev = 0;
                    foreach (var id in ids)
                    {
                        posOutput.WriteVarInt(id - prev);
                        prev = id;
                    }
                }

                bool hasFreqs = acc.HasFreqs;
                posOutput.WriteBoolean(hasFreqs);
                if (hasFreqs)
                {
                    for (int i = 0; i < ids.Length; i++)
                        posOutput.WriteVarInt(acc.GetFreq(i));
                }

                bool hasPositions = acc.HasPositions;
                posOutput.WriteBoolean(hasPositions);
                if (hasPositions)
                {
                    for (int i = 0; i < ids.Length; i++)
                    {
                        var positions = acc.GetPositions(i);
                        posOutput.WriteVarInt(positions.Length);
                        int prevPos = 0;
                        foreach (var p in positions)
                        {
                            posOutput.WriteVarInt(p - prevPos);
                            prevPos = p;
                        }
                    }
                }
            }
        }

        // Write term dictionary (.dic)
        TermDictionaryWriter.Write(basePath + ".dic", _sortedTermsBuffer, postingsOffsets);

        // Write per-field norms (.nrm) — use pre-tracked per-field token counts (O(1) per doc)
        var fieldNorms = new Dictionary<string, float[]>(_docTokenCounts.Count, StringComparer.Ordinal);
        foreach (var (fieldName, counts) in _docTokenCounts)
        {
            var norms = new float[_bufferedDocCount];
            for (int i = 0; i < _bufferedDocCount; i++)
            {
                int tokenCount = i < counts.Length ? counts[i] : 0;
                norms[i] = 1.0f / (1.0f + Math.Max(1, tokenCount));
            }
            fieldNorms[fieldName] = norms;
        }
        NormsWriter.Write(basePath + ".nrm", fieldNorms);

        // Write exact field lengths (.fln) for precise BM25 scoring
        var fieldLengths = new Dictionary<string, int[]>(_docTokenCounts.Count, StringComparer.Ordinal);
        foreach (var (fieldName, counts) in _docTokenCounts)
        {
            var lengths = new int[_bufferedDocCount];
            for (int i = 0; i < _bufferedDocCount; i++)
                lengths[i] = i < counts.Length ? counts[i] : 0;
            fieldLengths[fieldName] = lengths;
        }
        FieldLengthWriter.Write(basePath + ".fln", fieldLengths);

        // Write stored fields (.fdt + .fdx) from flat buffer
        StoredFieldsWriter.Write(basePath + ".fdt", basePath + ".fdx",
            _sfDocStarts, _sfFieldIds, _sfValues, _sfFieldIdToName,
            _config.StoredFieldBlockSize, _config.StoredFieldCompressionLevel);

        // Write numeric field index (.num)
        WriteNumericIndex(basePath + ".num");

        if (_bufferedVectors.Count > 0)
        {
            var vectors = new ReadOnlyMemory<float>[_bufferedDocCount];
            for (int i = 0; i < _bufferedDocCount; i++)
                vectors[i] = _bufferedVectors.TryGetValue(i, out var entry) ? entry.Vector : ReadOnlyMemory<float>.Empty;

            VectorWriter.Write(basePath + ".vec", vectors);
        }

        // Write DocValues column-stride files
        if (_numericDocValues.Count > 0)
        {
            var dvn = new Dictionary<string, double[]>(_numericDocValues.Count, StringComparer.Ordinal);
            foreach (var (field, list) in _numericDocValues)
            {
                var arr = new double[_bufferedDocCount];
                for (int i = 0; i < Math.Min(list.Count, _bufferedDocCount); i++)
                    arr[i] = list[i];
                dvn[field] = arr;
            }
            NumericDocValuesWriter.Write(basePath + ".dvn", dvn, _bufferedDocCount);
        }

        if (_sortedDocValues.Count > 0)
        {
            var dvs = new Dictionary<string, string?[]>(_sortedDocValues.Count, StringComparer.Ordinal);
            foreach (var (field, list) in _sortedDocValues)
            {
                var arr = new string?[_bufferedDocCount];
                for (int i = 0; i < Math.Min(list.Count, _bufferedDocCount); i++)
                    arr[i] = list[i];
                dvs[field] = arr;
            }
            SortedDocValuesWriter.Write(basePath + ".dvs", dvs, _bufferedDocCount);
        }

        // Write BKD tree for numeric fields (.bkd)
        if (_numericDocValues.Count > 0)
        {
            var bkdData = new Dictionary<string, List<(double Value, int DocId)>>(_numericDocValues.Count, StringComparer.Ordinal);
            foreach (var (field, list) in _numericDocValues)
            {
                var points = new List<(double Value, int DocId)>();
                for (int i = 0; i < Math.Min(list.Count, _bufferedDocCount); i++)
                    points.Add((list[i], i));
                if (points.Count > 0)
                    bkdData[field] = points;
            }
            if (bkdData.Count > 0)
                BKDWriter.Write(basePath + ".bkd", bkdData, _config.BKDMaxLeafSize);
        }

        // Write term vectors (.tvd + .tvx) when enabled
        if (_config.StoreTermVectors)
        {
            var tvDocs = new Dictionary<string, List<TermVectorEntry>>[_bufferedDocCount];
            for (int d = 0; d < _bufferedDocCount; d++)
                tvDocs[d] = new Dictionary<string, List<TermVectorEntry>>(StringComparer.Ordinal);

            foreach (var (qt, acc) in _postings)
            {
                if (!acc.HasPositions) continue;
                int sep = qt.IndexOf('\x00');
                if (sep < 0) continue;
                string fld = qt[..sep];
                string trm = qt[(sep + 1)..];

                var ids = acc.DocIds;
                for (int i = 0; i < ids.Length; i++)
                {
                    int docId = ids[i];
                    if (docId >= _bufferedDocCount) continue;
                    if (!tvDocs[docId].TryGetValue(fld, out var terms))
                    {
                        terms = [];
                        tvDocs[docId][fld] = terms;
                    }
                    int freq = acc.GetFreq(i);
                    var posSpan = acc.GetPositions(i);
                    var positions = posSpan.IsEmpty ? [] : posSpan.ToArray();
                    terms.Add(new TermVectorEntry(trm, freq, positions));
                }
            }
            TermVectorsWriter.Write(basePath + ".tvd", basePath + ".tvx", tvDocs);
        }

        // Write compound file (.cfs) when enabled
        if (_config.UseCompoundFile)
        {
            string[] extensions = [".seg", ".dic", ".pos", ".fdt", ".fdx", ".nrm", ".num", ".dvn", ".dvs"];
            var existingExtensions = extensions.Where(ext => File.Exists(basePath + ext)).ToArray();
            if (existingExtensions.Length > 0)
            {
                CompoundFileWriter.Write(basePath + ".cfs", basePath, existingExtensions);
                foreach (var ext in existingExtensions)
                    try { File.Delete(basePath + ext); } catch { /* best-effort */ }
            }
        }

        _committedSegments.Add(segInfo);
        ResetBuffer();

        // Release semaphore slots AFTER the flush is complete and buffers are cleared.
        if (_backpressureSemaphore is not null && docCountToFlush > 0)
        {
            int toRelease = Math.Min(docCountToFlush, _semaphoreSlotsHeld);
            if (toRelease > 0)
            {
                _backpressureSemaphore.Release(toRelease);
                _semaphoreSlotsHeld -= toRelease;
            }
        }
    }
}
