using System.Runtime.CompilerServices;
using System.Text.Json;
using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Codecs;
using Rowles.LeanLucene.Codecs.StoredFields;
using Rowles.LeanLucene.Document;
using Rowles.LeanLucene.Document.Fields;
using Rowles.LeanLucene.Store;

namespace Rowles.LeanLucene.Index.Indexer;

/// <summary>
/// Accepts documents, analyses text fields, buffers in memory,
/// and flushes immutable segments to disk.
/// </summary>
public sealed partial class IndexWriter : IDisposable
{
    private readonly MMapDirectory _directory;
    private readonly IndexWriterConfig _config;
    private readonly IAnalyser _defaultAnalyser;

    // Unified posting accumulator keyed by qualified term ("field\0term")
    private Dictionary<string, PostingAccumulator> _postings = new(8192, StringComparer.Ordinal);
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
    private readonly HashSet<string> _termPool = new(4096, StringComparer.Ordinal);
    // Per-field per-doc token counts for O(1) per-field norm computation
    private Dictionary<string, int[]> _docTokenCounts = new(StringComparer.Ordinal);
    // Track field names seen in this flush
    private readonly HashSet<string> _fieldNames = new(StringComparer.Ordinal);
    // Cache qualified term strings to avoid repeated string.Concat — keyed by the qualified term itself
    private Dictionary<string, string> _qualifiedTermPool = new(8192, StringComparer.Ordinal);
    // Cache field name prefixes ("fieldName\0") to avoid repeated prefix construction
    private readonly Dictionary<string, string> _fieldPrefixCache = new(StringComparer.Ordinal);
    // DocValues accumulators: field → per-doc values
    private Dictionary<string, List<double>> _numericDocValues = new(StringComparer.Ordinal);
    private Dictionary<string, List<string?>> _sortedDocValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IAnalyser> _analyserCache = new(StringComparer.Ordinal);
    private readonly List<string> _sortedTermsBuffer = new(capacity: 10000);
    // Parent bitset for block-join indexing: tracks which buffered doc IDs are parent docs
    private HashSet<int>? _parentDocIds;

    private int _bufferedDocCount;
    private long _estimatedRamBytes;
    private long _postingsRamBytes; // incrementally tracked sum of all PostingAccumulator.EstimatedBytes
    private int _nextSegmentOrdinal;
    private int _commitGeneration;
    private readonly List<SegmentInfo> _committedSegments = [];
    // Pending deletions: field → term → set of matching terms to delete
    private readonly List<(string field, string term)> _pendingDeletes = [];
    private readonly Lock _writeLock = new();
    private readonly SemaphoreSlim? _backpressureSemaphore;
    private int _semaphoreSlotsHeld;
    private readonly List<IndexSnapshot> _heldSnapshots = [];
    private int _disposed; // 0 = alive, 1 = disposed (atomically set via Interlocked)
    private readonly FileStream _writeLockFile;
    // Background merge
    private Task? _mergeTask;
    private readonly CancellationTokenSource _mergeCts = new();
    private readonly Lock _mergeLock = new();

    /// <summary>
    /// Initialises a new <see cref="IndexWriter"/> for the given directory with the specified configuration.
    /// Acquires an exclusive write lock on the directory. Only one writer may be open per directory at a time.
    /// </summary>
    /// <param name="directory">The directory where index files will be written.</param>
    /// <param name="config">Writer configuration including analyser, flush thresholds, and deletion policy.</param>
    /// <exception cref="WriteLockException">Thrown if another <see cref="IndexWriter"/> already holds the write lock for this directory.</exception>
    public IndexWriter(MMapDirectory directory, IndexWriterConfig config)
    {
        _directory = directory;
        _config = config;

        // If using default StandardAnalyser and config has custom stop words or cache size, rebuild it
        if (config.DefaultAnalyser is StandardAnalyser &&
            (config.StopWords is not null || config.AnalyserInternCacheSize != 4096))
        {
            _defaultAnalyser = new StandardAnalyser(config.AnalyserInternCacheSize, config.StopWords);
        }
        else
        {
            _defaultAnalyser = config.DefaultAnalyser;
        }

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

    /// <summary>
    /// Indexes a single document. Validates the document against the schema if one is configured.
    /// May block if <see cref="IndexWriterConfig.MaxQueuedDocs"/> backpressure is enabled.
    /// Automatically flushes a segment when the RAM or document count threshold is reached.
    /// </summary>
    /// <param name="doc">The document to index.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has been disposed.</exception>
    /// <exception cref="SchemaValidationException">Thrown if the document violates the configured schema.</exception>
    public void AddDocument(LeanDocument doc)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        _config.Schema?.Validate(doc);

        // Apply backpressure if enabled: wait for a semaphore slot before acquiring the write lock.
        // This prevents unbounded memory growth when documents are queued faster than they can be flushed.
        // Use TryWait first — if the semaphore is exhausted, force a flush to release slots
        // (avoids deadlock when MaxBufferedDocs > MaxQueuedDocs).
        if (_backpressureSemaphore is not null && !_backpressureSemaphore.Wait(0))
        {
            lock (_writeLock)
            {
                if (_bufferedDocCount > 0)
                    FlushSegment();
            }
            _backpressureSemaphore.Wait();
        }

        try
        {
            lock (_writeLock)
            {
                // Merge backpressure: if too many unmerged segments, flush and merge now
                if (ShouldThrottleForMerge() && _bufferedDocCount > 0)
                    FlushSegment();

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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (documents.Count == 0) return;

        if (_backpressureSemaphore is not null)
        {
            for (int i = 0; i < documents.Count; i++)
            {
                if (!_backpressureSemaphore.Wait(0))
                {
                    lock (_writeLock)
                    {
                        if (_bufferedDocCount > 0)
                            FlushSegment();
                    }
                    _backpressureSemaphore.Wait();
                }
            }
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

    /// <summary>
    /// Indexes a block of child documents followed by a parent document atomically.
    /// The last document in <paramref name="block"/> is the parent; all preceding
    /// documents are children. Children are stored contiguously before their parent
    /// in the segment, enabling block-join queries.
    /// </summary>
    /// <param name="block">The documents to index as a block. The last element is the parent. Must have at least 2 documents.</param>
    /// <exception cref="ArgumentException">Thrown if the block has fewer than 2 documents.</exception>
    public void AddDocumentBlock(IReadOnlyList<LeanDocument> block)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (block.Count < 2)
            throw new ArgumentException("A document block requires at least one child and one parent document.", nameof(block));

        if (_backpressureSemaphore is not null)
        {
            for (int i = 0; i < block.Count; i++)
            {
                if (!_backpressureSemaphore.Wait(0))
                {
                    lock (_writeLock)
                    {
                        if (_bufferedDocCount > 0)
                            FlushSegment();
                    }
                    _backpressureSemaphore.Wait();
                }
            }
        }

        try
        {
            lock (_writeLock)
            {
                if (_backpressureSemaphore is not null)
                    _semaphoreSlotsHeld += block.Count;

                // Index all docs in the block contiguously.
                // Record the parent ID BEFORE its AddDocumentCore call so that
                // a mid-block flush (triggered inside AddDocumentCore) includes
                // the correct parent in the ParentBitSet.
                for (int i = 0; i < block.Count; i++)
                {
                    if (i == block.Count - 1)
                    {
                        _parentDocIds ??= new HashSet<int>();
                        _parentDocIds.Add(_bufferedDocCount);
                    }
                    AddDocumentCore(block[i]);
                }
            }
        }
        catch
        {
            if (_backpressureSemaphore is not null)
            {
                _backpressureSemaphore.Release(block.Count);
                lock (_writeLock)
                {
                    _semaphoreSlotsHeld -= block.Count;
                }
            }
            throw;
        }
    }

    /// <summary>Atomically deletes documents matching the selector and adds the replacement.</summary>
    public void UpdateDocument(string field, string term, LeanDocument replacement)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_writeLock)
        {
            _pendingDeletes.Add((field, term));
            AddDocumentCore(replacement);
        }
    }

    /// <summary>
    /// Flushes all buffered documents and pending deletions to disk, writes a new
    /// <c>segments_N</c> commit file, and applies the configured deletion policy.
    /// Schedules a background merge after the flush.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has been disposed.</exception>
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_writeLock)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            CommitCore();
            sw.Stop();
            _config.Metrics.RecordCommit(sw.Elapsed);
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

        // Flush any DWPT pool buffers before the main flush
        FlushDwptPool();

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

        // Persist index statistics alongside the commit so IndexSearcher can
        // skip the expensive full-segment scan on construction.
        WriteCommitStats(_commitGeneration);

        // Apply deletion policy to prune old commit files
        _config.DeletionPolicy.OnCommit(_directory.DirectoryPath, _commitGeneration);
    }

    /// <summary>
    /// Computes current index statistics from committed segments and writes
    /// a stats_N.json file for the given commit generation.
    /// </summary>
    private void WriteCommitStats(int generation)
    {
        int totalDocCount = 0;
        int liveDocCount = 0;
        var fieldLengthSums = new Dictionary<string, long>(StringComparer.Ordinal);
        var fieldDocCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var seg in _committedSegments)
        {
            using var reader = new SegmentReader(_directory, seg);
            totalDocCount += reader.MaxDoc;
            for (int docId = 0; docId < reader.MaxDoc; docId++)
            {
                if (!reader.IsLive(docId)) continue;
                liveDocCount++;
                foreach (var field in seg.FieldNames)
                {
                    int len = reader.GetFieldLength(docId, field);
                    fieldLengthSums[field] = fieldLengthSums.GetValueOrDefault(field) + len;
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

        var stats = new IndexStats(totalDocCount, liveDocCount, avgFieldLengths, fieldDocCounts);
        stats.WriteTo(IndexStats.GetStatsPath(_directory.DirectoryPath, generation));
    }

    /// <summary>
    /// Releases all resources held by this writer, including the directory write lock.
    /// Cancels any background merge task and waits up to 10 seconds for it to complete.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        // Cancel and await background merge
        _mergeCts.Cancel();
        try { _mergeTask?.Wait(TimeSpan.FromSeconds(10)); }
        catch (AggregateException) { /* Expected: merge task cancelled during shutdown */ }
        catch (ObjectDisposedException) { /* CTS already disposed */ }
        _mergeCts.Dispose();

        _backpressureSemaphore?.Dispose();

        // Release the directory write lock
        _writeLockFile.Dispose();
        var lockPath = Path.Combine(_directory.DirectoryPath, "write.lock");
        try { File.Delete(lockPath); } catch { /* best-effort */ }
    }

    private bool ShouldFlush()
    {
        if (_bufferedDocCount >= _config.MaxBufferedDocs)
            return true;
        long ram = ComputeEstimatedRamBytes();
        if (ram >= (long)(_config.RamBufferSizeMB * 1024 * 1024))
            return true;
        if (_config.FlushThrottleBytes > 0 && ram >= _config.FlushThrottleBytes)
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether merge backpressure should pause indexing.
    /// When <see cref="IndexWriterConfig.MergeThrottleSegments"/> is set and the
    /// number of committed segments exceeds it, this returns true.
    /// </summary>
    private bool ShouldThrottleForMerge()
    {
        return _config.MergeThrottleSegments > 0
            && _committedSegments.Count >= _config.MergeThrottleSegments;
    }

    /// <summary>
    /// Returns the estimated RAM used by all buffered data. O(1) — uses the
    /// incrementally tracked <c>_postingsRamBytes</c> instead of iterating
    /// every <see cref="PostingAccumulator"/>.
    /// </summary>
    private long ComputeEstimatedRamBytes()
    {
        return _postingsRamBytes + _estimatedRamBytes;
    }

    private void ResetBuffer()
    {
        // Return pooled arrays from all accumulators before clearing
        foreach (var acc in _postings.Values)
            acc.ReturnBuffers();
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
        _postingsRamBytes = 0;
        _docTokenCounts.Clear();
        _parentDocIds = null;
    }

    private void LoadLatestCommit()
    {
        var recovery = IndexRecovery.RecoverLatestCommit(_directory.DirectoryPath);
        if (recovery is null) return;

        _commitGeneration = recovery.Generation;
        _nextSegmentOrdinal = recovery.SegmentIds.Count;

        foreach (var segId in recovery.SegmentIds)
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
