using System.Text.Json;
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index;

/// <summary>
/// Accepts documents, analyses text fields, buffers in memory,
/// and flushes immutable segments to disk.
/// </summary>
public sealed class IndexWriter : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly IndexWriterConfig _config;
    private readonly IAnalyser _defaultAnalyser;

    // Buffered in-memory inverted index: field → term → list of doc IDs
    private Dictionary<string, Dictionary<string, List<int>>> _invertedIndex = new();
    // Buffered stored fields per document (local doc ID → field data)
    private List<Dictionary<string, string>> _storedFields = [];
    // Buffered numeric fields per document
    private List<Dictionary<string, double>> _numericFields = [];
    // Per-field numeric values for range indexing: field → docId → value
    private Dictionary<string, Dictionary<int, double>> _numericIndex = new();
    private Dictionary<int, (string FieldName, float[] Vector)> _bufferedVectors = new();
    private Dictionary<string, Dictionary<string, Dictionary<int, int>>> _termFreqs = new();
    // Term positions: field → term → docId → list of positions
    private Dictionary<string, Dictionary<string, Dictionary<int, List<int>>>> _termPositions = new();
    private readonly HashSet<string> _termPool = new(StringComparer.Ordinal);
    // Per-doc token count for O(1) norm computation
    private int[] _docTokenCounts = new int[64];

    private int _bufferedDocCount;
    private long _estimatedRamBytes;
    private int _nextSegmentOrdinal;
    private int _commitGeneration;
    private readonly List<SegmentInfo> _committedSegments = [];
    // Pending deletions: field → term → set of matching terms to delete
    private readonly List<(string field, string term)> _pendingDeletes = [];
    private readonly Lock _writeLock = new();
    private readonly SemaphoreSlim? _backpressureSemaphore;
    private int _semaphoreSlotsHeld; // Track how many semaphore slots are currently held
    private bool _disposed;

    public IndexWriter(MMapDirectory directory, IndexWriterConfig config)
    {
        _directory = directory;
        _config = config;
        _defaultAnalyser = config.DefaultAnalyser;

        // Initialize backpressure semaphore if MaxQueuedDocs > 0
        if (config.MaxQueuedDocs > 0)
            _backpressureSemaphore = new SemaphoreSlim(config.MaxQueuedDocs, config.MaxQueuedDocs);

        // Load existing commit state if present
        LoadLatestCommit();
    }

    public void AddDocument(LeanDocument doc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Apply backpressure if enabled: wait for a semaphore slot before acquiring the write lock.
        // This prevents unbounded memory growth when documents are queued faster than they can be flushed.
        _backpressureSemaphore?.Wait();

        try
        {
            lock (_writeLock)
            {
                // Track that we've acquired a slot
                if (_backpressureSemaphore is not null)
                    _semaphoreSlotsHeld++;

                AddDocumentCore(doc);
            }
        }
        catch
        {
            // If AddDocumentCore fails, release the semaphore slot immediately
            if (_backpressureSemaphore is not null)
            {
                _backpressureSemaphore.Release();
                lock (_writeLock)
                {
                    _semaphoreSlotsHeld--;
                }
            }
            throw;
        }
    }

    /// <summary>Atomically deletes documents matching the selector and adds the replacement.</summary>
    public void UpdateDocument(string field, string term, LeanDocument replacement)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_writeLock)
        {
            _pendingDeletes.Add((field, term));
            AddDocumentCore(replacement);
        }
    }

    private void AddDocumentCore(LeanDocument doc)
    {
        int localDocId = _bufferedDocCount;
        var storedDoc = new Dictionary<string, string>();
        var numericDoc = new Dictionary<string, double>();

        foreach (var field in doc.Fields)
        {
            switch (field)
            {
                case TextField tf:
                    IndexTextField(tf.Name, tf.Value, localDocId);
                    if (tf.IsStored)
                        storedDoc[tf.Name] = tf.Value;
                    break;
                case StringField sf:
                    IndexStringField(sf.Name, sf.Value, localDocId);
                    if (sf.IsStored)
                        storedDoc[sf.Name] = sf.Value;
                    break;
                case NumericField nf:
                    IndexNumericField(nf.Name, nf.Value, localDocId);
                    if (nf.IsStored)
                        storedDoc[nf.Name] = nf.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case VectorField vf:
                    _bufferedVectors[localDocId] = (vf.Name, vf.Value.ToArray());
                    break;
            }
        }

        _storedFields.Add(storedDoc);
        _numericFields.Add(numericDoc);
        _bufferedDocCount++;

        // Estimate RAM usage (rough: 100 bytes per token entry + stored field sizes)
        _estimatedRamBytes += 200;
        foreach (var kvp in storedDoc)
            _estimatedRamBytes += kvp.Key.Length * 2 + kvp.Value.Length * 2;

        // Check flush thresholds
        if (ShouldFlush())
            FlushSegment();
    }

    public void DeleteDocuments(Search.TermQuery query)
    {
        lock (_writeLock)
        {
            _pendingDeletes.Add((query.Field, query.Term));
        }
    }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_writeLock)
        {
            CommitCore();
        }
    }

    private void CommitCore()
    {
        // Snapshot the segments that exist BEFORE flushing. Deletions from
        // UpdateDocument must only target these older segments — not the
        // replacement segment that will be flushed next.
        var preFlushSegmentCount = _committedSegments.Count;

        // Apply pending deletions to pre-existing segments only (for UpdateDocument)
        if (preFlushSegmentCount > 0 && _pendingDeletes.Count > 0)
            ApplyPendingDeletions(_committedSegments.GetRange(0, preFlushSegmentCount));

        // Flush any remaining buffered documents
        if (_bufferedDocCount > 0)
            FlushSegment();

        // Apply any remaining deletions to ALL segments (including the just-flushed one).
        // This handles the case where DeleteDocuments + Commit are called without UpdateDocument.
        if (_pendingDeletes.Count > 0)
            ApplyPendingDeletions(_committedSegments);

        // Maybe merge segments
        var merger = new SegmentMerger(_directory);
        var merged = merger.MaybeMerge(_committedSegments, ref _nextSegmentOrdinal);
        if (!ReferenceEquals(merged, _committedSegments))
        {
            _committedSegments.Clear();
            _committedSegments.AddRange(merged);
        }

        // Write segments_N commit file (atomic: write to temp, then rename)
        _commitGeneration++;
        var commitFile = Path.Combine(_directory.DirectoryPath, $"segments_{_commitGeneration}");
        var tempFile = commitFile + ".tmp";
        var segmentIds = _committedSegments.Select(s => s.SegmentId).ToList();
        var commitData = JsonSerializer.Serialize(new CommitData { Segments = segmentIds, Generation = _commitGeneration });
        File.WriteAllText(tempFile, commitData);
        File.Move(tempFile, commitFile, overwrite: true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _backpressureSemaphore?.Dispose();
    }

    /// <summary>
    /// Returns all committed and flushed segments for near-real-time search.
    /// Flushes any buffered documents first but does not write a commit file.
    /// </summary>
    public IReadOnlyList<SegmentInfo> GetNrtSegments()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_bufferedDocCount > 0)
            FlushSegment();
        return _committedSegments.AsReadOnly();
    }

    private void IndexTextField(string fieldName, string value, int docId)
    {
        var analyser = _config.FieldAnalysers.GetValueOrDefault(fieldName, _defaultAnalyser);
        var tokens = analyser.Analyse(value.AsSpan());

        // Track token count for O(1) norm computation
        if (docId >= _docTokenCounts.Length)
            Array.Resize(ref _docTokenCounts, Math.Max(_docTokenCounts.Length * 2, docId + 1));
        _docTokenCounts[docId] += tokens.Count;

        if (!_invertedIndex.ContainsKey(fieldName))
            _invertedIndex[fieldName] = new Dictionary<string, List<int>>();
        if (!_termFreqs.ContainsKey(fieldName))
            _termFreqs[fieldName] = new Dictionary<string, Dictionary<int, int>>();
        if (!_termPositions.ContainsKey(fieldName))
            _termPositions[fieldName] = new Dictionary<string, Dictionary<int, List<int>>>();

        var fieldIndex = _invertedIndex[fieldName];
        var fieldFreqs = _termFreqs[fieldName];
        var fieldPos = _termPositions[fieldName];

        for (int pos = 0; pos < tokens.Count; pos++)
        {
            var term = CanonicaliseTerm(tokens[pos].Text);
            if (!fieldIndex.TryGetValue(term, out var docList))
            {
                docList = [];
                fieldIndex[term] = docList;
            }
            if (docList.Count == 0 || docList[^1] != docId)
                docList.Add(docId);

            if (!fieldFreqs.TryGetValue(term, out var freqMap))
            {
                freqMap = new Dictionary<int, int>();
                fieldFreqs[term] = freqMap;
            }
            freqMap[docId] = freqMap.GetValueOrDefault(docId) + 1;

            if (!fieldPos.TryGetValue(term, out var posMap))
            {
                posMap = new Dictionary<int, List<int>>();
                fieldPos[term] = posMap;
            }
            if (!posMap.TryGetValue(docId, out var posList))
            {
                posList = [];
                posMap[docId] = posList;
            }
            posList.Add(pos);
        }
    }

    private void IndexStringField(string fieldName, string value, int docId)
    {
        // String fields are indexed as a single exact term (not analysed)
        if (!_invertedIndex.ContainsKey(fieldName))
            _invertedIndex[fieldName] = new Dictionary<string, List<int>>();

        var fieldIndex = _invertedIndex[fieldName];
        var term = CanonicaliseTerm(value);
        if (!fieldIndex.TryGetValue(term, out var docList))
        {
            docList = [];
            fieldIndex[term] = docList;
        }
        docList.Add(docId);
    }

    private bool ShouldFlush()
    {
        if (_bufferedDocCount >= _config.MaxBufferedDocs)
            return true;
        if (_estimatedRamBytes >= (long)(_config.RamBufferSizeMB * 1024 * 1024))
            return true;
        return false;
    }

    private void FlushSegment()
    {
        if (_bufferedDocCount == 0) return;

        // Track how many documents we're flushing so we can release the corresponding semaphore slots.
        int docCountToFlush = _bufferedDocCount;

        var segId = $"seg_{_nextSegmentOrdinal++}";
        var basePath = Path.Combine(_directory.DirectoryPath, segId);

        // Collect all field names
        var fieldNames = _invertedIndex.Keys.ToList();

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
        // Combine all fields into one .dic and .pos for simplicity
        var allTerms = new SortedDictionary<string, long>(StringComparer.Ordinal);
        var postingsData = new Dictionary<string, int[]>();

        foreach (var (fieldName, terms) in _invertedIndex)
        {
            foreach (var (term, docIds) in terms)
            {
                // Prefix terms with field name to disambiguate
                var qualifiedTerm = $"{fieldName}\x00{term}";
                postingsData[qualifiedTerm] = docIds.ToArray();
            }
        }

        // Write postings first to determine offsets
        var sortedQualifiedTerms = postingsData.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var postingsOffsets = new Dictionary<string, long>();

        // Write all postings to a single .pos file
        const int SkipInterval = 128;
        using (var posStream = new BinaryWriter(File.Create(basePath + ".pos")))
        {
            foreach (var qt in sortedQualifiedTerms)
            {
                postingsOffsets[qt] = posStream.BaseStream.Position;
                var ids = postingsData[qt];
                posStream.Write(ids.Length);

                // Write skip pointer entries (every SkipInterval docs)
                int skipCount = ids.Length >= SkipInterval ? (ids.Length - 1) / SkipInterval : 0;
                posStream.Write(skipCount);

                if (skipCount > 0)
                {
                    // Reserve space for skip entries, fill in after writing deltas
                    long skipTablePos = posStream.BaseStream.Position;
                    for (int s = 0; s < skipCount; s++)
                    {
                        posStream.Write(0); // placeholder docId
                        posStream.Write(0); // placeholder byte offset
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
                            posStream.Write(ids[i - 1]); // docId at boundary
                            posStream.Write((int)(currentPos - deltaStartPos)); // byte offset
                            posStream.BaseStream.Seek(currentPos, SeekOrigin.Begin);
                        }
                        PostingsWriter.WriteVarInt(posStream, ids[i] - prev);
                        prev = ids[i];
                    }
                }
                else
                {
                    // Small posting list — write deltas directly
                    int prev = 0;
                    foreach (var id in ids)
                    {
                        PostingsWriter.WriteVarInt(posStream, id - prev);
                        prev = id;
                    }
                }

                var parts = qt.Split('\x00', 2);
                Dictionary<int, int>? freqMap = null;
                bool hasFreqs = parts.Length == 2 && _termFreqs.TryGetValue(parts[0], out var fieldFreqs)
                    && fieldFreqs.TryGetValue(parts[1], out freqMap);

                posStream.Write(hasFreqs);
                if (hasFreqs)
                {
                    foreach (var id in ids)
                        PostingsWriter.WriteVarInt(posStream, freqMap!.GetValueOrDefault(id, 1));
                }

                Dictionary<int, List<int>>? posMap = null;
                bool hasPositions = parts.Length == 2 && _termPositions.TryGetValue(parts[0], out var fieldPositions)
                    && fieldPositions.TryGetValue(parts[1], out posMap);

                posStream.Write(hasPositions);
                if (hasPositions)
                {
                    foreach (var id in ids)
                    {
                        var positions = posMap!.GetValueOrDefault(id, []);
                        PostingsWriter.WriteVarInt(posStream, positions.Count);
                        int prevPos = 0;
                        foreach (var p in positions)
                        {
                            PostingsWriter.WriteVarInt(posStream, p - prevPos);
                            prevPos = p;
                        }
                    }
                }
            }
        }

        // Write term dictionary (.dic)
        TermDictionaryWriter.Write(basePath + ".dic", sortedQualifiedTerms, postingsOffsets);

        // Write norms (.nrm) — use pre-tracked token counts (O(1) per doc)
        var norms = new float[_bufferedDocCount];
        for (int i = 0; i < _bufferedDocCount; i++)
        {
            int totalTokens = i < _docTokenCounts.Length ? _docTokenCounts[i] : 0;
            norms[i] = 1.0f / (1.0f + Math.Max(1, totalTokens));
        }
        NormsWriter.Write(basePath + ".nrm", norms);

        // Write stored fields (.fdt + .fdx)
        StoredFieldsWriter.Write(basePath + ".fdt", basePath + ".fdx",
            _storedFields.ToArray());

        // Write numeric field index (.num)
        WriteNumericIndex(basePath + ".num");

        if (_bufferedVectors.Count > 0)
        {
            var vectors = new float[_bufferedDocCount][];
            for (int i = 0; i < _bufferedDocCount; i++)
                vectors[i] = _bufferedVectors.TryGetValue(i, out var entry) ? entry.Vector : [];

            VectorWriter.Write(basePath + ".vec", vectors);
        }

        _committedSegments.Add(segInfo);
        ResetBuffer();

        // Release semaphore slots AFTER the flush is complete and buffers are cleared.
        // Only release the slots we actually held (which equals docCountToFlush).
        if (_backpressureSemaphore is not null && docCountToFlush > 0)
        {
            // Ensure we don't release more slots than we're currently holding
            int toRelease = Math.Min(docCountToFlush, _semaphoreSlotsHeld);
            if (toRelease > 0)
            {
                _backpressureSemaphore.Release(toRelease);
                _semaphoreSlotsHeld -= toRelease;
            }
        }
    }

    private void ApplyPendingDeletions(List<SegmentInfo> segments)
    {
        if (_pendingDeletes.Count == 0) return;

        foreach (var seg in segments)
        {
            var basePath = Path.Combine(_directory.DirectoryPath, seg.SegmentId);
            var dicPath = basePath + ".dic";
            var posPath = basePath + ".pos";

            if (!File.Exists(dicPath) || !File.Exists(posPath))
                continue;

            using var dicReader = TermDictionaryReader.Open(dicPath);
            var liveDocs = new LiveDocs(seg.DocCount);

            // Load existing deletions if present
            var delPath = basePath + ".del";
            if (File.Exists(delPath))
                liveDocs = LiveDocs.Deserialise(delPath, seg.DocCount);

            bool changed = false;
            foreach (var (field, term) in _pendingDeletes)
            {
                // Look up the qualified term in the dictionary
                var qualifiedTerm = $"{field}\x00{term}";
                if (!dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
                    continue;

                // Read doc IDs from postings at that offset
                var docIds = ReadPostingsAtOffset(posPath, offset);
                foreach (var docId in docIds)
                {
                    if (liveDocs.IsLive(docId))
                    {
                        liveDocs.Delete(docId);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                LiveDocs.Serialise(delPath, liveDocs);
                seg.LiveDocCount = liveDocs.LiveCount;
            }
        }

        _pendingDeletes.Clear();
    }

    private static int[] ReadPostingsAtOffset(string posPath, long offset)
    {
        using var reader = new BinaryReader(File.OpenRead(posPath));
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        int count = reader.ReadInt32();
        int skipCount = reader.ReadInt32();
        // Skip past skip entries (each is 2 × int32 = 8 bytes)
        reader.BaseStream.Seek(skipCount * 8L, SeekOrigin.Current);
        var ids = new int[count];
        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            int delta = PostingsReader.ReadVarInt(reader);
            if (delta < 0)
                throw new InvalidDataException("Postings data is corrupt: negative delta encountered.");
            try
            {
                prev = checked(prev + delta);
            }
            catch (OverflowException ex)
            {
                throw new InvalidDataException("Postings data is corrupt: doc ID delta overflow.", ex);
            }
            ids[i] = prev;
        }
        return ids;
    }

    private void ResetBuffer()
    {
        _invertedIndex = new Dictionary<string, Dictionary<string, List<int>>>();
        _storedFields = [];
        _numericFields = [];
        _termFreqs = new Dictionary<string, Dictionary<string, Dictionary<int, int>>>();
        _termPositions = new Dictionary<string, Dictionary<string, Dictionary<int, List<int>>>>();
        _termPool.Clear();
        _numericIndex = new Dictionary<string, Dictionary<int, double>>();
        _bufferedVectors = new();
        _bufferedDocCount = 0;
        _estimatedRamBytes = 0;
        Array.Clear(_docTokenCounts);
    }

    private string CanonicaliseTerm(string term)
    {
        // Check if the term already exists in the pool and return the canonical reference.
        // This ensures all dictionaries share the same string instance, reducing memory pressure.
        if (_termPool.TryGetValue(term, out var canonical))
            return canonical;

        // Add the term to the pool. Since HashSet.Add returns false if the item already exists,
        // we need to retrieve the actual reference from the set after adding.
        _termPool.Add(term);
        
        // Now retrieve the canonical reference from the set.
        // For HashSet<string>, the set itself maintains the canonical reference.
        // We return the input term since we just added it, but the next lookup will return this instance.
        return term;
    }

    private void IndexNumericField(string fieldName, double value, int docId)
    {
        if (!_numericIndex.TryGetValue(fieldName, out var fieldMap))
        {
            fieldMap = new Dictionary<int, double>();
            _numericIndex[fieldName] = fieldMap;
        }
        fieldMap[docId] = value;
    }

    private void WriteNumericIndex(string filePath)
    {
        if (_numericIndex.Count == 0) return;

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: false);

        writer.Write(_numericIndex.Count); // field count
        foreach (var (fieldName, docValues) in _numericIndex)
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

    private void LoadLatestCommit()
    {
        // Find highest segments_N file
        var files = Directory.GetFiles(_directory.DirectoryPath, "segments_*");
        if (files.Length == 0) return;

        var latest = files
            .Select(f => (File: f, Gen: int.TryParse(Path.GetFileName(f).Replace("segments_", ""), out int g) ? g : -1))
            .Where(x => x.Gen >= 0)
            .OrderByDescending(x => x.Gen)
            .FirstOrDefault();

        if (latest.File == null) return;

        var json = File.ReadAllText(latest.File);
        var commitData = JsonSerializer.Deserialize<CommitData>(json);
        if (commitData == null) return;

        _commitGeneration = commitData.Generation;
        _nextSegmentOrdinal = commitData.Segments.Count;

        foreach (var segId in commitData.Segments)
        {
            var segPath = Path.Combine(_directory.DirectoryPath, segId + ".seg");
            if (File.Exists(segPath))
            {
                _committedSegments.Add(SegmentInfo.ReadFrom(segPath));
            }
        }
    }

    private sealed class CommitData
    {
        public List<string> Segments { get; set; } = [];
        public int Generation { get; set; }
    }
}
