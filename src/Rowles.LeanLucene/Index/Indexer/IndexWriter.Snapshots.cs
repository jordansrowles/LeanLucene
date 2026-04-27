namespace Rowles.LeanLucene.Index.Indexer;

public sealed partial class IndexWriter
{
    private void ScheduleBackgroundMerge()
    {
        lock (_mergeLock)
        {
            if (_mergeTask is not null && !_mergeTask.IsCompleted)
                return;

            var ct = _mergeCts.Token;
            _mergeTask = Task.Factory.StartNew(() =>
            {
                if (ct.IsCancellationRequested) return;

                lock (_writeLock)
                {
                    if (ct.IsCancellationRequested) return;

                    using var mergeActivity = Diagnostics.LeanLuceneActivitySource.Source
                        .StartActivity(Diagnostics.LeanLuceneActivitySource.Merge);
                    var mergeSw = System.Diagnostics.Stopwatch.StartNew();

                    var merger = new SegmentMerger(_directory, _config.MergeThreshold, _config.PostingsSkipInterval);
                    var sourceSegments = _committedSegments.ToArray();
                    var protectedSegments = GetSnapshotProtectedSegments();
                    var merged = merger.MaybeMerge(_committedSegments, ref _nextSegmentOrdinal, protectedSegments);

                    mergeSw.Stop();
                    int segmentsMerged = merged != _committedSegments
                        ? sourceSegments.Length - merged.Count + 1
                        : 0;
                    mergeActivity?.SetTag("index.segments_merged", segmentsMerged);
                    if (segmentsMerged > 0)
                        _config.Metrics.RecordMerge(mergeSw.Elapsed, segmentsMerged);

                    if (!ReferenceEquals(merged, _committedSegments))
                    {
                        _committedSegments.Clear();
                        _committedSegments.AddRange(merged);

                        _commitGeneration++;
                        WriteCommitFile(_commitGeneration);
                        WriteCommitStats(_commitGeneration);
                        _config.DeletionPolicy.OnCommit(_directory.DirectoryPath, _commitGeneration, GetSnapshotProtectedSegments());

                        var activeSegments = new HashSet<string>(
                            _committedSegments.Select(static segment => segment.SegmentId),
                            StringComparer.Ordinal);
                        foreach (var segment in sourceSegments)
                        {
                            if (!activeSegments.Contains(segment.SegmentId) &&
                                !protectedSegments.Contains(segment.SegmentId))
                            {
                                merger.CleanupSegmentFiles(segment);
                            }
                        }
                    }
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Returns all committed and flushed segments for near-real-time search.
    /// Flushes any buffered documents first but does not write a commit file.
    /// </summary>
    public IReadOnlyList<SegmentInfo> GetNrtSegments()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        lock (_writeLock)
        {
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
}
