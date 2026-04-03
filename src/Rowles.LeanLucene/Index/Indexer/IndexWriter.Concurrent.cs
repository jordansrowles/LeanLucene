using Rowles.LeanLucene.Analysis;
using Rowles.LeanLucene.Document;

namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    private DocumentsWriterPerThread[]? _dwptPool;
    private int _dwptCounter; // for round-robin assignment via Interlocked

    /// <summary>
    /// Initialises the DWPT pool for concurrent indexing.
    /// Call once before using <see cref="AddDocumentLockFree"/> or <see cref="AddDocumentsConcurrent"/>.
    /// </summary>
    /// <param name="threadCount">Number of per-thread writers to allocate (default: processor count).</param>
    public void InitialiseDwptPool(int threadCount = 0)
    {
        if (threadCount <= 0)
            threadCount = Math.Max(1, Environment.ProcessorCount);

        _dwptPool = new DocumentsWriterPerThread[threadCount];
        for (int i = 0; i < threadCount; i++)
            _dwptPool[i] = CreateThreadLocalDocumentWriter();
    }

    /// <summary>
    /// Lock-free document addition using per-thread DWPT buffers.
    /// Uses <see cref="Interlocked.Increment(ref int)"/> for round-robin DWPT selection.
    /// Each DWPT flushes independently when its RAM threshold is reached.
    /// Call <see cref="InitialiseDwptPool"/> before first use.
    /// </summary>
    public void AddDocumentLockFree(LeanDocument doc)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        _config.Schema?.Validate(doc);

        var pool = _dwptPool ?? throw new InvalidOperationException(
            "DWPT pool not initialised. Call InitialiseDwptPool() first.");

        // Register this call as in-flight BEFORE entering the hot path, then re-check
        // disposed. This two-step prevents Dispose from tearing down the semaphore
        // while we are still inside the lock on a DWPT slot.
        Interlocked.Increment(ref _inFlightAdds);
        try
        {
            // Re-check: Dispose may have set _disposed between the first check and the
            // increment above. If so, bail cleanly — the increment is undone in finally.
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(IndexWriter));

            // Round-robin DWPT selection — lock-free via Interlocked
            int slot = (int)((uint)Interlocked.Increment(ref _dwptCounter) % (uint)pool.Length);
            var dwpt = pool[slot];

            // Per-DWPT lock (not global — only contention on the same slot)
            lock (dwpt)
            {
                dwpt.AddDocument(doc);
            }

            // Check per-DWPT RAM threshold and flush if needed
            long ramThreshold = (long)(_config.RamBufferSizeMB * 1024 * 1024) / pool.Length;
            if (dwpt.EstimatedRamBytes > ramThreshold)
            {
                lock (_writeLock)
                {
                    lock (dwpt)
                    {
                        if (dwpt.DocCount > 0)
                        {
                            MergeDwpt(dwpt);
                            ResetDwpt(dwpt);
                        }
                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref _inFlightAdds);
        }
    }

    /// <summary>
    /// Indexes a batch of documents using parallel per-thread writer buffers (DWPT).
    /// Partitions the input across all available processors and merges results into the
    /// main buffer under a single lock acquisition per DWPT.
    /// </summary>
    /// <param name="documents">The documents to index concurrently.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has been disposed.</exception>
    public void AddDocumentsConcurrent(IReadOnlyList<Document.LeanDocument> documents)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (documents.Count == 0) return;

        var perThreadResults = new System.Collections.Concurrent.ConcurrentBag<DocumentsWriterPerThread>();

        Parallel.ForEach(
            System.Collections.Concurrent.Partitioner.Create(0, documents.Count),
            () => CreateThreadLocalDocumentWriter(),
            (range, _, dwpt) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    dwpt.AddDocument(documents[i]);
                return dwpt;
            },
            dwpt => perThreadResults.Add(dwpt));

        lock (_writeLock)
        {
            foreach (var dwpt in perThreadResults)
                MergeDwpt(dwpt);
        }
    }

    /// <summary>
    /// Flushes all DWPT pool buffers into the main buffer and then flushes to disk.
    /// Called during <see cref="Commit"/> to ensure all buffered data is persisted.
    /// </summary>
    private void FlushDwptPool()
    {
        var pool = _dwptPool;
        if (pool == null) return;

        foreach (var dwpt in pool)
        {
            lock (dwpt)
            {
                if (dwpt.DocCount > 0)
                {
                    MergeDwpt(dwpt);
                    ResetDwpt(dwpt);
                }
            }
        }
    }

    /// <summary>
    /// Resets a DWPT to empty state after its contents have been merged.
    /// </summary>
    private static void ResetDwpt(DocumentsWriterPerThread dwpt)
    {
        dwpt.Postings.Clear();
        dwpt.StoredFields.Clear();
        dwpt.NumericIndex.Clear();
        dwpt.NumericDocValues.Clear();
        dwpt.SortedDocValues.Clear();
        dwpt.FieldNames.Clear();
        dwpt.DocTokenCounts.Clear();
        dwpt.DocCount = 0;
    }

    /// <summary>
    /// Creates a DocumentsWriterPerThread with fresh analyser instances for thread-safe parallel indexing.
    /// </summary>
    private DocumentsWriterPerThread CreateThreadLocalDocumentWriter()
    {
        IAnalyser threadLocalDefaultAnalyser = _defaultAnalyser switch
        {
            StandardAnalyser => new StandardAnalyser(_config.AnalyserInternCacheSize, _config.StopWords),
            StemmedAnalyser => new StemmedAnalyser(),
            _ => _defaultAnalyser
        };

        var threadLocalFieldAnalysers = new Dictionary<string, IAnalyser>(_config.FieldAnalysers.Count);
        foreach (var kvp in _config.FieldAnalysers)
        {
            threadLocalFieldAnalysers[kvp.Key] = kvp.Value switch
            {
                StandardAnalyser => new StandardAnalyser(),
                StemmedAnalyser => new StemmedAnalyser(),
                _ => kvp.Value
            };
        }

        return new DocumentsWriterPerThread(threadLocalDefaultAnalyser, threadLocalFieldAnalysers);
    }

    private void MergeDwpt(DocumentsWriterPerThread dwpt)
    {
        int docBase = _bufferedDocCount;
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

        foreach (var storedDoc in dwpt.StoredFields)
        {
            _sfDocStarts.Add(_sfFieldIds.Count);
            foreach (var (name, values) in storedDoc)
            {
                foreach (var value in values)
                    AppendStoredField(name, value);
            }
        }

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
                Array.Resize(ref dstCounts, Math.Max(dstCounts.Length * 2, newTotal));
                _docTokenCounts[fieldName] = dstCounts;
            }
            
            for (int i = 0; i < dwpt.DocCount && i < counts.Length; i++)
                dstCounts[docBase + i] = counts[i];
        }

        foreach (var fn in dwpt.FieldNames)
            _fieldNames.Add(fn);

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
        // Stored-field overhead already tracked; postings tracked via EstimatedBytes

        if (ShouldFlush())
            FlushSegment();
    }
}
