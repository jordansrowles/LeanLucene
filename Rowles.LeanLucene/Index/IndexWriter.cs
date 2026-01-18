using System.Runtime.CompilerServices;
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

    // Unified posting accumulator keyed by qualified term ("field\0term")
    private Dictionary<string, PostingAccumulator> _postings = new(StringComparer.Ordinal);
    // Flat stored field buffer: parallel arrays indexed by entry position
    private List<int> _sfFieldIds = new(4096);
    private List<string> _sfValues = new(4096);
    private List<int> _sfDocStarts = new(256);
    private readonly Dictionary<string, int> _sfFieldNameToId = new(StringComparer.Ordinal);
    private readonly List<string> _sfFieldIdToName = new();
    // Buffered numeric fields per document
    private List<Dictionary<string, double>> _numericFields = [];
    // Per-field numeric values for range indexing: field → docId → value
    private Dictionary<string, Dictionary<int, double>> _numericIndex = new();
    private Dictionary<int, (string FieldName, ReadOnlyMemory<float> Vector)> _bufferedVectors = new();
    private readonly HashSet<string> _termPool = new(StringComparer.Ordinal);
    // Per-field per-doc token counts for O(1) per-field norm computation
    private Dictionary<string, int[]> _docTokenCounts = new(StringComparer.Ordinal);
    // Track field names seen in this flush
    private readonly HashSet<string> _fieldNames = new(StringComparer.Ordinal);
    // Cache qualified term strings to avoid repeated string.Concat — keyed by the qualified term itself
    private Dictionary<string, string> _qualifiedTermPool = new(StringComparer.Ordinal);
    // Cache field name prefixes ("fieldName\0") to avoid repeated prefix construction
    private readonly Dictionary<string, string> _fieldPrefixCache = new(StringComparer.Ordinal);
    // DocValues accumulators: field → per-doc values
    private Dictionary<string, List<double>> _numericDocValues = new(StringComparer.Ordinal);
    private Dictionary<string, List<string?>> _sortedDocValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IAnalyser> _analyserCache = new(StringComparer.Ordinal);
    private readonly List<string> _sortedTermsBuffer = new(capacity: 10000);

    private int _bufferedDocCount;
    private long _estimatedRamBytes;
    private int _nextSegmentOrdinal;
    private int _commitGeneration;
    private readonly List<SegmentInfo> _committedSegments = [];
    // Pending deletions: field → term → set of matching terms to delete
    private readonly List<(string field, string term)> _pendingDeletes = [];
    private readonly Lock _writeLock = new();
    private readonly SemaphoreSlim? _backpressureSemaphore;
    private int _semaphoreSlotsHeld;
    private readonly List<IndexSnapshot> _heldSnapshots = [];
    private bool _disposed;
    private readonly FileStream _writeLockFile;
    // Background merge
    private Task? _mergeTask;
    private readonly CancellationTokenSource _mergeCts = new();
    private readonly Lock _mergeLock = new();

    public IndexWriter(MMapDirectory directory, IndexWriterConfig config)
    {
        _directory = directory;
        _config = config;
        _defaultAnalyser = config.DefaultAnalyser;

        // Acquire exclusive write lock for this directory
        var lockPath = Path.Combine(directory.DirectoryPath, "write.lock");
        try
        {
            _writeLockFile = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }
        catch (IOException)
        {
            throw new WriteLockException(directory.DirectoryPath);
        }

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

    /// <summary>
    /// Indexes a batch of documents with a single lock acquisition.
    /// Faster than calling <see cref="AddDocument"/> in a loop because lock
    /// and backpressure overhead is paid once for the entire batch.
    /// </summary>
    public void AddDocuments(IReadOnlyList<LeanDocument> documents)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (documents.Count == 0) return;

        if (_backpressureSemaphore is not null)
        {
            for (int i = 0; i < documents.Count; i++)
                _backpressureSemaphore.Wait();
        }

        try
        {
            lock (_writeLock)
            {
                if (_backpressureSemaphore is not null)
                    _semaphoreSlotsHeld += documents.Count;

                for (int i = 0; i < documents.Count; i++)
                    AddDocumentCore(documents[i]);
            }
        }
        catch
        {
            if (_backpressureSemaphore is not null)
            {
                _backpressureSemaphore.Release(documents.Count);
                lock (_writeLock)
                {
                    _semaphoreSlotsHeld -= documents.Count;
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

    /// <summary>
    /// Indexes a batch of documents concurrently using per-thread buffers (DWPT-style).
    /// Analysis and posting accumulation run in parallel; the merge into the writer's
    /// buffer is serialised. Call <see cref="Commit"/> afterwards to persist.
    /// </summary>
    public void AddDocumentsConcurrent(IReadOnlyList<LeanDocument> documents)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (documents.Count == 0) return;

        // Partition docs into per-thread chunks and process in parallel
        // Each thread gets its own analyser instances to avoid thread-safety issues
        var perThreadResults = new System.Collections.Concurrent.ConcurrentBag<DocumentsWriterPerThread>();

        Parallel.ForEach(
            System.Collections.Concurrent.Partitioner.Create(0, documents.Count),
            () => CreateThreadLocalDocumentWriter(),
            (range, _, dwpt) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    dwpt.AddDocument(documents[i], i);
                return dwpt;
            },
            dwpt => perThreadResults.Add(dwpt));

        // Merge all per-thread results into the writer under the lock
        lock (_writeLock)
        {
            foreach (var dwpt in perThreadResults)
                MergeDwpt(dwpt);
        }
    }

    /// <summary>
    /// Creates a DocumentsWriterPerThread with fresh analyser instances for thread-safe parallel indexing.
    /// Each analyser instance maintains its own internal buffers (_tokensBuf, _internCache, etc.)
    /// to prevent race conditions when multiple threads analyse documents concurrently.
    /// </summary>
    private DocumentsWriterPerThread CreateThreadLocalDocumentWriter()
    {
        // Create a fresh default analyser instance for this thread
        IAnalyser threadLocalDefaultAnalyser = _defaultAnalyser switch
        {
            StandardAnalyser => new StandardAnalyser(),
            StemmedAnalyser => new StemmedAnalyser(),
            _ => _defaultAnalyser // Fall back to shared instance for unknown types
        };

        // Clone per-field analysers for this thread
        var threadLocalFieldAnalysers = new Dictionary<string, IAnalyser>(_config.FieldAnalysers.Count);
        foreach (var kvp in _config.FieldAnalysers)
        {
            threadLocalFieldAnalysers[kvp.Key] = kvp.Value switch
            {
                StandardAnalyser => new StandardAnalyser(),
                StemmedAnalyser => new StemmedAnalyser(),
                _ => kvp.Value // Fall back to shared instance for unknown types
            };
        }

        return new DocumentsWriterPerThread(threadLocalDefaultAnalyser, threadLocalFieldAnalysers);
    }

    private void MergeDwpt(DocumentsWriterPerThread dwpt)
    {
        int docBase = _bufferedDocCount;
        // Merge postings with doc ID remapping
        foreach (var (qt, srcAcc) in dwpt.Postings)
        {
            if (!_postings.TryGetValue(qt, out var dstAcc))
            {
                dstAcc = new PostingAccumulator();
                _postings[qt] = dstAcc;
            }
            var srcIds = srcAcc.DocIds;
            for (int i = 0; i < srcIds.Length; i++)
            {
                int remappedDocId = srcIds[i] + docBase;
                if (srcAcc.HasPositions)
                {
                    var positions = srcAcc.GetPositions(i);
                    foreach (var p in positions)
                        dstAcc.Add(remappedDocId, p);
                }
                else
                {
                    dstAcc.AddDocOnly(remappedDocId);
                }
            }
        }

        // Merge stored fields from DWPT dictionary format into flat buffer
        foreach (var storedDoc in dwpt.StoredFields)
        {
            _sfDocStarts.Add(_sfFieldIds.Count);
            foreach (var (name, values) in storedDoc)
            {
                foreach (var value in values)
                    AppendStoredField(name, value);
            }
        }

        // Merge per-field token counts
        // Pre-allocate to MaxBufferedDocs to avoid resize overhead
        foreach (var (fieldName, counts) in dwpt.DocTokenCounts)
        {
            if (!_docTokenCounts.TryGetValue(fieldName, out var dstCounts))
            {
                dstCounts = new int[_config.MaxBufferedDocs];
                _docTokenCounts[fieldName] = dstCounts;
            }
            
            int newTotal = docBase + dwpt.DocCount;
            if (newTotal > dstCounts.Length)
            {
                // Rare case: need to grow beyond pre-allocated size
                Array.Resize(ref dstCounts, Math.Max(dstCounts.Length * 2, newTotal));
                _docTokenCounts[fieldName] = dstCounts;
            }
            
            for (int i = 0; i < dwpt.DocCount && i < counts.Length; i++)
                dstCounts[docBase + i] = counts[i];
        }

        // Merge field names
        foreach (var fn in dwpt.FieldNames)
            _fieldNames.Add(fn);

        // Merge numeric index
        foreach (var (field, map) in dwpt.NumericIndex)
        {
            if (!_numericIndex.TryGetValue(field, out var dstMap))
            {
                dstMap = new Dictionary<int, double>();
                _numericIndex[field] = dstMap;
            }
            foreach (var (docId, val) in map)
                dstMap[docId + docBase] = val;
        }

        // Merge numeric doc values
        foreach (var (field, list) in dwpt.NumericDocValues)
        {
            if (!_numericDocValues.TryGetValue(field, out var dstList))
            {
                dstList = new List<double>();
                _numericDocValues[field] = dstList;
            }
            while (dstList.Count < docBase) dstList.Add(0);
            dstList.AddRange(list);
        }

        _bufferedDocCount += dwpt.DocCount;
        _estimatedRamBytes += dwpt.DocCount * 200;

        if (ShouldFlush())
            FlushSegment();
    }

    private void AddDocumentCore(LeanDocument doc)
    {
        int localDocId = _bufferedDocCount;
        _sfDocStarts.Add(_sfFieldIds.Count);
        Dictionary<string, double>? numericDoc = null;
        int storedEntryStart = _sfFieldIds.Count;

        foreach (var field in doc.Fields)
        {
            switch (field)
            {
                case TextField tf:
                    IndexTextField(tf.Name, tf.Value, localDocId);
                    if (tf.IsStored)
                    {
                        AppendStoredField(tf.Name, tf.Value);
                    }
                    break;
                case StringField sf:
                    IndexStringField(sf.Name, sf.Value, localDocId);
                    if (sf.IsStored)
                    {
                        AppendStoredField(sf.Name, sf.Value);
                    }
                    break;
                case NumericField nf:
                    IndexNumericField(nf.Name, nf.Value, localDocId);
                    numericDoc ??= new Dictionary<string, double>();
                    if (nf.IsStored)
                    {
                        AppendStoredField(nf.Name, nf.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    break;
                case VectorField vf:
                    _bufferedVectors[localDocId] = (vf.Name, vf.Value);
                    break;
            }
        }

        if (numericDoc is not null)
            _numericFields.Add(numericDoc);
        _bufferedDocCount++;

        // Estimate RAM usage
        _estimatedRamBytes += 200;
        for (int i = storedEntryStart; i < _sfFieldIds.Count; i++)
            _estimatedRamBytes += _sfValues[i].Length * 2 + 16;

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

        // Schedule merge in background (non-blocking)
        ScheduleBackgroundMerge();

        // Write segments_N commit file (atomic: write to temp, then rename)
        _commitGeneration++;
        var commitFile = Path.Combine(_directory.DirectoryPath, $"segments_{_commitGeneration}");
        var tempFile = commitFile + ".tmp";
        var segmentIds = new List<string>(_committedSegments.Count);
        foreach (var seg in _committedSegments)
            segmentIds.Add(seg.SegmentId);
        var commitData = JsonSerializer.Serialize(new CommitData { Segments = segmentIds, Generation = _commitGeneration });
        File.WriteAllText(tempFile, commitData);
        File.Move(tempFile, commitFile, overwrite: true);

        // Apply deletion policy to prune old commit files
        _config.DeletionPolicy.OnCommit(_directory.DirectoryPath, _commitGeneration);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Cancel and await background merge
        _mergeCts.Cancel();
        try { _mergeTask?.Wait(TimeSpan.FromSeconds(10)); } catch { /* best-effort */ }
        _mergeCts.Dispose();

        _backpressureSemaphore?.Dispose();

        // Release the directory write lock
        _writeLockFile.Dispose();
        var lockPath = Path.Combine(_directory.DirectoryPath, "write.lock");
        try { File.Delete(lockPath); } catch { /* best-effort */ }
    }

    private void ScheduleBackgroundMerge()
    {
        lock (_mergeLock)
        {
            if (_mergeTask is not null && !_mergeTask.IsCompleted)
                return; // merge already in flight

            var ct = _mergeCts.Token;
            _mergeTask = Task.Run(() =>
            {
                if (ct.IsCancellationRequested) return;

                // Spin briefly to avoid contending with an active flush/commit
                SpinWait sw = default;
                while (!_writeLock.TryEnter())
                {
                    if (ct.IsCancellationRequested) return;
                    sw.SpinOnce();
                }
                try
                {
                    var merger = new SegmentMerger(_directory);
                    var merged = merger.MaybeMerge(_committedSegments, ref _nextSegmentOrdinal);
                    if (!ReferenceEquals(merged, _committedSegments))
                    {
                        _committedSegments.Clear();
                        _committedSegments.AddRange(merged);

                        // Write updated commit file so the merged segments are discoverable
                        _commitGeneration++;
                        var commitFile = Path.Combine(_directory.DirectoryPath, $"segments_{_commitGeneration}");
                        var tempFile = commitFile + ".tmp";
                        var segmentIds = new List<string>(_committedSegments.Count);
                        foreach (var seg in _committedSegments)
                            segmentIds.Add(seg.SegmentId);
                        var commitData = JsonSerializer.Serialize(new CommitData { Segments = segmentIds, Generation = _commitGeneration });
                        File.WriteAllText(tempFile, commitData);
                        File.Move(tempFile, commitFile, overwrite: true);

                        _config.DeletionPolicy.OnCommit(_directory.DirectoryPath, _commitGeneration);
                    }
                }
                finally
                {
                    _writeLock.Exit();
                }
            }, ct);
        }
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

    /// <summary>
    /// Creates a point-in-time snapshot of the currently committed segments.
    /// While held, segments referenced by the snapshot will not be deleted during merge.
    /// Call <see cref="ReleaseSnapshot"/> when the snapshot is no longer needed.
    /// </summary>
    public IndexSnapshot CreateSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_writeLock)
        {
            // Flush any pending docs so the snapshot is up to date
            if (_bufferedDocCount > 0)
                FlushSegment();

            var snapshot = new IndexSnapshot(
                _commitGeneration,
                _committedSegments.Select(s => new SegmentInfo
                {
                    SegmentId = s.SegmentId,
                    DocCount = s.DocCount,
                    LiveDocCount = s.LiveDocCount,
                    CommitGeneration = s.CommitGeneration,
                    FieldNames = [.. s.FieldNames]
                }).ToList().AsReadOnly());

            _heldSnapshots.Add(snapshot);
            return snapshot;
        }
    }

    /// <summary>
    /// Releases a previously held snapshot, allowing its segments to be merged away.
    /// </summary>
    public void ReleaseSnapshot(IndexSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_writeLock)
        {
            _heldSnapshots.Remove(snapshot);
        }
    }

    /// <summary>Returns the set of segment IDs protected by active snapshots.</summary>
    internal HashSet<string> GetSnapshotProtectedSegments()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var snap in _heldSnapshots)
        {
            foreach (var seg in snap.Segments)
                ids.Add(seg.SegmentId);
        }
        return ids;
    }

    private void IndexTextField(string fieldName, string value, int docId)
    {
        if (!_analyserCache.TryGetValue(fieldName, out var analyser))
        {
            analyser = _config.FieldAnalysers.GetValueOrDefault(fieldName, _defaultAnalyser);
            _analyserCache[fieldName] = analyser;
        }
        var tokens = analyser.Analyse(value.AsSpan());

        // Track per-field token count for O(1) per-field norm computation
        // Pre-allocate to MaxBufferedDocs to avoid resize overhead during indexing
        if (!_docTokenCounts.TryGetValue(fieldName, out var counts))
        {
            counts = new int[_config.MaxBufferedDocs];
            _docTokenCounts[fieldName] = counts;
        }
        else if (docId >= counts.Length)
        {
            // Rare case: exceeded MaxBufferedDocs, grow the array
            Array.Resize(ref counts, Math.Max(counts.Length * 2, docId + 1));
            _docTokenCounts[fieldName] = counts;
        }
        counts[docId] += tokens.Count;

        _fieldNames.Add(fieldName);

        for (int pos = 0; pos < tokens.Count; pos++)
        {
            var term = CanonicaliseTerm(tokens[pos].Text);

            var pooledTerm = GetOrCreateQualifiedTerm(fieldName, term);

            if (!_postings.TryGetValue(pooledTerm, out var acc))
            {
                acc = new PostingAccumulator();
                _postings[pooledTerm] = acc;
            }
            acc.Add(docId, pos);
        }
    }

    private void IndexStringField(string fieldName, string value, int docId)
    {
        _fieldNames.Add(fieldName);
        var term = CanonicaliseTerm(value);

        var pooledTerm = GetOrCreateQualifiedTerm(fieldName, term);

        if (!_postings.TryGetValue(pooledTerm, out var acc))
        {
            acc = new PostingAccumulator();
            _postings[pooledTerm] = acc;
        }
        acc.AddDocOnly(docId);
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
        const int SkipInterval = 128;
        using (var posOutput = new IndexOutput(basePath + ".pos"))
        {
            foreach (var qt in _sortedTermsBuffer)
            {
                var acc = _postings[qt];
                var ids = acc.DocIds;

                postingsOffsets[qt] = posOutput.Position;
                posOutput.WriteInt32(ids.Length);

                // Write skip pointer entries (every SkipInterval docs)
                int skipCount = ids.Length >= SkipInterval ? (ids.Length - 1) / SkipInterval : 0;
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
                        if (i > 0 && i % SkipInterval == 0)
                        {
                            int skipIdx = (i / SkipInterval) - 1;
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
                BKDWriter.Write(basePath + ".bkd", bkdData);
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

        // Pre-compute qualified terms to avoid repeated string allocation
        var qualifiedTerms = new List<string>(_pendingDeletes.Count);
        foreach (var (field, term) in _pendingDeletes)
        {
            qualifiedTerms.Add($"{field}\x00{term}");
        }

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
            // Use memory-mapped IndexInput instead of BinaryReader for zero-copy reads
            using var posInput = new IndexInput(posPath);
            foreach (var qualifiedTerm in qualifiedTerms)
            {
                if (!dicReader.TryGetPostingsOffset(qualifiedTerm, out long offset))
                    continue;

                ReadPostingsAtOffsetInto(posInput, offset, liveDocs, ref changed);
            }

            if (changed)
            {
                LiveDocs.Serialise(delPath, liveDocs);
                seg.LiveDocCount = liveDocs.LiveCount;
            }
        }

        _pendingDeletes.Clear();
    }

    /// <summary>
    /// Reads doc IDs from postings at the given offset using a memory-mapped IndexInput,
    /// and marks matching live docs as deleted. Zero allocation for the common case.
    /// </summary>
    private static void ReadPostingsAtOffsetInto(IndexInput input, long offset, LiveDocs liveDocs, ref bool changed)
    {
        input.Seek(offset);
        int count = input.ReadInt32();
        int skipCount = input.ReadInt32();
        if (skipCount > 0)
            input.Seek(input.Position + skipCount * 8L);

        int prev = 0;
        for (int i = 0; i < count; i++)
        {
            int delta = input.ReadVarInt();
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
            if (liveDocs.IsLive(prev))
            {
                liveDocs.Delete(prev);
                changed = true;
            }
        }
    }

    private void ResetBuffer()
    {
        _postings.Clear();
        _sfFieldIds.Clear();
        _sfValues.Clear();
        _sfDocStarts.Clear();
        _numericFields.Clear();
        _termPool.Clear();
        _fieldNames.Clear();
        _qualifiedTermPool.Clear();
        _numericIndex.Clear();
        _bufferedVectors.Clear();
        _numericDocValues.Clear();
        _sortedDocValues.Clear();
        _sortedTermsBuffer.Clear();
        _bufferedDocCount = 0;
        _estimatedRamBytes = 0;
        _docTokenCounts.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendStoredField(string fieldName, string value)
    {
        if (!_sfFieldNameToId.TryGetValue(fieldName, out int fid))
        {
            fid = _sfFieldIdToName.Count;
            _sfFieldNameToId[fieldName] = fid;
            _sfFieldIdToName.Add(fieldName);
        }
        _sfFieldIds.Add(fid);
        _sfValues.Add(value);
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

    /// <summary>
    /// Returns a pooled qualified term string ("field\0term"). Avoids allocation when the
    /// qualified term already exists in the pool by checking the pool before constructing
    /// the string. Uses a cached field prefix to reduce per-token concat cost.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetOrCreateQualifiedTerm(string fieldName, string term)
    {
        if (!_fieldPrefixCache.TryGetValue(fieldName, out var prefix))
        {
            prefix = string.Concat(fieldName, "\x00");
            _fieldPrefixCache[fieldName] = prefix;
        }

        var qualifiedTerm = string.Create(prefix.Length + term.Length, (prefix, term),
            static (span, state) =>
            {
                state.prefix.AsSpan().CopyTo(span);
                state.term.AsSpan().CopyTo(span[state.prefix.Length..]);
            });

        if (_qualifiedTermPool.TryGetValue(qualifiedTerm, out var pooled))
            return pooled;

        _qualifiedTermPool[qualifiedTerm] = qualifiedTerm;
        return qualifiedTerm;
    }

    private void IndexNumericField(string fieldName, double value, int docId)
    {
        if (!_numericIndex.TryGetValue(fieldName, out var fieldMap))
        {
            fieldMap = new Dictionary<int, double>();
            _numericIndex[fieldName] = fieldMap;
        }
        fieldMap[docId] = value;

        // Also accumulate for NumericDocValues column-stride storage
        if (!_numericDocValues.TryGetValue(fieldName, out var dvList))
        {
            dvList = new List<double>();
            _numericDocValues[fieldName] = dvList;
        }
        // Pad with 0 for any skipped docs
        while (dvList.Count < docId)
            dvList.Add(0);
        dvList.Add(value);
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

        // Manual loop avoids LINQ allocation overhead  
        var latestFile = (string?)null;
        var latestGen = -1;
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (int.TryParse(fileName.Replace("segments_", ""), out int gen) && gen > latestGen)
            {
                latestGen = gen;
                latestFile = file;
            }
        }

        if (latestFile == null) return;

        var json = File.ReadAllText(latestFile);
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
