using System.Text.Json;
using Rowles.LeanLucene.Store;

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
            _mergeTask = Task.Run(() =>
            {
                if (ct.IsCancellationRequested) return;

                SpinWait sw = default;
                while (!_writeLock.TryEnter())
                {
                    if (ct.IsCancellationRequested) return;
                    sw.SpinOnce();
                }
                try
                {
                    var merger = new SegmentMerger(_directory, _config.MergeThreshold, _config.PostingsSkipInterval);
                    var merged = merger.MaybeMerge(_committedSegments, ref _nextSegmentOrdinal);
                    if (!ReferenceEquals(merged, _committedSegments))
                    {
                        _committedSegments.Clear();
                        _committedSegments.AddRange(merged);

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
